using AngleSharp.Dom;

namespace D12Canvas.Tests;

internal static class DrawnGlyph
{
    private static IElement IconIn(IElement container) =>
        Assert.Single(container.QuerySelectorAll("svg.d12-icon"));

    public static void AssertDecorativeIcon(Glyph expected, IElement container)
    {
        var icon = IconIn(container);
        Assert.Equal(
            expected.Parts.Select(part => part.Data),
            icon.QuerySelectorAll("path").Select(path => path.GetAttribute("d"))
        );
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
    }
}
