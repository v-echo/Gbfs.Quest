using Gbfs.Quest.Models;
using System.Collections.Concurrent;

namespace Gbfs.Quest.Data
{
    /// <summary>
    /// A static class with question generator functions. The approach taken produces questions from individual feeds.
    /// Another approach would've been to build a joined dataset from all feeds keyed by provider, and create cross-feed questions. Why didn't I go this route? Requires ordering async operations + time constraints.
    /// </summary>
    internal class QuestionGenerator
    {
        static readonly string[] RentalMethods = ["KEY", "CREDITCARD", "PAYPASS", "APPLEPAY", "ANDROIDPAY", "TRANSITCARD", "ACCOUNTNUMBER", "PHONE"];
        static readonly string[] PostCodes = ["10001", "SW1A 1AA", "10115", "75008", "00100", "28013", "1010", "100-0001", "1000", "2000", "110001", "2000", "3000", "01000", "1000 AA", "8001", "4000", "00130", "2000", "10200", "6000", "10000", "0001", "00150", "5000", "7000", "08001", "10000", "90001", "A1A 1A1"];

        static readonly ConcurrentDictionary<string, string> StationNames = new();

        /// <summary>
        /// Generate a set of questions and answers based on a given GBFS feed
        /// </summary>
        /// <param name="feed">The feed to source for the Q&A</param>
        /// <returns></returns>
        public static Dictionary<string, (object answer, bool valid)[]> Generate(StationInfo[] feeds, CancellationToken token = default)
        {
            var questions = new Dictionary<string, (object answer, bool valid)[]>();
            var rand = new Random();

            foreach (var feed in feeds)
            {
                if (token.IsCancellationRequested)
                    return questions;

                StationNames.TryAdd(feed.StationId, feed.Name);

                if (feed.Address is not null && feed.Capacity is not null)
                {
                    questions.TryAdd(string.Format(QuestionTemplates.StationCapacity, feed.ShortName ?? feed.Name), GenerateOptions(feed.Capacity.Value, () => GenerateNrInRange(feed.Capacity.Value, 5, rand)));
                }

                if (feed.RentalMethods is [..] && feed.RentalMethods.Length < RentalMethods.Length)
                {
                    questions.TryAdd(string.Format(QuestionTemplates.StationRentalTypes, feed.ShortName ?? feed.Name), GenerateOptions(feed.RentalMethods.First(), RentalMethods.Except(feed.RentalMethods)));
                }

                if (feed.PostCode is not null && feed.Address is not null)
                {
                    questions.TryAdd(string.Format(QuestionTemplates.StationPostCode, feed.Address), GenerateOptions(feed.PostCode, rand.GetItems(PostCodes, rand.Next(3, 4))));
                }

                if (feed.IsVirtualStation is not null)
                {
                    questions.TryAdd(string.Format(QuestionTemplates.StationVirtual, feed.Name), GenerateOptions(feed.IsVirtualStation.Value ? "Yes" : "No", [feed.IsVirtualStation.Value ? "No" : "Yes"]));
                }

                if (feed.RegionId is not null)
                {
                    questions.TryAdd(string.Format(QuestionTemplates.StationRegion, feed.Name), GenerateOptions(feed.RegionId, [Guid.NewGuid().ToString(), GenerateNrInRange(500, 250, rand).ToString(), GenerateNrInRange(500, 250, rand).ToString(), GenerateNrInRange(500, 250, rand).ToString()]));
                }
            }
            
            return questions;
        }

        /// <summary>
        /// Generate a set of questions and answers based on a given GBFS feed
        /// </summary>
        /// <param name="feed">The feed to source for the Q&A</param>
        /// <returns></returns>
        public static Dictionary<string, (object answer, bool valid)[]> Generate(StationStatus[] feeds, CancellationToken token = default)
        {
            var questions = new Dictionary<string, (object answer, bool valid)[]>();
            var rand = new Random();

            foreach (var feed in feeds)
            {
                if (token.IsCancellationRequested)
                    return questions;

                // To ensure we have the station name, we would need to preload data, aggregate or reorder the async operations. That's possible but it needs a different approach and has its own costs.
                if (!StationNames.TryGetValue(feed.StationId, out var name))
                    continue;

                questions.TryAdd(string.Format(QuestionTemplates.StationBikes, name ?? feed.StationId), GenerateOptions(feed.NumBikesAvailable, () => GenerateNrInRange(feed.NumBikesAvailable, 4, rand)));
                questions.TryAdd(string.Format(QuestionTemplates.StationRenting, name ?? feed.StationId), GenerateOptions(feed.IsRenting ? "Yes" : "No", [feed.IsRenting ? "No" : "Yes"]));
            }

            return questions;
        }

        // Given a correct answer and a generator function, create an array of alternative options
        private static (object, bool)[] GenerateOptions<T>(T answer, Func<T> generator) where T : IEquatable<T>
        {
            List<(object, bool)> options = [(answer, true)];
            var rand = new Random();

            for (int i = 0; i < rand.Next(3, 5); i++)
            {
                T value = generator();

                while (value.Equals(answer))
                    value = generator();

                options.Add((value, false));
            }

            var result = options.Distinct().ToArray();
            rand.Shuffle(result);

            return result;
        }

        // Given a correct answer and a generator function, create an array of alternative options
        private static (object, bool)[] GenerateOptions<T>(T answer, IEnumerable<T> others) where T : IEquatable<T>
        {
            var rand = new Random();

            (object, bool)[] options = [..others.Select(o => (o, false)), (answer, true)];
            rand.Shuffle(options);

            return options;
        }

        static int GenerateNrInRange(int value, int range, Random? rand) => (rand ?? new Random()).Next(Math.Max(0, value - range), value + range);
    }
}
