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
        Assert.Contains("background-color: #ff0000", style);
        Assert.Contains("border: 4px solid #00ff00", style);
    }

    [Fact]
    public void RendersWithItsDefaultPropsWhenNoneSupplied()
    {
        var rectangle = Render<Rectangle>();

        var style = rectangle.Find(".d12-rectangle").GetAttribute("style");
        Assert.Contains("background-color: #FFFFFF", style);
        Assert.Contains("border: 2px solid #333333", style);
    }
}
