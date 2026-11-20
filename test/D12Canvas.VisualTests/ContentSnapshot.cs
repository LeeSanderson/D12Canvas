using System.Runtime.CompilerServices;
using Microsoft.Playwright;

namespace D12Canvas.VisualTests;

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

    private const string UnmeasuredPropertyBar = ".d12-property-bar[style*='visibility: hidden']";

    private const string SettlingPropertyBar = ".d12-property-bar-settling";

    private const string WatchForSettlingScript = """
        () => {
            let settled = false;
            const observer = new MutationObserver(records => {
                settled ||= records.some(record => record.target.classList.contains("d12-property-bar-settling"));
            });
            observer.observe(document.body, { subtree: true, attributes: true, attributeFilter: ["class"] });
            window.__d12StopWatchingForSettling = () => {
                observer.disconnect();
                return settled;
            };
        }
        """;

    private const string StopWatchingForSettlingScript =
        "() => window.__d12StopWatchingForSettling()";

    private const int MaxCaptureAttempts = 3;

    public static async Task Verify(IPage page, [CallerFilePath] string sourceFile = "")
    {
        await Assertions.Expect(page.Locator(UnmeasuredPropertyBar)).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(SettlingPropertyBar)).ToHaveCountAsync(0);
        var content = page.Locator(ContentSelector);
        var html = await content.InnerHTMLAsync();
        var extent = await content.EvaluateAsync<Clip>(ContentExtentScript);
        var png = await CaptureAsync(page, extent);

        await Verifier.Verify(
            [new Target("html", html), new Target("png", new MemoryStream(png))],
            sourceFile: sourceFile
        );
    }

    private static async Task<byte[]> CaptureAsync(IPage page, Clip extent)
    {
        for (var attempt = 1; ; attempt++)
        {
            await page.EvaluateAsync(WatchForSettlingScript);
            var png = await page.ScreenshotAsync(
                new PageScreenshotOptions
                {
                    FullPage = true,
                    Clip = extent,
                    Type = ScreenshotType.Png,
                    Animations = ScreenshotAnimations.Disabled,
                }
            );
            var barSettledDuringCapture = await page.EvaluateAsync<bool>(
                StopWatchingForSettlingScript
            );
            if (!barSettledDuringCapture || attempt == MaxCaptureAttempts)
            {
                return png;
            }

            await Assertions.Expect(page.Locator(SettlingPropertyBar)).ToHaveCountAsync(0);
        }
    }
}
