using Bunit;
using D12Canvas.BuiltIns;
using Xunit;

namespace D12Canvas.Tests;

// A colour nobody chose is null and resolves from a board token in the component's own CSS; an
// authored colour is emitted as an override custom property that the same rule prefers. Nothing
// in C# knows which theme is live, so the swap happens entirely in CSS.
public class ThemedDefaultTests : ComponentTestBase
{
    [Fact]
    public void TextWithNoAuthoredColourEmitsNoOverrideAndReadsTheBoardTextToken()
    {
        var text = Render<Text>(parameters =>
            parameters.Add(p => p.Props, new TextProps("hello", null, 16, "normal", "left"))
        );

        var paragraph = text.Find(".d12-text");
        Assert.DoesNotContain("--d12-board-text-override", paragraph.GetAttribute("style"));
        Assert.DoesNotContain("color:", paragraph.GetAttribute("style"));

        var rule = ExtractBlock(StyleBlockText(text), ".d12-text {");
        Assert.Contains("color: var(--d12-board-text-override, var(--d12-board-text))", rule);
    }

    [Fact]
    public void TextWithAnAuthoredColourEmitsItAsTheOverride()
    {
        var text = Render<Text>(parameters =>
            parameters.Add(p => p.Props, new TextProps("hello", "#123456", 16, "normal", "left"))
        );

        Assert.Contains(
            "--d12-board-text-override: #123456",
            text.Find(".d12-text").GetAttribute("style")
        );
    }

    [Fact]
    public void RectangleWithNoAuthoredColoursEmitsOnlyItsStrokeWidthAndReadsTheBoardTokens()
    {
        var rectangle = Render<Rectangle>(parameters =>
            parameters.Add(p => p.Props, new RectangleProps(null, null, 3))
        );

        var box = rectangle.Find(".d12-rectangle");
        Assert.Equal("border-width: 3px;", box.GetAttribute("style"));

        var rule = ExtractBlock(StyleBlockText(rectangle), ".d12-rectangle {");
        Assert.Contains(
            "background-color: var(--d12-board-fill-override, var(--d12-board-fill))",
            rule
        );
        Assert.Contains(
            "border-color: var(--d12-board-stroke-override, var(--d12-board-stroke))",
            rule
        );
    }

    [Fact]
    public void RectangleWithAuthoredColoursEmitsEachAsItsOwnOverride()
    {
        var rectangle = Render<Rectangle>(parameters =>
            parameters.Add(p => p.Props, new RectangleProps("#e3f2fd", "#1565c0", 3))
        );

        var style = rectangle.Find(".d12-rectangle").GetAttribute("style");
        Assert.Contains("--d12-board-fill-override: #e3f2fd", style);
        Assert.Contains("--d12-board-stroke-override: #1565c0", style);
    }

    [Fact]
    public void RectangleWithOnlyAFillAuthoredLeavesTheStrokeToTheToken()
    {
        var rectangle = Render<Rectangle>(parameters =>
            parameters.Add(p => p.Props, new RectangleProps("#e3f2fd", null, 3))
        );

        var style = rectangle.Find(".d12-rectangle").GetAttribute("style");
        Assert.Contains("--d12-board-fill-override: #e3f2fd", style);
        Assert.DoesNotContain("--d12-board-stroke-override", style);
    }
}
