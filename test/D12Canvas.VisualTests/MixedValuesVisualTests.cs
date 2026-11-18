using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The mixed-values demo board: two rectangles with different fills and stroke widths, and two
// texts with different weights, beside the property panel. Selecting a pair shows each
// disagreeing row as mixed: the weight tiles press none and tag their label.
public sealed class MixedValuesVisualTests : IAsyncLifetime
{
    private const string BlueRectangleId = "10800000-0000-0000-0000-000000000001";
    private const string OrangeRectangleId = "10800000-0000-0000-0000-000000000002";
    private const string NormalTextId = "10800000-0000-0000-0000-000000000003";
    private const string BoldTextId = "10800000-0000-0000-0000-000000000004";

    private readonly IBrowser _browser;
    private IBrowserContext? _context;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public MixedValuesVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }
    }

    private async Task NewPageAsync(ColorScheme colorScheme = ColorScheme.Light)
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
        await _page.GotoAsync("/mixed-values-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(4);
    }

    private async Task SelectPairAsync(string firstId, string secondId)
    {
        await _page.Locator($".component-container[data-d12-entity='{firstId}']").ClickAsync();
        await _page
            .Locator($".component-container[data-d12-entity='{secondId}']")
            .ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(_page.Locator(".component-container.selected")).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task TwoRectanglesShowAHatchedFillAndAMixedStrokeWidth_MatchesBaseline()
    {
        await NewPageAsync();
        await SelectPairAsync(BlueRectangleId, OrangeRectangleId);

        await Expect(_page.Locator(".d12-property-panel-color-mixed")).ToHaveCountAsync(1);
        await Expect(_page.Locator("#d12-property-panel-field-StrokeWidth"))
            .ToHaveAttributeAsync("placeholder", "Mixed");

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task TwoTextsShowMixedWeightTiles_MatchesBaseline()
    {
        await NewPageAsync();
        await SelectPairAsync(NormalTextId, BoldTextId);

        await ExpectMixedWeightTilesAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task TwoTextsShowMixedWeightTiles_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await SelectPairAsync(NormalTextId, BoldTextId);

        await ExpectMixedWeightTilesAsync();

        await ContentSnapshot.Verify(_page);
    }

    private async Task ExpectMixedWeightTilesAsync()
    {
        var weight = _page.Locator("#d12-property-panel-field-FontWeight");
        await Expect(weight.Locator("[role=radio]")).ToHaveCountAsync(2);
        await Expect(weight.Locator("[aria-checked=true]")).ToHaveCountAsync(0);
        await Expect(_page.Locator(".d12-property-panel-mixed-tag")).ToHaveTextAsync("Mixed");
    }
}
