using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The real browser paths into and out of an inline edit: a double-press, F2, the palette's
// click, Enter and drop, Escape and blur.
public sealed class InlineEditProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string NoteId = "1d170000-0000-0000-0000-000000000001";
    private const string TextId = "1d170000-0000-0000-0000-000000000002";
    private const string RectangleId = "1d170000-0000-0000-0000-000000000003";
    private const string EdgeNoteId = "1d170000-0000-0000-0000-000000000004";

    private const string FocusedEditorSelection = """
        () => {
            const editor = document.activeElement;
            return editor instanceof HTMLTextAreaElement
                ? `${editor.selectionStart}-${editor.selectionEnd}/${editor.value.length}`
                : `not an editor: ${editor?.tagName}`;
        }
        """;

    private async Task ExpectAllTextSelectedAsync()
    {
        var length = (await Editor.InputValueAsync()).Length;
        var expected = $"0-{length}/{length}";
        var actual = "";
        for (var attempt = 0; attempt < 50 && actual != expected; attempt++)
        {
            actual = await Page.EvaluateAsync<string>(FocusedEditorSelection);
            await Task.Delay(100);
        }

        Assert.Equal(expected, actual);
    }

    protected override string ProbePagePath => "/inline-edit-demo";

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private ILocator Editor => Page.Locator("textarea");

    private Task<string?> FocusedEntityAsync() =>
        Page.EvaluateAsync<string?>(
            "() => document.activeElement?.getAttribute('data-d12-entity') ?? null"
        );

    [Fact]
    public async Task ADoublePressOnAText_OpensItsEditorFocusedWithAllTextSelected()
    {
        await Instance(TextId).DblClickAsync();

        await Expect(Editor).ToBeFocusedAsync();
        await Expect(Editor).ToHaveValueAsync("Release notes");
        await ExpectAllTextSelectedAsync();
    }

    [Fact]
    public async Task F2OnAFocusedNote_OpensItsEditorAndTypingReplacesTheText()
    {
        await Instance(NoteId).FocusAsync();
        await Page.Keyboard.PressAsync("F2");

        await Expect(Editor).ToBeFocusedAsync();
        await ExpectAllTextSelectedAsync();

        await Page.Keyboard.TypeAsync("Ship it");
        await Expect(Editor).ToHaveValueAsync("Ship it");
    }

    [Fact]
    public async Task F2OnAFocusedRectangle_OpensNothing()
    {
        await Instance(RectangleId).FocusAsync();
        await Page.Keyboard.PressAsync("F2");
        await SettleAsync();

        await Expect(Editor).ToHaveCountAsync(0);
        Assert.Equal(RectangleId, await FocusedEntityAsync());
    }

    [Fact]
    public async Task EscapeInTheEditor_CommitsAndLeavesFocusOnTheShapesSelectedStop()
    {
        await Instance(NoteId).FocusAsync();
        await Page.Keyboard.PressAsync("F2");
        await Expect(Editor).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("Shipped");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Editor).ToHaveCountAsync(0);
        await Expect(Instance(NoteId).Locator("p.d12-sticky-note-text")).ToHaveTextAsync("Shipped");
        await Expect(Instance(NoteId)).ToBeFocusedAsync();
        await Expect(Instance(NoteId)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task ClickingEmptyCanvas_CommitsTheEdit()
    {
        await Instance(TextId).DblClickAsync();
        await Expect(Editor).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("Draft");

        var empty = await PagePointOnCanvasAsync(560, 40);
        await Page.Mouse.ClickAsync(empty.X, empty.Y);

        await Expect(Editor).ToHaveCountAsync(0);
        await Expect(Instance(TextId).Locator("p.d12-text")).ToHaveTextAsync("Draft");
    }

    [Fact]
    public async Task ADoublePressOnANoteHalfOffScreen_PansItFullyIntoViewBeforeEditing()
    {
        // The board opens fitted, so the note is first pushed off the right edge by a pan.
        var above = await PagePointOnCanvasAsync(300, 10);
        await Page.Mouse.ClickAsync(above.X, above.Y);
        await Page.Keyboard.PressAsync("ArrowLeft");
        await SettleAsync();
        var container = await Page.Locator(".diagram-container").BoundingBoxAsync();
        var before = await Instance(EdgeNoteId).BoundingBoxAsync();
        Assert.True(before!.X + before.Width > container!.X + container.Width);

        await Instance(EdgeNoteId).DblClickAsync();
        await Expect(Editor).ToBeFocusedAsync();
        await SettleAsync();

        var after = await Instance(EdgeNoteId).BoundingBoxAsync();
        Assert.True(after!.X >= container.X);
        Assert.True(after.X + after.Width <= container.X + container.Width);
        Assert.Equal(before.Width, after.Width, 0.5);
    }

    [Fact]
    public async Task ClickingThePalettesTextEntry_OpensTheNewTextForTyping()
    {
        await Page.Locator("button[aria-label='Text']").ClickAsync();

        await Expect(Page.Locator(".component-container")).ToHaveCountAsync(5);
        await Expect(Page.Locator("textarea.d12-text-editor")).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("Fresh");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("p.d12-text", new() { HasText = "Fresh" })).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ATextPlacedAndAbandonedByClickingEmptyCanvas_IsRemoved()
    {
        await Page.Locator("button[aria-label='Text']").ClickAsync();
        await Expect(Page.Locator(".component-container")).ToHaveCountAsync(5);
        await Expect(Page.Locator("textarea.d12-text-editor")).ToBeFocusedAsync();

        var empty = await PagePointOnCanvasAsync(560, 40);
        await Page.Mouse.ClickAsync(empty.X, empty.Y);

        await Expect(Page.Locator(".component-container")).ToHaveCountAsync(4);
        await Expect(Editor).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ATextPlacedAndAbandonedWithEscape_IsRemovedAndFocusGoesToTheCanvas()
    {
        await Page.Locator("button[aria-label='Text']").ClickAsync();
        await Expect(Page.Locator("textarea.d12-text-editor")).ToBeFocusedAsync();
        await Page.Keyboard.TypeAsync("   ");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator(".component-container")).ToHaveCountAsync(4);
        await Expect(Page.Locator(".diagram-canvas")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task ClearingAnExistingTextAndPressingEscape_RemovesItAndCtrlZBringsItBack()
    {
        await Instance(TextId).DblClickAsync();
        await Expect(Editor).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Delete");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Instance(TextId)).ToHaveCountAsync(0);

        await Page.Keyboard.PressAsync("Control+z");
        await Expect(Instance(TextId).Locator("p.d12-text")).ToHaveTextAsync("Release notes");
    }

    [Fact]
    public async Task EnterOnThePalettesStickyNoteEntry_OpensTheNewNoteForTyping()
    {
        await Page.Locator("button[aria-label='Sticky Note']").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.Locator(".component-container")).ToHaveCountAsync(5);
        await Expect(Page.Locator("textarea.d12-sticky-note-editor")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task DroppingAStickyNoteFromThePalette_OpensItForTyping()
    {
        await Page.Locator("button[aria-label='Sticky Note']")
            .DragToAsync(
                Page.Locator(".diagram-canvas"),
                new LocatorDragToOptions
                {
                    TargetPosition = new TargetPosition { X = 160, Y = 380 },
                }
            );

        await Expect(Page.Locator(".component-container")).ToHaveCountAsync(5);
        await Expect(Page.Locator("textarea.d12-sticky-note-editor")).ToBeFocusedAsync();
    }
}
