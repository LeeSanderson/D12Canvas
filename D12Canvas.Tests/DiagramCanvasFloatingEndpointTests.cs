using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Floating endpoints. Releasing a connector drag on empty canvas creates an edge with a floating
// endpoint; an existing edge's endpoint - attached or floating - can be re-dragged to reattach or
// detach it, and every such reposition is one history entry.
public class DiagramCanvasFloatingEndpointTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasFloatingEndpointTests()
    {
        SetupDiagramCanvasJsModule();

        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: ComponentTypeKey,
                ComponentType: typeof(TestPropsComponent),
                PropsType: typeof(TestProps),
                DisplayName: "Test Props",
                AccessibleName: "Test props component",
                DefaultProps: new TestProps(),
                Icon: null,
                Role: "group",
                DefaultSize: null,
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    private static ComponentInstance AddInstance(Board board, double x, double y)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 100, 100)
        );
        board.AddComponent(instance);
        return instance;
    }

    // Two instances whose right port at (200, 150) and left port at (300, 150) an edge joins.
    private static Edge AddConnectedPair(
        Board board,
        out ComponentInstance source,
        out ComponentInstance target
    )
    {
        source = AddInstance(board, 100, 100);
        target = AddInstance(board, 300, 100);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );
        board.AddEdge(edge);
        return edge;
    }

    [Fact]
    public void AFloatingEndpointRendersAsAMarkerAtTheDropPoint()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.DragConnector(canvas.SelectedPortSpan(source.Id, "Right"), (200, 150), (190, 400));

        var marker = canvas.Find(".floating-endpoint");
        Assert.Equal("190", marker.GetAttribute("cx"));
        Assert.Equal("400", marker.GetAttribute("cy"));
    }

    // Verifies the marker stays at its board point through pan/zoom: it lives in board-space
    // cx/cy inside the same .canvas-content ancestor whose CSS transform carries pan/zoom (the
    // same mechanism .edge-line already relies on) - so zooming must move only that ancestor's
    // transform, never the marker's own coordinates.
    [Fact]
    public void AFloatingEndpointsBoardPointIsUnaffectedByZoom()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        board.AddEdge(
            new Edge(new PortEndpoint(source.Id, PortId.Right), new FloatingEndpoint(190, 400))
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var transformBefore = canvas.Find(".canvas-content").GetAttribute("style");

        canvas.ZoomIn();

        var transformAfter = canvas.Find(".canvas-content").GetAttribute("style");
        Assert.NotEqual(transformBefore, transformAfter); // the zoom actually happened

        var marker = canvas.Find(".floating-endpoint");
        Assert.Equal("190", marker.GetAttribute("cx"));
        Assert.Equal("400", marker.GetAttribute("cy"));
    }

    // Each floating marker is independent - carrying one end of an edge hides that edge's line
    // but not the OTHER end's own floating marker.
    [Fact]
    public void CarryingOneEndpointDoesNotHideTheOtherEndsFloatingMarker()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        board.AddEdge(
            new Edge(new PortEndpoint(source.Id, PortId.Right), new FloatingEndpoint(190, 400))
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.PressElement(canvas.SelectedPortSpan(source.Id, "Right"), (200, 150));
        canvas.MoveTo((210, 200));

        Assert.Empty(canvas.FindAll(".edge-line"));
        var marker = canvas.Find(".floating-endpoint");
        Assert.Equal("190", marker.GetAttribute("cx"));
        Assert.Equal("400", marker.GetAttribute("cy"));
    }

    // While an end is carried, the pending line is the only thing drawing its edge: from the end
    // that stays put to the pointer, with the carried end's own marker gone.
    [Fact]
    public void ThePendingLineIsTheOnlyThingDrawingAnEdgeWhoseEndIsCarried()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        board.AddEdge(
            new Edge(new PortEndpoint(source.Id, PortId.Right), new FloatingEndpoint(190, 400))
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.PressElement(canvas.Find(".floating-endpoint"), (190, 400));
        canvas.MoveTo((300, 350));

        Assert.Empty(canvas.FindAll(".edge-line"));
        Assert.Empty(canvas.FindAll(".edge-hit"));
        Assert.Empty(canvas.FindAll(".floating-endpoint"));
        var preview = canvas.Find(".connector-drag-preview");
        Assert.Equal(
            ("200", "150", "300", "350"),
            (
                preview.GetAttribute("x1"),
                preview.GetAttribute("y1"),
                preview.GetAttribute("x2"),
                preview.GetAttribute("y2")
            )
        );
    }

    [Fact]
    public void DraggingAFloatingEndpointOntoAPortAttachesIt()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        board.AddEdge(
            new Edge(new PortEndpoint(source.Id, PortId.Right), new FloatingEndpoint(190, 400))
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.DragConnectorToPort(
            canvas.Find(".floating-endpoint"),
            (190, 400),
            (300, 150),
            target.Id,
            "Left"
        );

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
        Assert.Empty(canvas.FindAll(".floating-endpoint"));
        Assert.NotNull(canvas.Find(".edge-line"));
    }

    [Fact]
    public void DraggingAnAttachedEndpointOffItsPortDetachesItToFloating()
    {
        var board = new Board();
        AddConnectedPair(board, out var source, out var target);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.DragConnector(canvas.SelectedPortSpan(target.Id, "Left"), (300, 150), (400, 400));

        var edge = Assert.Single(board.Edges); // detaching moves the endpoint, never creates a new edge
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new FloatingEndpoint(400, 400), edge.Target);
        var marker = canvas.Find(".floating-endpoint");
        Assert.Equal("400", marker.GetAttribute("cx"));
        Assert.Equal("400", marker.GetAttribute("cy"));
    }

    [Fact]
    public async Task UndoPutsADetachedEndpointBackOnItsPortAndRedoDetachesItAgain()
    {
        var board = new Board();
        var edge = AddConnectedPair(board, out _, out var target);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.DragConnector(canvas.SelectedPortSpan(target.Id, "Left"), (300, 150), (400, 400));
        Assert.Equal(new FloatingEndpoint(400, 400), edge.Target);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
        Assert.Empty(canvas.FindAll(".floating-endpoint"));

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        Assert.Equal(new FloatingEndpoint(400, 400), edge.Target);
    }

    [Fact]
    public void EscapeWhileRepositioningAnExistingEdgesEndpointLeavesItUnchanged()
    {
        var board = new Board();
        AddConnectedPair(board, out var source, out var target);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.PressElement(canvas.SelectedPortSpan(target.Id, "Left"), (300, 150));
        canvas.MoveTo((300, 300));
        canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        Assert.Single(canvas.FindAll(".edge-line"));

        canvas.MoveTo((400, 400));
        canvas.ReleaseOver((400, 400));

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
        Assert.Empty(canvas.FindAll(".floating-endpoint"));
    }

    // Guard: carrying one endpoint onto the shape the edge's other endpoint sits on would join the
    // shape to itself - that shape is never a drop target, and the edge is left unchanged.
    [Fact]
    public async Task DraggingAnEndpointOntoTheEdgesOtherEndpointsPortLeavesTheEdgeUnchanged()
    {
        var board = new Board();
        AddConnectedPair(board, out var source, out var target);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.DragConnector(
            canvas.SelectedPortSpan(source.Id, "Right"),
            (200, 150),
            (300, 150),
            over: canvas.ContainerOf(target.Id)
        );

        Assert.Empty(canvas.ContainerOf(target.Id).QuerySelectorAll(".port-span"));
        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);

        // Nothing entered history, so undo has nothing of this gesture's to take back.
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Single(board.Edges);
    }
}
