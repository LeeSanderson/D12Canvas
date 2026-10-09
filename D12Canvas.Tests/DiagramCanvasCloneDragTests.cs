using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasCloneDragTests : ComponentTestBase
{
    public DiagramCanvasCloneDragTests()
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

    private static (ComponentInstance Left, ComponentInstance Right, Edge Edge) ConnectedPair(
        Board board
    )
    {
        var left = AddShape(board, 0, 0);
        var right = AddShape(board, 200, 0);
        var edge = new Edge(
            new PortEndpoint(left.Id, PortId.Right),
            new PortEndpoint(right.Id, PortId.Left)
        );
        board.AddEdge(edge);
        return (left, right, edge);
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board, bool snapToGrid = false) =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, snapToGrid)
        );

    private static void SelectBoth(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance left,
        ComponentInstance right
    )
    {
        canvas.ClickOn(canvas.ContainerOf(left.Id));
        canvas.ClickOn(canvas.ContainerOf(right.Id), shift: true);
    }

    private static string ContainerStyle(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.ContainerOf(id).GetAttribute("style")!;

    private static string? AriaSelected(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.ContainerOf(id).GetAttribute("aria-selected");

    private static List<Guid> RenderedInstanceIds(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas
            .FindAll(".component-container")
            .Select(element => Guid.Parse(element.GetAttribute("data-d12-entity")!))
            .ToList();

    [Fact]
    public async Task AnAltDragOfTwoConnectedShapesDropsCopiesWithTheirEdgeInOneHistoryEntry()
    {
        var board = new Board();
        var (left, right, edge) = ConnectedPair(board);
        var canvas = RenderCanvas(board);
        SelectBoth(canvas, left, right);

        canvas.PressOn(canvas.ContainerOf(left.Id), (30, 30));
        canvas.MoveTo((30, 130), alt: true);
        canvas.ReleaseAt((30, 130));

        Assert.Equal(new Bounds(0, 0, 100, 50), left.Bounds);
        Assert.Equal(new Bounds(200, 0, 100, 50), right.Bounds);
        var copies = board.Components.Where(c => c.Id != left.Id && c.Id != right.Id).ToList();
        Assert.Equal(
            [new Bounds(0, 100, 100, 50), new Bounds(200, 100, 100, 50)],
            copies.Select(c => c.Bounds).OrderBy(b => b.X)
        );
        var copiedEdge = Assert.Single(board.Edges, candidate => candidate.Id != edge.Id);
        Assert.Equal(
            copies.Select(c => c.Id).Order(),
            new[]
            {
                copiedEdge.Source.ComponentId!.Value,
                copiedEdge.Target.ComponentId!.Value,
            }.Order()
        );

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(
            new[] { left.Id, right.Id }.Order(),
            board.Components.Select(c => c.Id).Order()
        );
        Assert.Equal([edge], board.Edges);
    }

    // A copied group shows selected through its own stop as a copied shape does through its
    // container, and like that container it takes no focus until it is on the board.
    [Fact]
    public void MidCloneACopiedGroupHasItsOwnStopShownSelectedAndOutOfTheTabOrder()
    {
        var board = new Board();
        var (left, right, _) = ConnectedPair(board);
        var group = new Group([left.Id, right.Id]);
        board.AddGroup(group);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(left.Id));

        canvas.PressOn(canvas.ContainerOf(left.Id), (30, 30));
        canvas.MoveTo((30, 130), alt: true);

        var stops = canvas.FindAll(".group-tab-stop");
        Assert.Equal(2, stops.Count);
        var original = Assert.Single(stops, stop => stop.GetAttribute("tabindex") == "0");
        Assert.Null(original.GetAttribute("aria-selected"));
        var copy = Assert.Single(stops, stop => stop.GetAttribute("tabindex") is null);
        Assert.Equal("true", copy.GetAttribute("aria-selected"));
        Assert.Contains("top: 100px", copy.GetAttribute("style"));

        canvas.ReleaseAt((30, 130));

        Assert.All(
            canvas.FindAll(".group-tab-stop"),
            stop => Assert.Equal("0", stop.GetAttribute("tabindex"))
        );
    }

    [Fact]
    public void MidCloneTheOriginalsStayPutAndTheCopiesShowSelectedUnderThePointer()
    {
        var board = new Board();
        var (left, right, edge) = ConnectedPair(board);
        var canvas = RenderCanvas(board);
        SelectBoth(canvas, left, right);

        canvas.PressOn(canvas.ContainerOf(left.Id), (30, 30));
        canvas.MoveTo((30, 130), alt: true);

        Assert.Contains("top: 0px", ContainerStyle(canvas, left.Id));
        Assert.Null(AriaSelected(canvas, left.Id));
        Assert.Null(AriaSelected(canvas, right.Id));
        var copyIds = RenderedInstanceIds(canvas).Except([left.Id, right.Id]).ToList();
        Assert.Equal(2, copyIds.Count);
        Assert.All(
            copyIds,
            id =>
            {
                Assert.Contains("top: 100px", ContainerStyle(canvas, id));
                Assert.Equal("true", AriaSelected(canvas, id));
            }
        );
        Assert.Equal(2, canvas.FindAll(".edge-line").Count);
        Assert.Single(canvas.FindAll(".edge-halo"));
        Assert.Contains("top: 100px", canvas.Find(".selection-bounding-box").GetAttribute("style"));
        Assert.Equal(2, board.Components.Count);
        Assert.Equal([edge], board.Edges);
    }

    [Fact]
    public void ReleasingAltMidDragPutsTheOriginalsUnderThePointerAndPressingItBringsTheCopiesBack()
    {
        var board = new Board();
        var shape = AddShape(board, 0, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));

        canvas.PressOn(canvas.ContainerOf(shape.Id), (30, 30));
        canvas.MoveTo((30, 130), alt: true);
        Assert.Equal(2, RenderedInstanceIds(canvas).Count);

        canvas.MoveTo((30, 130));
        Assert.Equal([shape.Id], RenderedInstanceIds(canvas));
        Assert.Contains("top: 100px", ContainerStyle(canvas, shape.Id));
        Assert.Equal("true", AriaSelected(canvas, shape.Id));

        canvas.MoveTo((30, 130), alt: true);
        Assert.Contains("top: 0px", ContainerStyle(canvas, shape.Id));
        canvas.ReleaseAt((30, 130));

        Assert.Equal(new Bounds(0, 0, 100, 50), shape.Bounds);
        var copy = Assert.Single(board.Components, c => c.Id != shape.Id);
        Assert.Equal(new Bounds(0, 100, 100, 50), copy.Bounds);
    }

    [Fact]
    public void LettingGoOfAltBeforeReleasingCommitsAMove()
    {
        var board = new Board();
        var shape = AddShape(board, 0, 0);
        var canvas = RenderCanvas(board);

        canvas.PressOn(canvas.ContainerOf(shape.Id), (30, 30));
        canvas.MoveTo((30, 130), alt: true);
        canvas.MoveTo((30, 130));
        canvas.ReleaseAt((30, 130));

        Assert.Equal([shape], board.Components);
        Assert.Equal(new Bounds(0, 100, 100, 50), shape.Bounds);
    }

    [Fact]
    public async Task AfterTheDropTheCopiesAreSelectedAndCtrlDContinuesAtTheDragsOffset()
    {
        var board = new Board();
        var shape = AddShape(board, 0, 0);
        var canvas = RenderCanvas(board);

        canvas.PressOn(canvas.ContainerOf(shape.Id), (30, 30));
        canvas.MoveTo((167, 43), alt: true);
        canvas.ReleaseAt((167, 43));

        var copy = Assert.Single(board.Components, c => c.Id != shape.Id);
        Assert.Equal(new Bounds(137, 13, 100, 50), copy.Bounds);
        Assert.Equal([copy], canvas.Instance.SelectedComponents);

        await canvas.InvokeAsync(() => canvas.Instance.OnDuplicatePressed());

        var third = Assert.Single(board.Components, c => c.Id != shape.Id && c.Id != copy.Id);
        Assert.Equal(new Bounds(274, 26, 100, 50), third.Bounds);
    }

    [Fact]
    public async Task EscapeMidCloneLeavesTheBoardUntouchedAndTheOriginalsSelected()
    {
        var board = new Board();
        var (left, right, edge) = ConnectedPair(board);
        var canvas = RenderCanvas(board);
        SelectBoth(canvas, left, right);

        canvas.PressOn(canvas.ContainerOf(left.Id), (30, 30));
        canvas.MoveTo((30, 130), alt: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        Assert.Equal(new[] { left.Id, right.Id }.Order(), RenderedInstanceIds(canvas).Order());
        Assert.Equal("true", AriaSelected(canvas, left.Id));
        Assert.Equal("true", AriaSelected(canvas, right.Id));

        canvas.ReleaseAt((30, 130));

        Assert.Equal([left, right], board.Components.OrderBy(c => c.Bounds.X));
        Assert.Equal([edge], board.Edges);
        Assert.Equal(new Bounds(0, 0, 100, 50), left.Bounds);
        Assert.Equal(2, canvas.Instance.SelectedComponents.Count);
    }
}
