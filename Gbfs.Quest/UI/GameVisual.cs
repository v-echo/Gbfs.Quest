using CsvHelper;
using Gbfs.Quest.Data;
using System.Collections.Concurrent;
using System.Globalization;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Threading;

namespace Gbfs.Quest.UI
{
    internal class GameVisual(GbfsService service) : IVisualTree
    {
        // Reactive state is here + SharedState class
        readonly State<bool> SessionInProgress = new(false);
        readonly State<bool> Loading = new(false);
        readonly State<string> ProgressText = new(string.Empty);
        readonly State<int> CurrentQuestionIndex = new(-1);
        readonly State<int> SelectedAnswer = new(0);
        readonly State<int> CurrentPoints = new(0);
        readonly State<bool> FinishedOnce = new(false);

        // Local, non-reactive state
        KeyValuePair<string, (object answer, bool valid)[]>[] Questions = [];
        readonly ConcurrentDictionary<string, int> Players = new();
        readonly List<(string player, int score, DateTime dt)> History = [];
        Table? Leaderboard;

        public Visual GetVisual()
        {
            var start = new Button("Start")
                .Tone(ControlTone.Primary)
                .Click(StartSession);

            var confirm = new Button("Continue")
                .Tone(ControlTone.Success)
                .Click(CheckAnswer)
                .IsVisible(SessionInProgress);

            var logout = new Button("Logout")
                .Tone(ControlTone.Warning)
                .Click(Logout);

            var export = new Button("Export History")
                .Tone(ControlTone.Primary)
                .Click(ExportHistory)
                .IsVisible(FinishedOnce);

            var points = new ComputedVisual(() => "Current Points: " + CurrentPoints.Value);
            var question = new ComputedVisual(() => CurrentQuestionIndex.Value >= 0 ? Questions[CurrentQuestionIndex.Value].Key : null);

            Leaderboard = new Table()
                .Headers("Player", "Highest Score")
                .IsVisible(() => FinishedOnce.Value && !SessionInProgress.Value);

            var options = new OptionList<OptionListItem>()
                .MinHeight(6)
                .MaxHeight(6)
                .IsVisible(SessionInProgress)
                .HorizontalAlignment(Align.Stretch)
                .Update(o => 
                {
                    var index = CurrentQuestionIndex.Value;
                    if (index >= 0)
                    {
                        o.Items(PopulateOptions(index));
                        o.SelectedIndex = SelectedAnswer.Value;
                    }
                })
                .SelectionChanged((_, e) => SelectedAnswer.Value = e.NewIndex);
            //options.ItemActivated((_, e) => SelectedAnswer.Value = e.Index);
            //options.SelectionChanged((_, e) => SelectedAnswer.Value = e.NewIndex);

            var stack = new VStack(
                new ComputedVisual(() => "Welcome, " + SharedState.CurrentUser.Value),
                new ComputedVisual(() => ProgressText.Value),
                new HStack(start, logout, export).Spacing(1).IsVisible(() => !SessionInProgress.Value),
                points,
                Leaderboard,
                new VStack(question, options, confirm).Spacing(1).IsVisible(SessionInProgress)).Spacing(1).IsVisible(SharedState.Authenticated);

            return stack;
        }

        private OptionListItem[] PopulateOptions(int index)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Questions.Length);

            return Questions[index].Value.Select(o => new OptionListItem(o.answer.ToString()!)).ToArray();
        }

        private void StartSession()
        {
            if (Loading.Value)
            {
                return;
            }

            Reset();
            Loading.Value = true;
            ProgressText.Value = "Generating Questions...";

            _ = Task.Run(async () =>
            {
                try
                {
                    var questions = await service.GetQuestionsAsync(100).ConfigureAwait(false);

                    await Dispatcher.Current.InvokeAsync(() =>
                    {
                        ProgressText.Value = "";
                        Loading.Value = false;
                        SessionInProgress.Value = true;
                        Questions = questions;
                        CurrentQuestionIndex.Value++;
                    });

                    using var duration = new PeriodicTimer(TimeSpan.FromSeconds(60));

                    await duration.WaitForNextTickAsync();
                    await Dispatcher.Current.InvokeAsync(() =>
                    {
                        if (SessionInProgress.Value) // It's possible the player has exhausted the questions before the timer fires
                        {
                            SessionInProgress.Value = false;
                            UpdateLeaderboard();
                        }
                    });
                }
                catch (Exception ex)
                {
                    await Dispatcher.Current.InvokeAsync(() =>
                    {
                        ProgressText.Value = $"Error: {ex.Message}";
                        Loading.Value = false;
                    });
                }
            });
        }

        private void CheckAnswer()
        {
            if (CurrentQuestionIndex.Value == Questions.Length - 1)
            {
                ProgressText.Value = $"Congratulations! You've answered all {Questions.Length} questions.";
                SessionInProgress.Value = false;
                UpdateLeaderboard();
                return;
            }

            if (Questions[CurrentQuestionIndex.Value].Value[SelectedAnswer.Value].valid)
                CurrentPoints.Value += 50;
            else CurrentPoints.Value -= 20;

            SelectedAnswer.Value = 0;
            CurrentQuestionIndex.Value++;
        }

        private void UpdateLeaderboard()
        {
            if (SharedState.CurrentUser.Value is null)
                return;

            if (CurrentPoints.Value < 0) // Player lost, so there's nothing to update since we only track victories.
            {
                ProgressText.Value = $"Too bad! Better luck next time.";
                return;
            }

            History.Add((SharedState.CurrentUser.Value, CurrentPoints.Value, DateTime.UtcNow));
            Players.AddOrUpdate(SharedState.CurrentUser.Value, CurrentPoints.Value, (user, value) => Math.Max(value, CurrentPoints.Value));
            Leaderboard!.Rows(History.OrderByDescending(p => p.score).Select(p => new Visual[] { p.player, p.score.ToString() }).ToArray());
            FinishedOnce.Value = true;
        }

        private void ExportHistory()
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolderOption.None), $"{SharedState.CurrentUser.Value}.history.csv");
                using var writer = new StreamWriter(path);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.WriteRecords(History.Where(h => h.player == SharedState.CurrentUser.Value).Select(h => new { Player = h.player, Score = h.score, Date = h.dt }));
                ProgressText.Value = $"Saved player history to Desktop\\{SharedState.CurrentUser.Value}.history.csv";
            }
            catch (Exception ex)
            {
                ProgressText.Value = $"Oops. Saving failed for the following reason: {ex.Message}";
            }
        }

        private void Logout()
        {
            Reset();
            SharedState.Authenticated.Value = false;
        }

        private void Reset()
        {
            CurrentQuestionIndex.Value = -1;
            CurrentPoints.Value = 0;
            SelectedAnswer.Value = 0;
        }
    }
}
