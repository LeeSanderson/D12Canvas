using Bunit;
using D12Canvas.App.Components;

namespace D12Canvas.App.Tests;

public sealed class IconTests : BunitContext
{
    [Fact]
    public void AnIconIsADecorativeInlineSvgSizedToTheTextAndColouredLikeIt()
    {
        var icon = Render<Icon>(parameters => parameters.Add(p => p.Name, "trash"));

        var svg = icon.Find("svg");
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.Equal("1em", svg.GetAttribute("width"));
        Assert.Equal("1em", svg.GetAttribute("height"));
        Assert.Equal("currentColor", svg.GetAttribute("fill"));
        Assert.Equal(2, icon.FindAll("path").Count);
    }

    [Fact]
    public void AnUnknownIconNameFailsLoudly()
    {
        Assert.Throws<ArgumentException>(
            () => Render<Icon>(parameters => parameters.Add(p => p.Name, "no-such-icon"))
        );
    }
}
