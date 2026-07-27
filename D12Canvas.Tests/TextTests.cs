using Bunit;
using D12Canvas.BuiltIns;
using Xunit;

namespace D12Canvas.Tests;

public class TextTests : ComponentTestBase
{
    [Fact]
    public void RendersContentAndFontStylingFromProps()
    {
        var text = Render<Text>(parameters =>
            parameters.Add(
                p => p.Props,
                new TextProps("Hello, D12Canvas", "#ff0000", 24, "bold", "center")
            )
        );

        var element = text.Find(".d12-text");
        Assert.Contains("Hello, D12Canvas", element.TextContent);

        var style = element.GetAttribute("style");
        Assert.Contains("color: #ff0000", style);
        Assert.Contains("font-size: 24px", style);
        Assert.Contains("font-weight: bold", style);
        Assert.Contains("text-align: center", style);
    }

    [Fact]
    public void RendersWithItsDefaultPropsWhenNoneSupplied()
    {
        var text = Render<Text>();

        var element = text.Find(".d12-text");
        var style = element.GetAttribute("style");
        Assert.Contains("color: #000000", style);
        Assert.Contains("font-size: 16px", style);
        Assert.Contains("font-weight: normal", style);
        Assert.Contains("text-align: left", style);
    }
}
