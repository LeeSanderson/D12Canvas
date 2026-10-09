using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Who owns a menu request on an instance's content, the browser or the canvas, is decided once: at the
// secondary press, or at the menu key's keydown, and the browser's own contextmenu that follows
// uses that verdict without looking at its target. A native menu cannot be seen in the DOM, so
// each probe reads whether the contextmenu was prevented, and whether the object menu opened.
public sealed class MenuVerdictProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string SourceId = "a0000000-0000-0000-0000-000000000001";
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";
    private const string NoteId = "a0000000-0000-0000-0000-000000000004";

    private const string Fill =
        "display: block; width: 100%; height: 100%; box-sizing: border-box; margin: 0;";

    private const string Pixel = "data:image/gif;base64,R0lGODlhAQABAAAAACw=";

    private static readonly LocatorClickOptions RightClick = new() { Button = MouseButton.Right };

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private ILocator Menu => Page.Locator(".d12-context-menu");

    public static TheoryData<string, string, bool> FiveRules() =>
        new()
        {
            { "an editable target", $"<textarea style='{Fill}'></textarea>", true },
            {
                "an editable target under a canvas marker",
                $"<div data-d12-context-menu='canvas' style='{Fill}'><textarea style='{Fill}'></textarea></div>",
                true
            },
            {
                "an image marked for the browser",
                $"<img alt='' src='{Pixel}' data-d12-context-menu='browser' style='{Fill}'>",
                true
            },
            {
                "marked content marked for the browser",
                "<div data-d12-author-content data-d12-context-menu='browser'>card</div>",
                true
            },
            {
                "a link under a canvas marker",
                $"<a href='#bookmark' data-d12-context-menu='canvas' style='{Fill}'>bookmark</a>",
                false
            },
            { "a link", $"<a href='#elsewhere' style='{Fill}'>link</a>", true },
            { "a video", $"<video style='{Fill} background: #000;'></video>", true },
            { "an audio element", $"<audio controls style='{Fill}'></audio>", true },
            { "an image", $"<img alt='' src='{Pixel}' style='{Fill}'>", false },
            { "plain content", "<div>plain</div>", false },
            { "plain marked content", "<div data-d12-author-content>plain</div>", false },
        };

    [Theory]
    [MemberData(nameof(FiveRules))]
    public async Task ASecondaryPressOnAnInstancesContentFollowsTheFiveRules(
        string content,
        string markup,
        bool browserKeepsIt
    )
    {
        await InjectIntoTargetAsync(markup);
        await RecordContextMenusAsync();
        var inner = Page.Locator(".probe-content > *");
        var pressed = await inner.CountAsync() > 0 ? inner.First : Page.Locator(".probe-content");

        await pressed.ClickAsync(RightClick);
        await SettleAsync();

        var recorded = await RecordedContextMenusAsync();
        Assert.True(
            recorded.SequenceEqual([!browserKeepsIt]),
            $"{content}: contextmenu prevented [{string.Join(", ", recorded)}]"
        );
        await Expect(Menu).ToHaveCountAsync(browserKeepsIt ? 0 : 1);
    }

    [Theory]
    [InlineData("<a href='#elsewhere'>link</a>", "author-content", "browser")]
    [InlineData("<div data-d12-author-content>plain</div>", "author-content", "canvas")]
    [InlineData("<div data-d12-context-menu='browser'>card</div>", "instance", "browser")]
    public async Task ASecondaryPressCarriesItsVerdictToTheCanvas(
        string markup,
        string role,
        string verdict
    )
    {
        await InjectIntoTargetAsync(markup);

        await Page.Locator(".probe-content").ClickAsync(RightClick);
        await SettleAsync();

        var press = await SinglePressAsync();
        Assert.Equal(role, press.GetProperty("role").GetString());
        Assert.Equal(verdict, press.GetProperty("menuVerdict").GetString());
        Assert.Equal(verdict == "browser" ? 0 : 1, (await CallsToAsync("OnPointerReleased")).Count);
    }

    [Fact]
    public async Task ALiveTextSelectionInTheContentKeepsTheBrowsersMenu()
    {
        await InjectIntoTargetAsync("<div data-d12-author-content>selected words</div>");
        await Page.EvaluateAsync(
            """
            () => {
                const range = document.createRange();
                range.selectNodeContents(document.querySelector(".probe-content"));
                const selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(range);
            }
            """
        );
        await RecordContextMenusAsync();

        await Page.Locator(".probe-content").ClickAsync(RightClick);
        await SettleAsync();

        Assert.Equal(new[] { false }, await RecordedContextMenusAsync());
        await Expect(Menu).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ASecondaryPressOnAStickyNotesTextAtRestOpensTheObjectMenu()
    {
        await RecordContextMenusAsync();

        await Instance(NoteId).Locator(".d12-sticky-note-text").ClickAsync(RightClick);
        await SettleAsync();

        Assert.Equal(new[] { true }, await RecordedContextMenusAsync());
        await Expect(Menu).ToHaveCountAsync(1);
        await Expect(Instance(NoteId)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task ASecondaryPressInsideAnOpenStickyNoteEditorKeepsTheBrowsersMenu()
    {
        var editor = await OpenNoteEditorAsync();
        await RecordContextMenusAsync();

        await editor.ClickAsync(RightClick);
        await SettleAsync();

        Assert.Equal(new[] { false }, await RecordedContextMenusAsync());
        await Expect(Menu).ToHaveCountAsync(0);
        await Expect(editor).ToBeFocusedAsync();
    }

    [Theory]
    [InlineData("ContextMenu")]
    [InlineData("Shift+F10")]
    public async Task AMenuKeyOnAFocusedShapeOpensExactlyOneMenuOnItsFirstRow(string key)
    {
        await Instance(SourceId).FocusAsync();
        await Expect(Instance(SourceId)).ToHaveAttributeAsync("aria-selected", "true");
        await RecordContextMenusAsync();

        await Page.Keyboard.PressAsync(key);
        await SettleAsync();

        await Expect(Menu).ToHaveCountAsync(1);
        await Expect(Menu.Locator(".d12-context-menu-item").First).ToBeFocusedAsync();
        Assert.Single(await CallsToAsync("OnContextMenuKeyPressed"));
        var trailing = await RecordedContextMenusAsync();
        Assert.True(trailing.Length <= 1, $"{trailing.Length} contextmenu events for one key");
        Assert.All(trailing, Assert.True);
    }

    [Fact]
    public async Task ShiftF10OpensTheMenuAtTheShapesBoxAndClosingReturnsFocusToItsStop()
    {
        // The board is panned up first so the whole menu fits below the shape.
        var empty = await EmptyCanvasPointAsync();
        await Page.Mouse.ClickAsync(empty.X, empty.Y);
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await SettleAsync();
        await Instance(SourceId).FocusAsync();
        var shape = await Instance(SourceId).BoundingBoxAsync();
        Assert.NotNull(shape);

        await Page.Keyboard.PressAsync("Shift+F10");
        await Expect(Menu).ToBeVisibleAsync();
        var menu = await Menu.BoundingBoxAsync();
        Assert.NotNull(menu);
        Assert.InRange(menu!.X, shape!.X - 1, shape.X + 1);
        Assert.InRange(menu.Y, shape.Y + shape.Height - 1, shape.Y + shape.Height + 1);

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Menu).ToHaveCountAsync(0);
        await Expect(Instance(SourceId)).ToBeFocusedAsync();
        await Expect(Instance(SourceId)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task ShiftF10WithNothingSelectedOpensTheCanvasMenu()
    {
        await RecordContextMenusAsync();

        await Page.Keyboard.PressAsync("Shift+F10");

        await Expect(Menu).ToHaveAttributeAsync("aria-label", "Canvas actions");
        await SettleAsync();
        Assert.All(await RecordedContextMenusAsync(), Assert.True);
    }

    [Fact]
    public async Task TheContextMenuKeyInAnOpenStickyNoteEditorKeepsTheBrowsersMenu()
    {
        var editor = await OpenNoteEditorAsync();
        await RecordContextMenusAsync();

        await Page.Keyboard.PressAsync("ContextMenu");
        await SettleAsync();

        Assert.Equal(new[] { false }, await RecordedContextMenusAsync());
        await Expect(Menu).ToHaveCountAsync(0);
        await Expect(editor).ToBeFocusedAsync();
    }

    [Fact]
    public async Task AnUnusedCanvasVerdictIsClearedByTheNextKeySoAnEditorStillGetsTheBrowsersMenu()
    {
        await Instance(SourceId).FocusAsync();
        await Page.Keyboard.PressAsync("Shift+F10");
        await Expect(Menu).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Menu).ToHaveCountAsync(0);
        var editor = await OpenNoteEditorAsync();
        await RecordContextMenusAsync();

        await Page.Keyboard.PressAsync("ContextMenu");
        await SettleAsync();

        Assert.Equal(new[] { false }, await RecordedContextMenusAsync());
        await Expect(Menu).ToHaveCountAsync(0);
        await Expect(editor).ToBeFocusedAsync();
    }

    [Fact]
    public async Task AnUnusedCanvasVerdictDoesNotTakeTheHostPagesOwnMenu()
    {
        await Page.EvaluateAsync(
            """
            () => {
                const host = document.createElement("p");
                host.className = "probe-host-text";
                host.textContent = "Host page text";
                document.body.append(host);
            }
            """
        );
        await Instance(SourceId).FocusAsync();
        await Page.Keyboard.PressAsync("Shift+F10");
        await Expect(Menu).ToHaveCountAsync(1);
        await RecordContextMenusAsync();

        await Page.Locator(".probe-host-text").ClickAsync(RightClick);
        await SettleAsync();

        Assert.Equal(new[] { false }, await RecordedContextMenusAsync());
    }

    private async Task<ILocator> OpenNoteEditorAsync()
    {
        await Instance(NoteId).DblClickAsync();
        var editor = Page.Locator("textarea.d12-sticky-note-editor");
        await Expect(editor).ToBeFocusedAsync();
        await SettleAsync();
        await ClearCallsAsync();
        return editor;
    }

    private Task InjectIntoTargetAsync(string markup) =>
        Page.EvaluateAsync(
            """
            ([selector, markup]) => {
                const host = document.querySelector(selector);
                host.insertAdjacentHTML("beforeend", markup);
                const content = host.lastElementChild;
                content.classList.add("probe-content");
                content.style.cssText +=
                    "position: absolute; left: 10px; top: 10px; width: 120px; height: 40px; z-index: 1;";
            }
            """,
            new[] { $"[data-d12-entity='{TargetId}'] .container-content", markup }
        );

    private Task RecordContextMenusAsync() =>
        Page.EvaluateAsync(
            """
            () => {
                window.__d12ContextMenuPrevented = [];
                window.addEventListener("contextmenu", (event) => {
                    setTimeout(() => window.__d12ContextMenuPrevented.push(event.defaultPrevented));
                }, true);
            }
            """
        );

    private async Task<bool[]> RecordedContextMenusAsync() =>
        await Page.EvaluateAsync<bool[]>("() => window.__d12ContextMenuPrevented");
}

// A member of a group that is not entered cannot be addressed, so a secondary press on a link
// inside it is the canvas's and opens the group's menu, ahead of every other rule.
public sealed class UnaddressableMenuVerdictProbes(
    PlaywrightFixture playwright,
    DemoAppFixture demoApp
) : InteractionProbe(playwright, demoApp)
{
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";

    protected override string ProbePageQuery => "?group=true";

    [Fact]
    public async Task ALinkInsideAGroupThatIsNotEnteredOpensTheGroupsMenu()
    {
        var target = Page.Locator($".component-container[data-d12-entity='{TargetId}']");
        await Expect(target).ToHaveAttributeAsync("data-d12-unaddressable", "true");
        await Page.EvaluateAsync(
            """
            (selector) => {
                const link = document.createElement("a");
                link.className = "probe-link";
                link.href = "#elsewhere";
                link.textContent = "link";
                link.style.cssText =
                    "position: absolute; left: 10px; top: 10px; width: 80px; height: 30px; z-index: 1;";
                document.querySelector(selector).appendChild(link);
                window.__d12ContextMenuPrevented = [];
                window.addEventListener("contextmenu", (event) => {
                    setTimeout(() => window.__d12ContextMenuPrevented.push(event.defaultPrevented));
                }, true);
            }
            """,
            $"[data-d12-entity='{TargetId}'] .container-content"
        );

        await Page.Locator(".probe-link").ClickAsync(new() { Button = MouseButton.Right });
        await SettleAsync();

        Assert.Equal(
            new[] { true },
            await Page.EvaluateAsync<bool[]>("() => window.__d12ContextMenuPrevented")
        );
        await Expect(Page.Locator(".d12-context-menu")).ToHaveCountAsync(1);
        Assert.Equal("instance", (await SinglePressAsync()).GetProperty("role").GetString());
    }
}
