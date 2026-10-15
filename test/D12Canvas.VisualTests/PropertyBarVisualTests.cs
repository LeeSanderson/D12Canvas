using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The property bar demo on a light and a dark pane: a selected rectangle's bar centred above it, a
// mixed fill across two rectangles, a selected edge's bar, a shape along the top whose bar slides
// along the container's edge, and the selection chrome hidden while the pointer is on a bar.
public sealed class PropertyBarVisualTests : IAsyncLifetime
{
    internal const string TopRectangleId = "b0a00000-0000-0000-0000-000000000001";
    internal const string BlueRectangleId = "b0a00000-0000-0000-0000-000000000002";
    internal const string OrangeRectangleId = "b0a00000-0000-0000-0000-000000000003";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public PropertyBarVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
                ViewportSize = new ViewportSize { Width = 1200, Height = 640 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/property-bar-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(6);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    internal static ILocator Pane(IPage page, string theme) =>
        page.Locator($".theme-pane[data-d12-theme='{theme}']");

    internal static ILocator Shape(IPage page, string theme, string id) =>
        Pane(page, theme).Locator($".component-container[data-d12-entity='{id}']");

    internal static async Task SelectAsync(IPage page, string theme, params string[] ids)
    {
        await Shape(page, theme, ids[0]).ClickAsync();
        foreach (var id in ids.Skip(1))
        {
            await Shape(page, theme, id).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        }

        await Expect(Pane(page, theme).Locator(".component-container.selected"))
            .ToHaveCountAsync(ids.Length);
        await Expect(Pane(page, theme).Locator(".d12-property-bar")).ToBeVisibleAsync();
    }

    internal static async Task SelectEdgeAsync(IPage page, string theme)
    {
        var point = await Pane(page, theme)
            .Locator(".edge-hit")
            .EvaluateAsync<double[]>(
                @"el => {
                    const p = el.getPointAtLength(el.getTotalLength() / 2);
                    const m = el.getScreenCTM();
                    return [p.x * m.a + m.e, p.y * m.d + m.f];
                }"
            );
        await page.Mouse.ClickAsync((float)point[0], (float)point[1]);
        await Expect(Pane(page, theme).Locator(".edge-halo")).ToHaveCountAsync(1);
        await Expect(Pane(page, theme).Locator(".d12-property-bar")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ASelectedRectangleShowsItsBarOnBothThemes_MatchesBaseline()
    {
        await SelectAsync(_page, "light", BlueRectangleId);
        await SelectAsync(_page, "dark", BlueRectangleId);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task TwoRectanglesInDifferentFillsShowAHatchedFillGlyph_MatchesBaseline()
    {
        await SelectAsync(_page, "light", BlueRectangleId, OrangeRectangleId);
        await SelectAsync(_page, "dark", BlueRectangleId, OrangeRectangleId);

        await Expect(_page.Locator(".d12-property-bar-swatch-mixed-fill")).ToHaveCountAsync(2);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task ASelectedEdgeShowsRoutingArrowsAndColour_MatchesBaseline()
    {
        await SelectEdgeAsync(_page, "light");
        await SelectEdgeAsync(_page, "dark");

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task AShapeAlongTheTopSlidesItsBarAlongTheEdge_MatchesBaseline()
    {
        await SelectAsync(_page, "light", TopRectangleId);
        await SelectAsync(_page, "dark", TopRectangleId);

        var container = await Pane(_page, "light").Locator(".diagram-container").BoundingBoxAsync();
        var bar = await Pane(_page, "light").Locator(".d12-property-bar").BoundingBoxAsync();
        var shape = await Shape(_page, "light", TopRectangleId).BoundingBoxAsync();
        Assert.True(
            bar!.Y < shape!.Y + shape.Height,
            "The bar should overlap the shape it sits over."
        );
        Assert.True(bar.Y >= container!.Y, "The bar should stay inside the container.");

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task HoveringTheLightBarHidesItsSelectionChromeAndOnlyItsOwn_MatchesBaseline()
    {
        await HoverOneBarAsync("light", "dark");

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task HoveringTheDarkBarHidesItsSelectionChromeAndOnlyItsOwn_MatchesBaseline()
    {
        await HoverOneBarAsync("dark", "light");

        await ContentSnapshot.Verify(_page);
    }

    private async Task HoverOneBarAsync(string hovered, string other)
    {
        await SelectAsync(_page, "light", BlueRectangleId, OrangeRectangleId);
        await SelectAsync(_page, "dark", BlueRectangleId, OrangeRectangleId);

        await Pane(_page, hovered).Locator(".d12-property-bar-cell").First.HoverAsync();

        await Expect(Pane(_page, hovered).Locator(".selection-bounding-box")).ToBeHiddenAsync();
        await Expect(Pane(_page, other).Locator(".selection-bounding-box")).ToBeVisibleAsync();
    }
}
