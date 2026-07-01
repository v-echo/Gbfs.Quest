using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Templating;
using static Gbfs.Quest.UI.SharedState;

namespace Gbfs.Quest.UI
{
    internal class DashboardVisual(AuthVisual auth, GameVisual game) : IVisualTree
    {
        List<ColorScheme> ColorPalettes { get; } = ColorScheme.GetPredefinedSchemes();

        public Visual GetVisual()
        {
            var toast = new ToastHost();

            var schemeSelect = new Select<ColorScheme>()
                .Items(ColorPalettes)
                .SelectedIndex(CurrentPaletteIndex)
                .ItemTemplate(new DataTemplate<ColorScheme>(Display: (DataTemplateValue<ColorScheme> v, in DataTemplateContext _) => new TextBlock(v.GetValue().Name), Editor: null))
                .MaxWidth(24);

            schemeSelect.SelectionChanged((_, _) =>
            {
                UpdateColors();
                var idx = Math.Clamp(CurrentPaletteIndex.Value, 0, Math.Max(0, ColorPalettes.Count - 1));
            });

            var accentSelect = new EnumSelect<ThemeAccentColor>()
                .Value(CurrentAccent)
                .MaxWidth(12)
                .SelectionChanged((_, _) => UpdateColors());

            var header = new Header
            {
                Left = new Markup("[bold]GBFS.Quest[/] CLI") { Wrap = false },
                Center = new Markup("[dim]Tab focus • Mousewheel scroll • Ctrl+Q quit[/]") { Wrap = false },
            };

            var footer = new Footer
            {
                Center = new HStack("Theme:", schemeSelect, "Accent:", accentSelect).Spacing(1)
            };

            var content = game.GetVisual();

            var root = new DockLayout()
                .HorizontalAlignment(Align.Stretch)
                .VerticalAlignment(Align.Stretch)
                .Top(new VStack(header).Spacing(0))
                .Content(new ZStack(
                    content,
                    new ComputedVisual(() => auth.GetVisual()))) // Is this still change tracked?
                .Bottom(new VStack(new CommandBar(), footer).Spacing(0));

            toast.Content(root);

            UpdateColors();

            return toast;

            void UpdateColors() => toast.SetStyle(Theme.Key, BuildTheme());
        }

        private Theme BuildTheme()
        {
            if (ColorPalettes.Count == 0)
            {
                return Theme.Default;
            }

            var index = Math.Clamp(CurrentPaletteIndex.Value, 0, ColorPalettes.Count - 1);
            return Theme.FromScheme(ColorPalettes[index], ThemeSchemeBrightness.Auto, CurrentAccent.Value);
        }
    }
}
