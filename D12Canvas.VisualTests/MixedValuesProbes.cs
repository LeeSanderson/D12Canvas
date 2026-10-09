using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A group of two rectangles with different stroke widths, one of them locked. An edit writes only
// the unlocked member, so the row stays mixed, and the field must go back to showing that rather
// than keep the typed text as though every member now held it.
public sealed class MixedValuesProbes : IAsyncLifetime
{
    private const string BlueRectangleId = "10800000-0000-0000-0000-000000000001";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public MixedValuesProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public async ValueTask InitializeAsync()
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/mixed-values-demo?locked=true");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(4);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task ARowStillMixedAfterAnEditClearsTheTypedTextBackToItsPlaceholder()
    {
        await _page
            .Locator($".component-container[data-d12-entity='{BlueRectangleId}']")
            .ClickAsync();
        var strokeWidth = _page.Locator("#d12-property-panel-field-StrokeWidth");
        await Expect(strokeWidth).ToHaveAttributeAsync("placeholder", "Mixed");

        await strokeWidth.FillAsync("9");
        await strokeWidth.PressAsync("Enter");

        await Expect(_page.Locator(".d12-rectangle").First)
            .ToHaveAttributeAsync(
                "style",
                new System.Text.RegularExpressions.Regex("border-width: 9px")
            );
        await Expect(_page.Locator("#d12-property-panel-field-StrokeWidth")).ToHaveValueAsync("");
        await Expect(_page.Locator("#d12-property-panel-field-StrokeWidth"))
            .ToHaveAttributeAsync("placeholder", "Mixed");
    }

    [Fact]
    public async Task AMixedColourSwatchOpensOnAColourOtherThanBlack()
    {
        await _page
            .Locator($".component-container[data-d12-entity='{BlueRectangleId}']")
            .ClickAsync();

        var fill = _page.Locator("#d12-property-panel-field-FillColor");
        await Expect(fill)
            .ToHaveClassAsync(
                new System.Text.RegularExpressions.Regex("d12-property-panel-color-mixed")
            );
        await Expect(fill).Not.ToHaveValueAsync("#000000");
    }
}
