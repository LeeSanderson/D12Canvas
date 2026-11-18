using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class GlyphGalleryVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public GlyphGalleryVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task OpenGalleryAsync(ColorScheme colorScheme)
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1400, Height = 1000 },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/glyph-gallery");
        await Expect(_page.Locator(".glyph-gallery-cell").Last).ToBeVisibleAsync();
    }

    [Fact]
    public async Task EveryGlyph_MatchesBaseline()
    {
        await OpenGalleryAsync(ColorScheme.Light);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task EveryGlyph_DarkColorScheme_MatchesBaseline()
    {
        await OpenGalleryAsync(ColorScheme.Dark);

        await ContentSnapshot.Verify(_page);
    }
}
