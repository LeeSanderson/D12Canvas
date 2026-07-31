using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baselines for the property panel: its empty state with nothing selected, and
// its populated state for each property-editor kind (Number, Color, Dropdown, Custom). Checkbox
// has no baseline here since no built-in declares a Checkbox-kind property yet - see
// PropertyPanelTests for its control-level coverage.
public sealed class PropertyPanelVisualTests : IAsyncLifetime
{
    private static readonly PageScreenshotOptions ScreenshotOptions = new()
    {
        FullPage = true,
        Type = ScreenshotType.Png,
        Animations = ScreenshotAnimations.Disabled,
    };

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public PropertyPanelVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/property-panel-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(6);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task EmptyPanel_MatchesBaseline()
    {
        await Expect(_page.Locator(".d12-property-panel-empty")).ToBeVisibleAsync();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task PopulatedPanel_MatchesBaseline()
    {
        await _page.Locator(".d12-palette-entry-button[aria-label='Rectangle']").ClickAsync();
        await _page.Locator(".component-container").ClickAsync();

        await Expect(_page.Locator("#d12-property-panel-field-StrokeWidth")).ToBeVisibleAsync();
        await Expect(_page.Locator("#d12-property-panel-field-FillColor")).ToBeVisibleAsync();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task PopulatedPanelWithDropdownControl_MatchesBaseline()
    {
        await _page.Locator(".d12-palette-entry-button[aria-label='Text']").ClickAsync();
        await _page.Locator(".component-container").ClickAsync();

        await Expect(_page.Locator("#d12-property-panel-field-FontWeight")).ToBeVisibleAsync();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task PopulatedPanelWithCustomControl_MatchesBaseline()
    {
        await _page.Locator(".d12-palette-entry-button[aria-label='Demo note']").ClickAsync();
        await _page.Locator(".component-container").ClickAsync();

        await Expect(_page.Locator(".demo-note-color-editor")).ToBeVisibleAsync();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
