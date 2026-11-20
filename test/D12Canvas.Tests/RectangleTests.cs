using Bunit;
using D12Canvas.BuiltIns;
using Xunit;

namespace D12Canvas.Tests;

public class RectangleTests : ComponentTestBase
{
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

    [Fact]
    public void RendersWithItsDefaultPropsWhenNoneSupplied()
    {
        var rectangle = Render<Rectangle>();

        var style = rectangle.Find(".d12-rectangle").GetAttribute("style");
        Assert.Equal("border-width: 2px;", style);
    }
}
