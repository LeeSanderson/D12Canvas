using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A sticky note on a dark board, opened for editing by a double-press, its text all selected.
public sealed class InlineEditVisualTests : IAsyncLifetime
{
    private const string NoteId = "1d170000-0000-0000-0000-000000000001";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public InlineEditVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/inline-edit-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(4);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task AnOpenEditorOnADarkBoard_MatchesBaseline()
    {
        await _page.Locator($".component-container[data-d12-entity='{NoteId}']").DblClickAsync();

        var editor = _page.Locator("textarea.d12-sticky-note-editor");
        await Expect(editor).ToBeFocusedAsync();
        await Expect(editor).ToHaveValueAsync("Plan the launch");

        await ContentSnapshot.Verify(_page);
    }
}
