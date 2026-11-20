using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class PropertyPanelVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public PropertyPanelVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task NewPageAsync(ColorScheme colorScheme)
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/property-panel-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(8);
    }

    private async Task PlaceAndSelectAsync(string paletteEntryLabel)
    {
        await _page.Locator($".d12-palette-entry[aria-label='{paletteEntryLabel}']").ClickAsync();
        await _page.Locator(".component-container").ClickAsync();
    }

    [Fact]
    public async Task EmptyPanel_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);

        await Expect(_page.Locator(".d12-property-panel-empty")).ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PopulatedPanel_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await PlaceAndSelectAsync("Rectangle");

        await Expect(_page.Locator("#d12-property-panel-field-StrokeWidth")).ToBeVisibleAsync();
        await Expect(_page.Locator("#d12-property-panel-field-FillColor")).ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PopulatedPanel_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await PlaceAndSelectAsync("Rectangle");

        await Expect(_page.Locator("#d12-property-panel-field-StrokeWidth")).ToBeVisibleAsync();
        await Expect(_page.Locator("#d12-property-panel-field-FillColor")).ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PopulatedPanelWithDropdownControl_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await PlaceAndSelectAsync("Process");

        await Expect(_page.Locator("select#d12-property-panel-field-Outline")).ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PopulatedPanelWithDropdownControl_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await PlaceAndSelectAsync("Process");

        await Expect(_page.Locator("select#d12-property-panel-field-Outline")).ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PopulatedPanelWithGlyphTiles_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await PlaceAndSelectAsync("Text");

        await Expect(_page.Locator("#d12-property-panel-field-FontWeight [aria-checked=true]"))
            .ToHaveCountAsync(1);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PopulatedPanelWithGlyphTiles_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await PlaceAndSelectAsync("Text");

        await Expect(_page.Locator("#d12-property-panel-field-FontWeight [aria-checked=true]"))
            .ToHaveCountAsync(1);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PopulatedPanelWithCustomControl_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await PlaceAndSelectAsync("Demo note");

        await Expect(_page.Locator(".demo-note-color-editor")).ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }
}
