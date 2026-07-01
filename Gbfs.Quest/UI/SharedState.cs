using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Styling;

namespace Gbfs.Quest.UI
{
    internal static class SharedState
    {
        public static State<bool> Authenticated { get; } = new(false);
        public static State<string?> CurrentUser { get; } = new(null);
        public static State<int> CurrentPaletteIndex { get; } = new(37);
        public static State<ThemeAccentColor> CurrentAccent { get; } = new(ThemeAccentColor.Blue);
    }
}
