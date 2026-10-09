using System.Runtime.CompilerServices;
using Microsoft.Playwright;

namespace D12Canvas.VisualTests;

// Snapshots the demo page's content area rather than the whole page, so the demo app's nav menu
// and layout stay out of every baseline. Some demo pages overflow the content area's fixed
// height, so the screenshot covers its scroll extent, not just its box.
public static class ContentSnapshot
{
    private const string ContentSelector = "article.content";

    private const string ContentExtentScript = """
        element => {
            const box = element.getBoundingClientRect();
            const left = Math.floor(box.left + window.scrollX);
            const top = Math.floor(box.top + window.scrollY);
            const right = Math.ceil(box.left + window.scrollX + Math.max(box.width, element.scrollWidth));
            const bottom = Math.ceil(box.top + window.scrollY + Math.max(box.height, element.scrollHeight));
            return { X: left, Y: top, Width: right - left, Height: bottom - top };
        }
        """;

    // A property bar is drawn hidden until it has measured its own width, one interop round trip
    // after it mounts.
    private const string UnmeasuredPropertyBar = ".d12-property-bar[style*='visibility: hidden']";

    public static async Task Verify(IPage page, [CallerFilePath] string sourceFile = "")
    {
        await Assertions.Expect(page.Locator(UnmeasuredPropertyBar)).ToHaveCountAsync(0);
        var content = page.Locator(ContentSelector);
        var html = await content.InnerHTMLAsync();
        var extent = await content.EvaluateAsync<Clip>(ContentExtentScript);
        var png = await page.ScreenshotAsync(
            new PageScreenshotOptions
            {
                FullPage = true,
                Clip = extent,
                Type = ScreenshotType.Png,
                Animations = ScreenshotAnimations.Disabled,
            }
        );

        await Verifier.Verify(
            [new Target("html", html), new Target("png", new MemoryStream(png))],
            sourceFile: sourceFile
        );
    }
}
