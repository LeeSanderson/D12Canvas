using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The canvas starts every inline edit, and the editor ends it with one CommitInlineEdit call.
// Exercised through the real DiagramCanvas/ComponentContainer/built-in stack, since that is where
// the ParentCanvas and InstanceId cascading values reach the editor.
public class DiagramCanvasInlineTextEditingTests : ComponentTestBase
{
    private readonly Board _board = new();

    public DiagramCanvasInlineTextEditingTests()
    {
        SetupDiagramCanvasJsModule();

        var services = new ServiceCollection();
        services.AddD12Canvas(_ => { });
        Services.AddSingleton(
            services.BuildServiceProvider().GetRequiredService<IComponentRegistry>()
        );
    }

    private ComponentInstance AddStickyNote(string text, double x = 0, double y = 0)
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new StickyNoteProps(text, "#FFEB3B", "#000000", 14),
            new Bounds(x, y, 200, 200)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private ComponentInstance AddText(string text, double x = 0, double y = 0)
    {
        var instance = new ComponentInstance(
            "text",
            new TextProps(text, null, 16, "normal", "left"),
            new Bounds(x, y, 200, 40)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private ComponentInstance AddRectangle(double x = 0, double y = 0)
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps(null, null, 2),
            new Bounds(x, y, 160, 100)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas() =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, _board).Add(p => p.SnapToGrid, false)
        );

    private static IElement ContainerOf(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.Find($".component-container[data-d12-entity='{id}']");

    private static void DoublePress(IRenderedComponent<DiagramCanvas> canvas, Guid id)
    {
        canvas.ClickOn(ContainerOf(canvas, id));
        canvas.ClickOn(ContainerOf(canvas, id), pressCount: 2);
    }

    private static IElement StickyEditor(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find("textarea.d12-sticky-note-editor");

    private static IElement TextEditor(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find("textarea.d12-text-editor");

    private static void PressEscapeIn(IElement editor) =>
        editor.KeyDown(new KeyboardEventArgs { Key = "Escape" });

    private static Task PressF2(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnBeginEditPressed());

    private static Task Undo(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

    private int SelectAllCalls =>
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "focusAndSelectAll");

    private IReadOnlyList<int> FocusTabStopIndices =>
        CanvasModule
            .Invocations["focusTabStopAt"]
            .Select(invocation => (int)invocation.Arguments[1]!)
            .ToList();

    private static string TextOf(ComponentInstance instance) =>
        instance.Props switch
        {
            StickyNoteProps sticky => sticky.Text,
            TextProps text => text.Text,
            _ => throw new InvalidOperationException(),
        };

    [Fact]
    public void ADoublePressOnAStickyNoteOpensItsEditorWithAllTextSelected()
    {
        var note = AddStickyNote("Original");
        var canvas = RenderCanvas();

        DoublePress(canvas, note.Id);

        Assert.Equal("Original", StickyEditor(canvas).GetAttribute("value"));
        Assert.Equal(1, SelectAllCalls);
    }

    [Fact]
    public void ADoublePressOnATextOpensItsEditor()
    {
        var text = AddText("Words");
        var canvas = RenderCanvas();

        DoublePress(canvas, text.Id);

        Assert.Equal("Words", TextEditor(canvas).GetAttribute("value"));
    }

    [Fact]
    public void ADoublePressOnARectangleOpensNothing()
    {
        var rectangle = AddRectangle();
        var canvas = RenderCanvas();

        DoublePress(canvas, rectangle.Id);

        Assert.Empty(canvas.FindAll("textarea"));
        Assert.Equal(0, SelectAllCalls);
    }

    [Fact]
    public void ADoublePressOnAGroupedStickyNoteEntersTheGroupAndASecondOpensTheEditor()
    {
        var note = AddStickyNote("Original");
        var other = AddStickyNote("Other", 300, 0);
        _board.AddGroup(new Group([note.Id, other.Id]));
        var canvas = RenderCanvas();

        DoublePress(canvas, note.Id);
        Assert.Empty(canvas.FindAll("textarea"));

        canvas.ClickOn(ContainerOf(canvas, note.Id), pressCount: 2);

        Assert.Equal("Original", StickyEditor(canvas).GetAttribute("value"));
    }

    [Fact]
    public async Task F2OnAFocusedStickyNoteOpensItsEditor()
    {
        var note = AddStickyNote("Original");
        var canvas = RenderCanvas();
        ContainerOf(canvas, note.Id).Focus();

        await PressF2(canvas);

        Assert.Equal("Original", StickyEditor(canvas).GetAttribute("value"));
        Assert.Equal(1, SelectAllCalls);
    }

    [Fact]
    public async Task F2WithNothingFocusedOpensNothing()
    {
        var note = AddStickyNote("Original");
        var canvas = RenderCanvas();
        canvas.ClickOn(ContainerOf(canvas, note.Id));

        await PressF2(canvas);

        Assert.Empty(canvas.FindAll("textarea"));
    }

    [Fact]
    public async Task F2OnAFocusedRectangleOpensNothing()
    {
        var rectangle = AddRectangle();
        var canvas = RenderCanvas();
        ContainerOf(canvas, rectangle.Id).Focus();

        await PressF2(canvas);

        Assert.Empty(canvas.FindAll("textarea"));
    }

    [Fact]
    public async Task F2OnAFocusedLockedStickyNoteOpensNothing()
    {
        var note = new ComponentInstance(
            "sticky-note",
            new StickyNoteProps("Locked", "#FFEB3B", "#000000", 14),
            new Bounds(0, 0, 200, 200),
            locked: true
        );
        _board.AddComponent(note);
        var canvas = RenderCanvas();
        ContainerOf(canvas, note.Id).Focus();

        await PressF2(canvas);

        Assert.Empty(canvas.FindAll("textarea"));
    }

    [Fact]
    public async Task F2EndsAdditiveTraversal()
    {
        var first = AddStickyNote("First");
        var second = AddStickyNote("Second", 300, 0);
        var third = AddStickyNote("Third", 600, 0);
        var canvas = RenderCanvas();
        ContainerOf(canvas, first.Id).Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());
        ContainerOf(canvas, second.Id).Focus();

        await PressF2(canvas);
        StickyEditor(canvas).Blur();
        ContainerOf(canvas, third.Id).Focus();

        Assert.Equal([third.Id], canvas.Instance.SelectedComponents.Select(c => c.Id));
    }

    [Fact]
    public async Task BlurAfterEditingCommitsOneUndoableEntry()
    {
        var note = AddStickyNote("Original");
        var canvas = RenderCanvas();

        DoublePress(canvas, note.Id);
        StickyEditor(canvas).Input("Edited");
        StickyEditor(canvas).Blur();

        Assert.Equal("Edited", TextOf(note));
        Assert.Contains("Edited", canvas.Find("p.d12-sticky-note-text").TextContent);
        Assert.Empty(CanvasModule.Invocations["focusTabStopAt"]);

        await Undo(canvas);

        Assert.Equal("Original", TextOf(note));
        Assert.Contains("Original", canvas.Find("p.d12-sticky-note-text").TextContent);
    }

    [Fact]
    public async Task RedoAfterUndoingAnEditReappliesIt()
    {
        var note = AddStickyNote("Original");
        var canvas = RenderCanvas();
        DoublePress(canvas, note.Id);
        StickyEditor(canvas).Input("Edited");
        StickyEditor(canvas).Blur();
        await Undo(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal("Edited", TextOf(note));
    }

    [Fact]
    public async Task EscapeCommitsTheTypedTextAndReturnsFocusToTheShapesTabStop()
    {
        AddStickyNote("Before", 0, 0);
        var note = AddStickyNote("Original", 300, 0);
        var canvas = RenderCanvas();

        DoublePress(canvas, note.Id);
        StickyEditor(canvas).Input("Kept");
        PressEscapeIn(StickyEditor(canvas));

        Assert.Equal("Kept", TextOf(note));
        Assert.Empty(canvas.FindAll("textarea"));
        Assert.Equal([1], FocusTabStopIndices);

        await Undo(canvas);
        Assert.Equal("Original", TextOf(note));
    }

    [Fact]
    public async Task AnEditEndingWithFocusReturnedWhereNoStopIsOnScreenFocusesTheCanvas()
    {
        var note = AddStickyNote("Far", 3000, 3000);
        var canvas = RenderCanvas();
        var props = note.Props;

        await canvas.InvokeAsync(
            () => canvas.Instance.CommitInlineEdit(note.Id, props, props, returnFocus: true)
        );

        Assert.Empty(CanvasModule.Invocations["focusTabStopAt"]);
        Assert.Single(CanvasModule.Invocations["focusCanvas"]);
    }

    [Fact]
    public async Task EscapeWithNothingChangedRecordsNothing()
    {
        var note = AddStickyNote("Same");
        var canvas = RenderCanvas();
        canvas.ClickOn(ContainerOf(canvas, note.Id));
        canvas.DragOn(ContainerOf(canvas, note.Id), (100, 100), (150, 120));
        var moved = note.Bounds;

        DoublePress(canvas, note.Id);
        PressEscapeIn(StickyEditor(canvas));
        await Undo(canvas);

        Assert.NotEqual(moved, note.Bounds);
    }

    [Fact]
    public async Task BlurWithNothingChangedRecordsNothing()
    {
        var note = AddStickyNote("Same");
        var canvas = RenderCanvas();
        canvas.ClickOn(ContainerOf(canvas, note.Id));
        canvas.DragOn(ContainerOf(canvas, note.Id), (100, 100), (150, 120));
        var moved = note.Bounds;

        DoublePress(canvas, note.Id);
        StickyEditor(canvas).Blur();
        await Undo(canvas);

        Assert.NotEqual(moved, note.Bounds);
    }

    [Fact]
    public async Task ClickToAddOpensTheNewTextForTypingAndTheEditIsItsOwnEntry()
    {
        var canvas = RenderCanvas();

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd("text"));
        var placed = Assert.Single(_board.Components);
        TextEditor(canvas).Input("Typed");
        PressEscapeIn(TextEditor(canvas));

        Assert.Equal("Typed", TextOf(placed));
        await Undo(canvas);
        Assert.Equal("", TextOf(placed));
        Assert.Single(_board.Components);
        await Undo(canvas);
        Assert.Empty(_board.Components);
    }

    [Fact]
    public async Task ClickToAddARectangleOpensNothing()
    {
        var canvas = RenderCanvas();

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd("rectangle"));

        Assert.Single(_board.Components);
        Assert.Empty(canvas.FindAll("textarea"));
    }

    [Fact]
    public void DroppingAStickyNoteFromThePaletteOpensItForTyping()
    {
        var canvas = RenderCanvas();

        canvas.Instance.BeginPaletteDrag("sticky-note");
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        Assert.Single(_board.Components);
        Assert.Single(canvas.FindAll("textarea.d12-sticky-note-editor"));
    }

    [Fact]
    public void ADoublePressOnAShapeHalfOffScreenPansItFullyIntoViewAtTheSameScale()
    {
        var note = AddStickyNote("Edge", 700, 100);
        var canvas = RenderCanvas();
        var tracker = canvas.Instance.ZoomPanTracker;

        DoublePress(canvas, note.Id);

        Assert.Equal(1, tracker.Scale);
        Assert.Equal((-100, 0), (tracker.PanX, tracker.PanY));
        Assert.Single(canvas.FindAll("textarea"));
    }

    [Fact]
    public async Task APlacementShownAsAPlaceholderOpensNothingAndDoesNotPan()
    {
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, _board).Add(p => p.LodSizeThreshold, 1000)
        );
        var tracker = canvas.Instance.ZoomPanTracker;

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd("text"));

        Assert.Empty(canvas.FindAll("textarea"));
        Assert.Equal((0, 0), (tracker.PanX, tracker.PanY));
        Assert.Single(_board.Components);
    }

    [Fact]
    public async Task APastedOrDuplicatedTextNeverOpensForEditing()
    {
        var text = AddText("Copy me");
        var canvas = RenderCanvas();
        canvas.ClickOn(ContainerOf(canvas, text.Id));

        var envelope = await canvas.InvokeAsync(() => canvas.Instance.OnCopyRequested());
        await canvas.InvokeAsync(() => canvas.Instance.OnPasteReceived(envelope!, null, null));
        await canvas.InvokeAsync(() => canvas.Instance.OnDuplicatePressed());

        Assert.Equal(3, _board.Components.Count);
        Assert.Empty(canvas.FindAll("textarea"));
        Assert.Equal(0, SelectAllCalls);
    }

    [Fact]
    public void EditedTextRoundTripsThroughJsonSerialization()
    {
        var note = AddStickyNote("Original");
        var canvas = RenderCanvas();
        DoublePress(canvas, note.Id);
        StickyEditor(canvas).Input("Persisted text");
        StickyEditor(canvas).Blur();

        var serializer = new BoardJsonSerializer(Services.GetRequiredService<IComponentRegistry>());
        var reloaded = serializer.Deserialize(serializer.Serialize(_board));

        var reloadedInstance = Assert.Single(reloaded.Components);
        Assert.Equal("Persisted text", ((StickyNoteProps)reloadedInstance.Props).Text);
    }

    private Edge AddEdgeBetweenTwoRectangles()
    {
        var source = AddRectangle(0, 0);
        var target = AddRectangle(300, 0);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );
        _board.AddEdge(edge);
        return edge;
    }

    private static Task Redo(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

    [Fact]
    public async Task PlacingATextAndPressingEscapeWithoutTypingLeavesTheBoardAndHistoryAsBefore()
    {
        var note = AddStickyNote("Moved");
        var canvas = RenderCanvas();
        canvas.ClickOn(ContainerOf(canvas, note.Id));
        canvas.DragOn(ContainerOf(canvas, note.Id), (100, 100), (150, 120));
        var moved = note.Bounds;

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd("text"));
        PressEscapeIn(TextEditor(canvas));

        Assert.Equal([note.Id], _board.Components.Select(instance => instance.Id));
        Assert.Empty(canvas.FindAll("textarea"));
        Assert.Empty(canvas.Instance.SelectedComponents);
        Assert.Single(CanvasModule.Invocations["focusCanvas"]);

        await Undo(canvas);
        Assert.NotEqual(moved, note.Bounds);
        await Redo(canvas);
        Assert.Equal(moved, note.Bounds);
        await Redo(canvas);
        Assert.Single(_board.Components);
    }

    [Fact]
    public async Task ATextPlacedAndLeftAsWhitespaceIsRemovedOnBlurWithNoEntry()
    {
        var canvas = RenderCanvas();

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd("text"));
        TextEditor(canvas).Input("   ");
        TextEditor(canvas).Blur();

        Assert.Empty(_board.Components);
        Assert.Empty(CanvasModule.Invocations["focusCanvas"]);
        await Undo(canvas);
        Assert.Empty(_board.Components);
        await Redo(canvas);
        Assert.Empty(_board.Components);
    }

    [Fact]
    public async Task ClearingAnExistingTextRemovesItInOneEntryAndUndoBringsItBackWithItsText()
    {
        var text = AddText("Words");
        var canvas = RenderCanvas();

        DoublePress(canvas, text.Id);
        TextEditor(canvas).Input("");
        PressEscapeIn(TextEditor(canvas));

        Assert.Empty(_board.Components);
        Assert.Empty(canvas.Instance.SelectedComponents);
        Assert.Single(CanvasModule.Invocations["focusCanvas"]);

        await Undo(canvas);

        var restored = Assert.Single(_board.Components);
        Assert.Same(text, restored);
        Assert.Equal("Words", TextOf(restored));
        Assert.Contains("Words", canvas.Find("p.d12-text").TextContent);
    }

    [Fact]
    public async Task AnEmptyTextOpenedAndClosedUnchangedIsRemoved()
    {
        var text = AddText("");
        var canvas = RenderCanvas();
        ContainerOf(canvas, text.Id).Focus();

        await PressF2(canvas);
        PressEscapeIn(TextEditor(canvas));

        Assert.Empty(_board.Components);
        await Undo(canvas);
        Assert.Same(text, Assert.Single(_board.Components));
    }

    [Fact]
    public async Task ClearingAStickyNoteLeavesAnEmptyStickyNote()
    {
        var note = AddStickyNote("Words");
        var canvas = RenderCanvas();

        DoublePress(canvas, note.Id);
        StickyEditor(canvas).Input("");
        StickyEditor(canvas).Blur();

        Assert.Same(note, Assert.Single(_board.Components));
        Assert.Equal("", TextOf(note));
        await Undo(canvas);
        Assert.Equal("Words", TextOf(note));
    }

    [Fact]
    public async Task AStickyNotePlacedAndAbandonedStaysAsOneEntry()
    {
        var canvas = RenderCanvas();

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd("sticky-note"));
        PressEscapeIn(StickyEditor(canvas));

        Assert.Single(_board.Components);
        await Undo(canvas);
        Assert.Empty(_board.Components);
    }

    [Fact]
    public async Task RemovingAGroupedTextThatLeavesOneMemberDissolvesTheGroupInTheSameEntry()
    {
        var text = AddText("Words");
        var note = AddStickyNote("Other", 300, 0);
        var group = new Group([text.Id, note.Id]);
        _board.AddGroup(group);
        var canvas = RenderCanvas();

        DoublePress(canvas, text.Id);
        canvas.ClickOn(ContainerOf(canvas, text.Id), pressCount: 2);
        TextEditor(canvas).Input("");
        PressEscapeIn(TextEditor(canvas));

        Assert.Equal([note.Id], _board.Components.Select(instance => instance.Id));
        Assert.Empty(_board.Groups);

        await Undo(canvas);

        Assert.Equal("Words", TextOf(text));
        Assert.Equal(2, _board.Components.Count);
        var restoredGroup = Assert.Single(_board.Groups);
        Assert.Equal(group.Id, restoredGroup.Id);
        Assert.Equal([text.Id, note.Id], restoredGroup.MemberIds);
    }

    [Fact]
    public async Task ACommandBetweenCreationAndAnEmptyEditEndKeepsTheCreationAndRemovesAsItsOwnEntry()
    {
        var canvas = RenderCanvas();

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd("text"));
        var text = Assert.Single(_board.Components);
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd("rectangle"));
        PressEscapeIn(TextEditor(canvas));

        Assert.DoesNotContain(text, _board.Components);
        await Undo(canvas);
        Assert.Contains(text, _board.Components);
        Assert.Equal(2, _board.Components.Count);
        await Undo(canvas);
        Assert.Equal([text.Id], _board.Components.Select(instance => instance.Id));
        await Undo(canvas);
        Assert.Empty(_board.Components);
    }

    [Fact]
    public async Task AnUndoSinceTheCreationKeepsTheCreationAndRemovesAsItsOwnEntry()
    {
        var canvas = RenderCanvas();
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd("text"));
        var text = Assert.Single(_board.Components);
        TextEditor(canvas).Input("a");
        TextEditor(canvas).Blur();
        await Undo(canvas);

        ContainerOf(canvas, text.Id).Focus();
        await PressF2(canvas);
        PressEscapeIn(TextEditor(canvas));

        Assert.Empty(_board.Components);
        await Undo(canvas);
        Assert.Same(text, Assert.Single(_board.Components));
        Assert.Equal("", TextOf(text));
        await Undo(canvas);
        Assert.Empty(_board.Components);
    }

    [Fact]
    public async Task AnEdgeLabelAddedAndAbandonedLeavesNoLabelAndNoEntry()
    {
        var edge = AddEdgeBetweenTwoRectangles();
        var canvas = RenderCanvas();

        canvas.DoubleClickElement(canvas.Find(".edge-hit"));
        PressEscapeIn(TextEditor(canvas));

        Assert.Null(edge.Label);
        Assert.Empty(canvas.FindAll(".edge-label"));
        Assert.Equal(1, FocusTabStopIndices[^1]);
        await Undo(canvas);
        Assert.Null(edge.Label);
        await Redo(canvas);
        Assert.Null(edge.Label);
    }

    [Fact]
    public async Task AnEdgeLabelClearedToWhitespaceIsRemovedAndFocusLandsOnTheEdgesStop()
    {
        var edge = AddEdgeBetweenTwoRectangles();
        var label = new ComponentInstance(
            "text",
            new TextProps("Name", null, 16, "normal", "left"),
            new Bounds(0, 0, 80, 24)
        );
        edge.Label = label;
        var canvas = RenderCanvas();

        canvas.DoubleClickElement(canvas.Find(".edge-label"));
        TextEditor(canvas).Input("  \t ");
        PressEscapeIn(TextEditor(canvas));

        Assert.Null(edge.Label);
        Assert.Equal([1], FocusTabStopIndices);

        await Undo(canvas);
        Assert.Same(label, edge.Label);
        Assert.Equal("Name", TextOf(label));
    }
}
