using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The property panel's glyph tiles driven by real keys on the property panel demo, with a sticky
// note selected. The panel is narrowed until the text align tiles wrap, so the arrow keys have to
// carry real focus onto the second row and back round to the first tile without committing a
// value; Enter and Space then commit the focused tile.
public sealed class PropertyPanelTileProbes : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public PropertyPanelTileProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/property-panel-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(8);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator AlignTiles =>
        _page
            .GetByRole(AriaRole.Radiogroup, new() { Name = "TextAlign", Exact = true })
            .GetByRole(AriaRole.Radio);

    private ILocator NoteText => _page.Locator(".d12-sticky-note-text");

    [Fact]
    public async Task ArrowKeysMoveRealFocusAcrossWrappedTilesWithoutCommitting()
    {
        await _page.Locator(".d12-palette-entry[aria-label='Sticky Note']").ClickAsync();
        await _page.Locator(".component-container").ClickAsync();
        await _page.Locator(".panel-slot").EvaluateAsync("slot => slot.style.width = '150px'");
        await Expect(AlignTiles).ToHaveCountAsync(3);
        await Expect(AlignTiles.Nth(1)).ToHaveAttributeAsync("aria-checked", "true");

        var firstRow = (await AlignTiles.Nth(0).BoundingBoxAsync())!.Y;
        var lastRow = (await AlignTiles.Nth(2).BoundingBoxAsync())!.Y;
        Assert.True(lastRow > firstRow, "The third tile should wrap onto a second row.");

        await AlignTiles.Nth(1).FocusAsync();
        await _page.Keyboard.PressAsync("ArrowRight");
        await Expect(AlignTiles.Nth(2)).ToBeFocusedAsync();
        await Expect(AlignTiles.Nth(1)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(AlignTiles.Nth(2)).ToHaveAttributeAsync("aria-checked", "false");

        await _page.Keyboard.PressAsync("ArrowRight");
        await Expect(AlignTiles.Nth(0)).ToBeFocusedAsync();

        await _page.Keyboard.PressAsync("ArrowLeft");
        await Expect(AlignTiles.Nth(2)).ToBeFocusedAsync();
        await Expect(AlignTiles.Nth(1)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(NoteText).ToHaveAttributeAsync("style", new Regex("text-align: center"));

        await _page.Keyboard.PressAsync("Enter");
        await Expect(AlignTiles.Nth(2)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(NoteText).ToHaveAttributeAsync("style", new Regex("text-align: right"));
        await Expect(AlignTiles.Nth(2)).ToBeFocusedAsync();

        await _page.Keyboard.PressAsync("ArrowLeft");
        await Expect(AlignTiles.Nth(1)).ToBeFocusedAsync();
        await _page.Keyboard.PressAsync("Space");
        await Expect(AlignTiles.Nth(1)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(NoteText).ToHaveAttributeAsync("style", new Regex("text-align: center"));
    }
}
