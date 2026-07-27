using System.Threading.Tasks;
using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 43: Sticky Note/Text edit their own content inline, WYSIWYG, on the canvas (ADR 0008) -
// committing on blur records exactly one MutateEntityCommand (ADR 0007); Escape cancels with no
// history entry at all. Exercised here through the real DiagramCanvas/ComponentContainer/
// StickyNote stack, since that's where the ParentCanvas/InstanceId cascading parameters that
// route the commit back to Board/History actually get populated.
public class DiagramCanvasInlineTextEditingTests : ComponentTestBase
{
    public DiagramCanvasInlineTextEditingTests()
    {
        SetupDiagramCanvasJsModule();
        JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");

        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: "sticky-note",
                ComponentType: typeof(StickyNote),
                PropsType: typeof(StickyNoteProps),
                DisplayName: "Sticky Note",
                AccessibleName: "Sticky Note",
                DefaultProps: new StickyNoteProps("", "#FFEB3B", "#000000", 14),
                Icon: null,
                Role: "group",
                DefaultSize: null,
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    private static ComponentInstance AddStickyNote(Board board, string text)
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new StickyNoteProps(text, "#FFEB3B", "#000000", 14),
            new Bounds(0, 0, 200, 200)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public async Task BlurAfterEditingCommitsOneMutateEntityCommand()
    {
        var board = new Board();
        var instance = AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find("p.d12-sticky-note-text").DoubleClick();
        var editor = canvas.Find("textarea.d12-sticky-note-editor");
        editor.Input("Edited");
        editor.Blur();

        Assert.Equal("Edited", ((StickyNoteProps)instance.Props).Text);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("Original", ((StickyNoteProps)instance.Props).Text);
    }

    // Ticket 75: ComponentContainer.ShouldRender() previously only compared Bounds/selection, so
    // an in-place Props edit at unchanged Bounds never reached the nested built-in - the Board was
    // correct but the screen stayed stale. Fixed alongside ticket 43 by comparing Props too.
    [Fact]
    public void BlurAfterEditingUpdatesTheRenderedText()
    {
        var board = new Board();
        AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find("p.d12-sticky-note-text").DoubleClick();
        canvas.Find("textarea.d12-sticky-note-editor").Input("Edited");
        canvas.Find("textarea.d12-sticky-note-editor").Blur();

        Assert.Contains("Edited", canvas.Find("p.d12-sticky-note-text").TextContent);
    }

    [Fact]
    public async Task UndoAfterEditingRevertsTheRenderedText()
    {
        var board = new Board();
        AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find("p.d12-sticky-note-text").DoubleClick();
        canvas.Find("textarea.d12-sticky-note-editor").Input("Edited");
        canvas.Find("textarea.d12-sticky-note-editor").Blur();

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Contains("Original", canvas.Find("p.d12-sticky-note-text").TextContent);
    }

    // ComponentContainer has its own unrelated legacy double-click ("SwitchToEditMode" - free
    // drag/resize without prior selection, predates the Board-backed canvas). Entering the inline
    // WYSIWYG editor must stop that double-click from also bubbling up and engaging it - otherwise
    // the container would gain both edit surfaces at once. The editor's own textarea carries the
    // identical @ondblclick:stopPropagation guard for the same reason, but bUnit's dispatch helper
    // requires a real handler (not just a stopPropagation modifier) on the element it's invoked on,
    // so that half of the guard is exercised by code review/the Playwright visual test instead.
    [Fact]
    public void DoubleClickToEnterInlineEditDoesNotAlsoEngageComponentContainersLegacyEditMode()
    {
        var board = new Board();
        AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find("p.d12-sticky-note-text").DoubleClick();

        Assert.Single(canvas.FindAll("textarea.d12-sticky-note-editor"));
        Assert.DoesNotContain("edit-mode", canvas.Find(".component-container").ClassList);
    }

    [Fact]
    public async Task RedoAfterUndoingATextEditReappliesTheEdit()
    {
        var board = new Board();
        var instance = AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find("p.d12-sticky-note-text").DoubleClick();
        canvas.Find("textarea.d12-sticky-note-editor").Input("Edited");
        canvas.Find("textarea.d12-sticky-note-editor").Blur();
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal("Edited", ((StickyNoteProps)instance.Props).Text);
    }

    [Fact]
    public async Task EscapeCancelsTheEditWithoutAddingAHistoryEntry()
    {
        var board = new Board();
        var instance = AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        // One real gesture before the cancelled edit - if Escape wrongly recorded an entry, a
        // single Undo below would revert that phantom entry instead of this move.
        canvas.Find(".component-container").Click();
        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 100, ClientY = 100 });
        container.MouseMove(new MouseEventArgs { ClientX = 150, ClientY = 120 });
        container.MouseUp(new MouseEventArgs { ClientX = 150, ClientY = 120 });
        Assert.NotEqual(new Bounds(0, 0, 200, 200), instance.Bounds);

        canvas.Find("p.d12-sticky-note-text").DoubleClick();
        var editor = canvas.Find("textarea.d12-sticky-note-editor");
        editor.Input("Discard me");
        editor.KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal("Original", ((StickyNoteProps)instance.Props).Text);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(0, 0, 200, 200), instance.Bounds);
        Assert.Equal("Original", ((StickyNoteProps)instance.Props).Text);
    }

    [Fact]
    public async Task BlurWithNoActualChangeRecordsNoHistoryEntry()
    {
        var board = new Board();
        var instance = AddStickyNote(board, "Same");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();
        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 100, ClientY = 100 });
        container.MouseMove(new MouseEventArgs { ClientX = 150, ClientY = 120 });
        container.MouseUp(new MouseEventArgs { ClientX = 150, ClientY = 120 });
        Assert.NotEqual(new Bounds(0, 0, 200, 200), instance.Bounds);

        canvas.Find("p.d12-sticky-note-text").DoubleClick();
        canvas.Find("textarea.d12-sticky-note-editor").Blur(); // no edit in between

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(0, 0, 200, 200), instance.Bounds);
    }

    [Fact]
    public void EditedTextRoundTripsThroughJsonSerialization()
    {
        var board = new Board();
        AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find("p.d12-sticky-note-text").DoubleClick();
        canvas.Find("textarea.d12-sticky-note-editor").Input("Persisted text");
        canvas.Find("textarea.d12-sticky-note-editor").Blur();

        var serializer = new BoardJsonSerializer(Services.GetRequiredService<IComponentRegistry>());
        var json = serializer.Serialize(board);
        var reloaded = serializer.Deserialize(json);

        var reloadedInstance = Assert.Single(reloaded.Components);
        Assert.Equal("Persisted text", ((StickyNoteProps)reloadedInstance.Props).Text);
    }
}
