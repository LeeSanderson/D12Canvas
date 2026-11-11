using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Nine sticky notes, one per pairing of horizontal and vertical alignment, beside a short note
// whose text runs past its bottom edge.
public sealed class StickyNoteAlignmentVisualTests : IAsyncLifetime
{
    private const string OverflowNoteId = "a1170000-0000-0000-0000-000000000099";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public StickyNoteAlignmentVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/sticky-note-alignment-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(10);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Note(string id) =>
        _page.Locator($".component-container[data-d12-entity='{id}']");

    private static string GridNoteId(int row, int column) =>
        $"a1170000-0000-0000-0000-0000000000{row}{column}";

    [Fact]
    public async Task EveryAlignmentAndAnOverflowingNote_MatchesBaseline()
    {
        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task OverflowingTextStartsAtTheNotesTopEdge()
    {
        var note = await Note(OverflowNoteId).BoundingBoxAsync();
        var text = await Note(OverflowNoteId).Locator("p.d12-sticky-note-text").BoundingBoxAsync();

        Assert.True(text!.Height > note!.Height);
        Assert.InRange(text.Y - note.Y, 0, note.Height / 4);
    }

    [Fact]
    public async Task StartingAndEndingAnEditLeavesTheTextWhereItWas()
    {
        foreach (var id in Enumerable.Range(0, 9).Select(i => GridNoteId(i / 3, i % 3)))
        {
            var note = Note(id);
            var rendered = await note.Locator("p.d12-sticky-note-text").BoundingBoxAsync();

            await note.DblClickAsync();
            var editor = note.Locator("textarea.d12-sticky-note-editor");
            await Expect(editor).ToBeFocusedAsync();
            var editing = await editor.BoundingBoxAsync();

            await _page.Keyboard.PressAsync("Escape");
            await Expect(editor).ToHaveCountAsync(0);
            var after = await note.Locator("p.d12-sticky-note-text").BoundingBoxAsync();

            ExpectSameBox(rendered!, editing!, id);
            ExpectSameBox(rendered!, after!, id);
        }
    }

    private static void ExpectSameBox(
        LocatorBoundingBoxResult expected,
        LocatorBoundingBoxResult actual,
        string id
    )
    {
        Assert.True(
            Math.Abs(expected.X - actual.X) < 0.5
                && Math.Abs(expected.Y - actual.Y) < 0.5
                && Math.Abs(expected.Width - actual.Width) < 0.5
                && Math.Abs(expected.Height - actual.Height) < 0.5,
            $"{id}: expected {expected.X},{expected.Y} {expected.Width}x{expected.Height}, got {actual.X},{actual.Y} {actual.Width}x{actual.Height}"
        );
    }
}
