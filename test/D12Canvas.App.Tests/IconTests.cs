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
    public void TheFaviconDrawsTheSameMarkAsTheD12Icon()
    {
        var favicon = System.Xml.Linq.XDocument.Load(
            Path.Combine(RepositoryRoot(), "app", "D12Canvas.App", "wwwroot", "favicon.svg")
        );

        var faviconPaths = favicon
            .Descendants()
            .Where(element => element.Name.LocalName == "path")
            .Select(path => new IconPath(
                (string)path.Attribute("d")!,
                (string?)path.Attribute("fill-rule")
            ));

        Assert.Equal(IconPaths.For("d12"), faviconPaths);
    }

    [Fact]
    public void TheLoadingScreenDrawsTheSameMarkAsTheD12Icon()
    {
        var indexHtml = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(
            File.ReadAllText(
                Path.Combine(RepositoryRoot(), "app", "D12Canvas.App", "wwwroot", "index.html")
            )
        );

        var loadingScreenPaths = indexHtml
            .QuerySelectorAll("#app svg path")
            .Select(path => new IconPath(path.GetAttribute("d")!, path.GetAttribute("fill-rule")));

        Assert.Equal(IconPaths.For("d12"), loadingScreenPaths);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "D12Canvas.slnx")))
        {
            directory = directory.Parent!;
        }

        return directory.FullName;
    }

    [Fact]
    public void AnUnknownIconNameFailsLoudly()
    {
        Assert.Throws<ArgumentException>(
            () => Render<Icon>(parameters => parameters.Add(p => p.Name, "no-such-icon"))
        );
    }
}
