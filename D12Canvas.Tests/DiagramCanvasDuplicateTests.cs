using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasDuplicateTests : ComponentTestBase
{
    public DiagramCanvasDuplicateTests()
    {
        SetupDiagramCanvasJsModule();
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        Services.AddSingleton(options.Registry);
    }

    private static ComponentInstance AddShape(Board board, double x, double y = 0)
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps(null, null, 2),
            new Bounds(x, y, 100, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board, bool snapToGrid = false) =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, snapToGrid)
        );

    private static Task Duplicate(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnDuplicatePressed());

    private static ComponentInstance SingleAddedSince(Board board, IEnumerable<Guid> before) =>
        Assert.Single(board.Components, instance => !before.Contains(instance.Id));

    private static Guid[] Ids(Board board) => board.Components.Select(c => c.Id).ToArray();

    [Fact]
    public async Task CtrlDPlacesACopyTwentyDownAndRightAndSelectsIt()
    {
        var board = new Board();
        var shape = AddShape(board, 100, 60);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));

        await Duplicate(canvas);

        var copy = SingleAddedSince(board, [shape.Id]);
        Assert.NotEqual(shape.Id, copy.Id);
        Assert.Equal(new Bounds(120, 80, 100, 50), copy.Bounds);
        Assert.Equal([copy], canvas.Instance.SelectedComponents);
        Assert.Equal(new Bounds(100, 60, 100, 50), shape.Bounds);
    }

    [Fact]
    public async Task DuplicatingNeverTouchesTheClipboard()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));

        await Duplicate(canvas);

        Assert.DoesNotContain(
            CanvasModule.Invocations,
            invocation => invocation.Identifier is "writeClipboardText" or "readClipboardText"
        );
    }

    [Fact]
    public async Task TheFirstStepIsNotSnappedToTheGrid()
    {
        var board = new Board();
        var shape = AddShape(board, 3, 7);
        var canvas = RenderCanvas(board, snapToGrid: true);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));

        await Duplicate(canvas);

        Assert.Equal(new Bounds(23, 27, 100, 50), SingleAddedSince(board, [shape.Id]).Bounds);
    }

    [Fact]
    public async Task DraggingTheCopyThenDuplicatingAgainRepeatsThatOffsetOffGrid()
    {
        var board = new Board();
        var shape = AddShape(board, 0, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        await Duplicate(canvas);
        var second = SingleAddedSince(board, [shape.Id]);

        canvas.DragOn(canvas.ContainerOf(second.Id), (30, 30), (147, 13));
        Assert.Equal(new Bounds(137, 3, 100, 50), second.Bounds);
        await Duplicate(canvas);

        var third = SingleAddedSince(board, [shape.Id, second.Id]);
        Assert.Equal(new Bounds(274, 6, 100, 50), third.Bounds);
        Assert.Equal([third], canvas.Instance.SelectedComponents);

        await Duplicate(canvas);

        var fourth = SingleAddedSince(board, [shape.Id, second.Id, third.Id]);
        Assert.Equal(new Bounds(411, 9, 100, 50), fourth.Bounds);
    }

    [Fact]
    public async Task WithSnappingOnTheRunIsReplayedUnsnapped()
    {
        var board = new Board();
        var shape = AddShape(board, 0, 0);
        var canvas = RenderCanvas(board, snapToGrid: true);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        await Duplicate(canvas);
        var second = SingleAddedSince(board, [shape.Id]);
        second.Bounds = second.Bounds with { X = 133, Y = 4 };

        await Duplicate(canvas);

        Assert.Equal(
            new Bounds(266, 8, 100, 50),
            SingleAddedSince(board, [shape.Id, second.Id]).Bounds
        );
    }

    [Fact]
    public async Task ClickingElsewhereAndDuplicatingStartsAFreshRun()
    {
        var board = new Board();
        var shape = AddShape(board, 0, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        await Duplicate(canvas);
        var second = SingleAddedSince(board, [shape.Id]);
        second.Bounds = second.Bounds with { X = 300, Y = 0 };

        await canvas.ClickCanvas(700, 500);
        canvas.ClickOn(canvas.ContainerOf(second.Id));
        await Duplicate(canvas);

        Assert.Equal(
            new Bounds(320, 20, 100, 50),
            SingleAddedSince(board, [shape.Id, second.Id]).Bounds
        );
    }

    [Fact]
    public async Task DuplicatingASelectionCarriesItsInteriorEdgesAndMovesAsOneBody()
    {
        var board = new Board();
        var left = AddShape(board, 0, 0);
        var right = AddShape(board, 200, 0);
        board.AddEdge(
            new Edge(
                new PortEndpoint(left.Id, PortId.Right),
                new PortEndpoint(right.Id, PortId.Left)
            )
        );
        var canvas = RenderCanvas(board);
        await canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());

        await Duplicate(canvas);

        Assert.Equal(4, board.Components.Count);
        Assert.Equal(2, board.Edges.Count);
        var copies = board.Components.Where(c => c.Id != left.Id && c.Id != right.Id).ToList();
        Assert.Equal(
            [new Bounds(20, 20, 100, 50), new Bounds(220, 20, 100, 50)],
            copies.Select(c => c.Bounds).OrderBy(b => b.X)
        );
        Assert.Single(canvas.Instance.SelectedEdges);
        Assert.Equal(2, canvas.Instance.SelectedComponents.Count);
    }

    [Fact]
    public async Task ALoneSelectedEdgeDuplicatesAsAFloatingEdge()
    {
        var board = new Board();
        var edge = new Edge(new FloatingEndpoint(10, 10), new FloatingEndpoint(90, 40));
        board.AddEdge(edge);
        var canvas = RenderCanvas(board);
        await canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());

        await Duplicate(canvas);

        var copy = Assert.Single(board.Edges, candidate => candidate.Id != edge.Id);
        Assert.Equal(new FloatingEndpoint(30, 30), copy.Source);
        Assert.Equal(new FloatingEndpoint(110, 60), copy.Target);
        Assert.Equal(copy, Assert.Single(canvas.Instance.SelectedEdges));
    }

    [Fact]
    public async Task OneUndoRemovesTheDuplicate()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        await Duplicate(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal([shape.Id], Ids(board));
    }

    [Fact]
    public async Task UndoingADuplicateEndsTheRunAndRedoDoesNotRestoreIt()
    {
        var board = new Board();
        var shape = AddShape(board, 0, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        await Duplicate(canvas);
        var second = SingleAddedSince(board, [shape.Id]);
        second.Bounds = second.Bounds with { X = 300, Y = 0 };

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        await Duplicate(canvas);

        Assert.Equal(
            new Bounds(320, 20, 100, 50),
            SingleAddedSince(board, [shape.Id, second.Id]).Bounds
        );
    }

    [Fact]
    public async Task UndoingAMoveOfTheCopyKeepsTheRunAndFollowsTheRestoredBounds()
    {
        var board = new Board();
        var shape = AddShape(board, 0, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        await Duplicate(canvas);
        var second = SingleAddedSince(board, [shape.Id]);
        canvas.DragOn(canvas.ContainerOf(second.Id), (30, 30), (130, 10));
        canvas.DragOn(canvas.ContainerOf(second.Id), (130, 10), (330, 10));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        await Duplicate(canvas);

        Assert.Equal(new Bounds(120, 0, 100, 50), second.Bounds);
        Assert.Equal(
            new Bounds(240, 0, 100, 50),
            SingleAddedSince(board, [shape.Id, second.Id]).Bounds
        );
    }

    [Fact]
    public async Task WithNothingSelectedDuplicateDoesNothing()
    {
        var board = new Board();
        AddShape(board, 0);
        var canvas = RenderCanvas(board);

        await Duplicate(canvas);

        Assert.Single(board.Components);
    }

    [Fact]
    public async Task TheCopyIsNotFocusedAndNoInlineEditOpens()
    {
        var board = new Board();
        var note = new ComponentInstance(
            "sticky-note",
            new StickyNoteProps("Label", "#FFEB3B", "#000000", 14),
            new Bounds(0, 0, 200, 200)
        );
        board.AddComponent(note);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(note.Id));
        var focusCallsBefore = FocusCalls();

        await Duplicate(canvas);

        Assert.Equal(focusCallsBefore, FocusCalls());
        Assert.Empty(canvas.FindAll("textarea.d12-sticky-note-editor"));
    }

    private int FocusCalls() =>
        CanvasModule.Invocations.Count(invocation =>
            invocation.Identifier.StartsWith("focus", StringComparison.Ordinal)
        );

    [Fact]
    public async Task TheObjectMenuOffersDuplicateWithItsChord()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        await OpenObjectMenu(canvas, shape);

        var row = DuplicateRow(canvas);

        Assert.Equal("Ctrl+D", row.QuerySelector(".d12-context-menu-hint")!.TextContent);
    }

    [Fact]
    public async Task TheCanvasMenuHasNoDuplicateRow()
    {
        var canvas = RenderCanvas(new Board());

        await canvas.Press(300, 200, PointerPress.SecondaryButton);
        await canvas.Release(300, 200, PointerPress.SecondaryButton);

        Assert.DoesNotContain(
            "Duplicate",
            canvas.FindAll(".d12-context-menu-label").Select(label => label.TextContent)
        );
    }

    [Fact]
    public async Task TheDuplicateRowDuplicatesTheSelection()
    {
        var board = new Board();
        var shape = AddShape(board, 40, 40);
        var canvas = RenderCanvas(board);
        await OpenObjectMenu(canvas, shape);

        await DuplicateRow(canvas)
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        var copy = SingleAddedSince(board, [shape.Id]);
        Assert.Equal(new Bounds(60, 60, 100, 50), copy.Bounds);
        Assert.Equal([copy], canvas.Instance.SelectedComponents);
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    private static AngleSharp.Dom.IElement DuplicateRow(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas
            .FindAll(".d12-context-menu-item")
            .Single(item =>
                item.QuerySelector(".d12-context-menu-label")!.TextContent == "Duplicate"
            );

    private static async Task OpenObjectMenu(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance shape
    )
    {
        await canvas.Press(
            10,
            10,
            PointerPress.SecondaryButton,
            role: HitRole.Instance,
            entityId: shape.Id
        );
        await canvas.Release(10, 10, PointerPress.SecondaryButton);
    }
}
