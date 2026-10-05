using Bunit;
using D12Canvas.BuiltIns;
using Xunit;

namespace D12Canvas.Tests;

public class RectangleTests : ComponentTestBase
{
    // Authored colours travel as override custom properties that the component's own rule
    // prefers over the board tokens; the stroke width is the one plain inline declaration.
    [Fact]
    public void RendersFillAndStrokeFromProps()
    {
        var rectangle = Render<Rectangle>(parameters =>
            parameters.Add(p => p.Props, new RectangleProps("#ff0000", "#00ff00", 4))
        );

        var style = rectangle.Find(".d12-rectangle").GetAttribute("style");
        Assert.Contains("--d12-board-fill-override: #ff0000", style);
        Assert.Contains("--d12-board-stroke-override: #00ff00", style);
        Assert.Contains("border-width: 4px", style);
    }

    // The default props carry no colour, so the default rendering emits no override at all and
    // the board tokens paint it (see ThemedDefaultTests).
    [Fact]
    public void RendersWithItsDefaultPropsWhenNoneSupplied()
    {
        var rectangle = Render<Rectangle>();

        var style = rectangle.Find(".d12-rectangle").GetAttribute("style");
        Assert.Equal("border-width: 2px;", style);
    }
}
