using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baselines for the property panel: its empty state with nothing selected, its
// populated state for each property-editor kind (Number, Color, Dropdown, Custom), and the two
// populated states again under the dark colour scheme, where the panel's raised values and
// color-scheme must carry its native inputs along with it. Checkbox has no baseline here since no
// built-in declares a Checkbox-kind property yet - see PropertyPanelTests for its control-level
// coverage.
public sealed class PropertyPanelVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
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
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/property-panel-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(6);
    }

    private async Task PlaceAndSelectAsync(string paletteEntryLabel)
    {
        await _page
            .Locator($".d12-palette-entry-button[aria-label='{paletteEntryLabel}']")
            .ClickAsync();
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
        await PlaceAndSelectAsync("Text");

        await Expect(_page.Locator("#d12-property-panel-field-FontWeight")).ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PopulatedPanelWithDropdownControl_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await PlaceAndSelectAsync("Text");

        await Expect(_page.Locator("#d12-property-panel-field-FontWeight")).ToBeVisibleAsync();

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
