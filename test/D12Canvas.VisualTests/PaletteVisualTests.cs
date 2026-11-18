using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class PaletteVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public PaletteVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task NewPageAsync(ColorScheme colorScheme, int width = 400, int height = 400)
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = width, Height = height },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/palette-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(8);
    }

    [Fact]
    public async Task RenderedPalette_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task RenderedPalette_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);

        await ContentSnapshot.Verify(_page);
    }

    private async Task OpenTheMoreComponentsFlyoutAsync()
    {
        await _page
            .GetByRole(AriaRole.Button, new() { Name = "More components", Exact = true })
            .ClickAsync();
        await Expect(_page.GetByRole(AriaRole.Menu, new() { Name = "More components" }))
            .ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Menuitem, new() { Name = "Terminator" }))
            .ToBeFocusedAsync();
        await _page.Mouse.MoveAsync(5, 5);
    }

    [Fact]
    public async Task MoreComponentsFlyoutOpen_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light, width: 900, height: 700);
        await OpenTheMoreComponentsFlyoutAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task MoreComponentsFlyoutOpen_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark, width: 900, height: 700);
        await OpenTheMoreComponentsFlyoutAsync();

        await ContentSnapshot.Verify(_page);
    }
}
