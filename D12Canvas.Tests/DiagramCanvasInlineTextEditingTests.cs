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

// Sticky Note/Text edit their own content inline, WYSIWYG, on the canvas - committing on blur
// records exactly one MutateEntityCommand; Escape cancels with no history entry at all. Exercised
// here through the real DiagramCanvas/ComponentContainer/StickyNote stack, since that's where the
// ParentCanvas/InstanceId cascading parameters that route the commit back to Board/History
// actually get populated.
public class DiagramCanvasInlineTextEditingTests : ComponentTestBase
{
    public DiagramCanvasInlineTextEditingTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();

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

    private static void DoublePressTheNote(IRenderedComponent<DiagramCanvas> canvas)
    {
        canvas.ClickOn(canvas.Find(".component-container"));
        canvas.ClickOn(canvas.Find(".component-container"), pressCount: 2);
    }

    [Fact]
    public async Task BlurAfterEditingCommitsOneMutateEntityCommand()
    {
        var board = new Board();
        var instance = AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        DoublePressTheNote(canvas);
        var editor = canvas.Find("textarea.d12-sticky-note-editor");
        editor.Input("Edited");
        editor.Blur();

        Assert.Equal("Edited", ((StickyNoteProps)instance.Props).Text);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("Original", ((StickyNoteProps)instance.Props).Text);
    }

    // ComponentContainer.ShouldRender() previously only compared Bounds/selection, so an
    // in-place Props edit at unchanged Bounds never reached the nested built-in - the Board was
    // correct but the screen stayed stale. Fixed by comparing Props too.
    [Fact]
    public void BlurAfterEditingUpdatesTheRenderedText()
    {
        var board = new Board();
        AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        DoublePressTheNote(canvas);
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
        DoublePressTheNote(canvas);
        canvas.Find("textarea.d12-sticky-note-editor").Input("Edited");
        canvas.Find("textarea.d12-sticky-note-editor").Blur();

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Contains("Original", canvas.Find("p.d12-sticky-note-text").TextContent);
    }

    [Fact]
    public void ADoublePressOnAStickyNoteOpensItsEditor()
    {
        var board = new Board();
        AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickOn(canvas.Find(".component-container"));
        canvas.ClickOn(canvas.Find(".component-container"), pressCount: 2);

        var editor = canvas.Find("textarea.d12-sticky-note-editor");
        Assert.Equal("Original", editor.GetAttribute("value"));
    }

    [Fact]
    public void ADoublePressOnAGroupedStickyNoteOpensNoEditor()
    {
        var board = new Board();
        var note = AddStickyNote(board, "Original");
        var other = AddStickyNote(board, "Other");
        board.AddGroup(new Group([note.Id, other.Id]));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var container = canvas.Find($".component-container[data-d12-entity='{note.Id}']");
        canvas.ClickOn(container);
        canvas.ClickOn(container, pressCount: 2);

        Assert.Empty(canvas.FindAll("textarea"));
    }

    [Fact]
    public async Task RedoAfterUndoingATextEditReappliesTheEdit()
    {
        var board = new Board();
        var instance = AddStickyNote(board, "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        DoublePressTheNote(canvas);
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
        canvas.ClickOn(canvas.Find(".component-container"));
        var container = canvas.Find(".component-container");
        canvas.DragOn(container, (100, 100), (150, 120));
        Assert.NotEqual(new Bounds(0, 0, 200, 200), instance.Bounds);

        DoublePressTheNote(canvas);
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

        canvas.ClickOn(canvas.Find(".component-container"));
        var container = canvas.Find(".component-container");
        canvas.DragOn(container, (100, 100), (150, 120));
        Assert.NotEqual(new Bounds(0, 0, 200, 200), instance.Bounds);

        DoublePressTheNote(canvas);
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
        DoublePressTheNote(canvas);
        canvas.Find("textarea.d12-sticky-note-editor").Input("Persisted text");
        canvas.Find("textarea.d12-sticky-note-editor").Blur();

        var serializer = new BoardJsonSerializer(Services.GetRequiredService<IComponentRegistry>());
        var json = serializer.Serialize(board);
        var reloaded = serializer.Deserialize(json);

        var reloadedInstance = Assert.Single(reloaded.Components);
        Assert.Equal("Persisted text", ((StickyNoteProps)reloadedInstance.Props).Text);
    }
}
