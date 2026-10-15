using Microsoft.Playwright;
using Xunit;
using static D12Canvas.VisualTests.PropertyBarVisualTests;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The property bar driven by real input on the light pane of the property bar demo: absent through
// a drag and while a menu is open, chrome hidden only while the pointer is on it, Ctrl+Enter in,
// arrows across and Escape back to the shape, and a recolour that shows on the edge at once.
public sealed class PropertyBarProbes : IAsyncLifetime
{
    private const string Theme = "light";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public PropertyBarProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
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

    private ILocator Bar => Pane(_page, Theme).Locator(".d12-property-bar");

    private ILocator Blue => Shape(_page, Theme, BlueRectangleId);

    private Task<string> FocusedLabelAsync() =>
        _page.EvaluateAsync<string>(
            "() => document.activeElement?.getAttribute('aria-label') ?? ''"
        );

    [Fact]
    public async Task TheBarIsAbsentThroughADragAndReturnsAfterTheRelease()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);
        var box = (await Blue.BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        await _page.Mouse.MoveAsync(x, y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(x + 30, y + 30, new() { Steps = 3 });
        await Expect(Bar).ToHaveCountAsync(0);

        await _page.Mouse.UpAsync();
        await Expect(Bar).ToBeVisibleAsync();
    }

    [Fact]
    public async Task TheBarIsAbsentWhileAMenuIsOpenAndReturnsWhenItCloses()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);

        await Blue.ClickAsync(new() { Button = MouseButton.Right });
        await Expect(Pane(_page, Theme).Locator(".d12-context-menu")).ToBeVisibleAsync();
        await Expect(Bar).ToHaveCountAsync(0);

        await _page.Keyboard.PressAsync("Escape");
        await Expect(Pane(_page, Theme).Locator(".d12-context-menu")).ToHaveCountAsync(0);
        await Expect(Bar).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HoveringTheBarHidesTheOutlineHandlesAndPortsAndLeavingRestoresThem()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);
        var handle = Blue.Locator(".resize-handle").First;
        await Expect(handle).ToBeVisibleAsync();

        await Bar.Locator(".d12-property-bar-cell").First.HoverAsync();

        await Expect(handle).ToBeHiddenAsync();
        await Expect(Blue.Locator(".port").First).ToBeHiddenAsync();
        Assert.Equal(
            "rgba(0, 0, 0, 0)",
            await Blue.EvaluateAsync<string>("el => getComputedStyle(el).outlineColor")
        );

        await _page.Mouse.MoveAsync(5, 5);

        await Expect(handle).ToBeVisibleAsync();
        Assert.NotEqual(
            "rgba(0, 0, 0, 0)",
            await Blue.EvaluateAsync<string>("el => getComputedStyle(el).outlineColor")
        );
    }

    [Fact]
    public async Task HoveringAnEdgesBarHidesItsHalo()
    {
        await SelectEdgeAsync(_page, Theme);
        var halo = Pane(_page, Theme).Locator(".edge-halo");

        await Bar.Locator(".d12-property-bar-cell").First.HoverAsync();
        await Expect(halo).ToBeHiddenAsync();

        await _page.Mouse.MoveAsync(5, 5);
        await Expect(halo).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CtrlEnterFocusesTheFirstGlyphArrowsRoveAndEscapeReturnsToTheShape()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);
        await Blue.FocusAsync();

        await _page.Keyboard.PressAsync("Control+Enter");
        Assert.Equal("Fill", await FocusedLabelAsync());

        await _page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("Stroke", await FocusedLabelAsync());
        await _page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("Stroke width", await FocusedLabelAsync());
        await _page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("Fill", await FocusedLabelAsync());
        await _page.Keyboard.PressAsync("ArrowLeft");
        Assert.Equal("Stroke width", await FocusedLabelAsync());

        var before = await Blue.GetAttributeAsync("style");
        await _page.Keyboard.PressAsync("Escape");

        Assert.Equal(
            BlueRectangleId,
            await _page.EvaluateAsync<string>(
                "() => document.activeElement?.getAttribute('data-d12-entity') ?? ''"
            )
        );
        await Expect(Blue).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("selected"));
        Assert.Equal(before, await Blue.GetAttributeAsync("style"));
    }

    [Fact]
    public async Task CtrlEnterAfterAClickReachesTheBarAndEscapeFocusesTheSelectedShape()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);

        await _page.Keyboard.PressAsync("Control+Enter");
        Assert.Equal("Fill", await FocusedLabelAsync());

        await _page.Keyboard.PressAsync("Escape");
        Assert.Equal(
            BlueRectangleId,
            await _page.EvaluateAsync<string>(
                "() => document.activeElement?.getAttribute('data-d12-entity') ?? ''"
            )
        );
    }

    [Fact]
    public async Task DeleteOnAColourGlyphReturnsItToTheThemeAndLeavesTheEdgeOnTheBoard()
    {
        await SelectEdgeAsync(_page, Theme);
        var line = Pane(_page, Theme).Locator(".edge-line");
        await _page.Keyboard.PressAsync("Control+Enter");
        for (var step = 0; step < 3; step++)
        {
            await _page.Keyboard.PressAsync("ArrowRight");
        }

        Assert.Equal("Colour", await FocusedLabelAsync());
        await _page.Keyboard.PressAsync("Delete");

        await Expect(Bar.Locator(".d12-property-bar-swatch-themed-stroke")).ToHaveCountAsync(1);
        await Expect(line)
            .Not.ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("#e5246b"));
        await Expect(Pane(_page, Theme).Locator(".edge-line")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ARecolourFromTheBarShowsOnTheEdgeAtOnceAndUndoesInOneStep()
    {
        await SelectEdgeAsync(_page, Theme);
        var line = Pane(_page, Theme).Locator(".edge-line");

        await Bar.Locator("input[aria-label='Colour']").FillAsync("#2ecc71");

        await Expect(line)
            .ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("#2ecc71"));
        Assert.Equal(
            "rgb(46, 204, 113)",
            await line.EvaluateAsync<string>("el => getComputedStyle(el).stroke")
        );

        await Pane(_page, Theme).Locator(".diagram-canvas").FocusAsync();
        await _page.Keyboard.PressAsync("Control+z");
        await Expect(line)
            .ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("#e5246b"));
    }

    // A wheel moves the viewport under a visible bar. The bar hides while it is being moved and
    // shows again where the selection now is once the wheel has stopped.
    [Fact]
    public async Task AWheelHidesTheBarWhileTheViewMovesAndShowsItAgainWhenItStops()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);
        await Expect(Bar).ToBeVisibleAsync();
        await Bar.EvaluateAsync(
            """
            bar => {
                window.__d12BarSettled = [];
                new MutationObserver(() =>
                    window.__d12BarSettled.push(bar.classList.contains("d12-property-bar-settling"))
                ).observe(bar, { attributes: true, attributeFilter: ["class"] });
            }
            """
        );
        var before = await Bar.GetAttributeAsync("style");
        var container = (
            await Pane(_page, Theme).Locator(".diagram-container").BoundingBoxAsync()
        )!;

        await _page.Mouse.MoveAsync(container.X + 20, container.Y + container.Height - 20);
        await _page.Mouse.WheelAsync(0, 300);
        await _page.Mouse.WheelAsync(0, 300);

        await Expect(Bar)
            .Not.ToHaveClassAsync(
                new System.Text.RegularExpressions.Regex("d12-property-bar-settling")
            );
        await Expect(Bar).ToBeVisibleAsync();
        Assert.NotEqual(before, await Bar.GetAttributeAsync("style"));
        Assert.Equal(
            [true, false],
            (await _page.EvaluateAsync<bool[]>("() => window.__d12BarSettled")).Distinct()
        );
    }
}
