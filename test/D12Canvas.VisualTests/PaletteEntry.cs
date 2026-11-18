using Microsoft.Playwright;

namespace D12Canvas.VisualTests;

internal static class PaletteEntry
{
    public const string DemoNote = "Demo note";
    public const string StressItem = "Stress item";
    public const string Rectangle = "Rectangle";

    public static ILocator Named(IPage page, string accessibleName) =>
        page.Locator($".d12-palette-entry[aria-label='{accessibleName}']");
}
