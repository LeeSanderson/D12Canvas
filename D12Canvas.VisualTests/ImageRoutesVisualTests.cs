using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The image routes demo board on a dark theme: an empty image showing its placeholder beside a
// filled one, the property panel's picture editor for a selected image, and the object menu's
// two image rows.
public sealed class ImageRoutesVisualTests : IAsyncLifetime
{
    private const string EmptyImageId = "f0000000-0000-0000-0000-000000000001";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public ImageRoutesVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public async ValueTask InitializeAsync()
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/image-routes-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);
        await Expect(_page.Locator(".d12-image")).ToHaveCountAsync(1);
        await _page.Locator(".d12-image").EvaluateAsync("image => image.decode()");
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator EmptyImage =>
        _page.Locator($".component-container[data-d12-entity='{EmptyImageId}']");

    [Fact]
    public async Task EmptyImagePlaceholderOnADarkBoard_MatchesBaseline()
    {
        await Expect(_page.Locator(".d12-image-placeholder")).ToHaveCountAsync(1);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PictureEditorForASelectedImage_MatchesBaseline()
    {
        await EmptyImage.ClickAsync();

        await Expect(_page.Locator(".d12-image-picture-button")).ToHaveCountAsync(2);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task ObjectMenuOnAnImageOffersTheImageRows_MatchesBaseline()
    {
        await EmptyImage.ClickAsync();
        await EmptyImage.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        await Expect(_page.Locator(".d12-context-menu-item", new() { HasText = "Choose image…" }))
            .ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }
}
