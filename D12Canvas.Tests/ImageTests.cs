using Bunit;
using D12Canvas.BuiltIns;
using Xunit;
using ErrorEventArgs = Microsoft.AspNetCore.Components.Web.ErrorEventArgs;

namespace D12Canvas.Tests;

public class ImageTests : ComponentTestBase
{
    [Fact]
    public void RendersImgWithUrlAltTextAndFitFromProps()
    {
        var image = Render<Image>(parameters =>
            parameters.Add(
                p => p.Props,
                new ImageProps("https://example.com/photo.png", "A photo", "contain")
            )
        );

        var img = image.Find(".d12-image");
        Assert.Equal("https://example.com/photo.png", img.GetAttribute("src"));
        Assert.Equal("A photo", img.GetAttribute("alt"));
        Assert.Contains("object-fit: contain", img.GetAttribute("style"));
    }

    [Fact]
    public void RendersPlaceholderWhenUrlIsMissing()
    {
        var image = Render<Image>();

        Assert.Empty(image.FindAll(".d12-image"));
        Assert.NotEmpty(image.FindAll(".d12-image-placeholder"));
    }

    [Fact]
    public void ThePlaceholderPaintsFromTheCanvasThemeTokens()
    {
        var image = Render<Image>();

        var placeholder = ExtractBlock(StyleBlockText(image), ".d12-image-placeholder");
        Assert.Contains("background-color: var(--d12-surface)", placeholder);
        Assert.Contains("border: 1px dashed var(--d12-border)", placeholder);
        Assert.Contains("color: var(--d12-muted-text)", placeholder);
    }

    [Fact]
    public void RendersPlaceholderWhenImageFailsToLoad()
    {
        var image = Render<Image>(parameters =>
            parameters.Add(
                p => p.Props,
                new ImageProps("https://example.com/broken.png", "A photo", "cover")
            )
        );

        image.Find(".d12-image").Error(new ErrorEventArgs());

        Assert.Empty(image.FindAll(".d12-image"));
        Assert.NotEmpty(image.FindAll(".d12-image-placeholder"));
    }

    [Fact]
    public void RecoversFromAPriorLoadFailureWhenPropsSupplyANewUrl()
    {
        var image = Render<Image>(parameters =>
            parameters.Add(
                p => p.Props,
                new ImageProps("https://example.com/broken.png", "", "cover")
            )
        );
        image.Find(".d12-image").Error(new ErrorEventArgs());

        image.Render(parameters =>
            parameters.Add(
                p => p.Props,
                new ImageProps("https://example.com/fixed.png", "", "cover")
            )
        );

        var img = image.Find(".d12-image");
        Assert.Equal("https://example.com/fixed.png", img.GetAttribute("src"));
    }
}
