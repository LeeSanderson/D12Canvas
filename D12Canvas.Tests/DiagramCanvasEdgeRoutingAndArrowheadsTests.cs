using Bunit;
using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Per-edge routing style (straight/orthogonal/curved) and arrowheads (none/start/end/both), never
// board-wide. Straight stays a <line> (preserving the x1/y1/x2/y2 contract other tests already
// depend on, e.g. DiagramCanvasPortDragTests); Orthogonal/Curved render as a <path> with a
// computed `d`. Arrowheads are SVG <marker> refs on marker-start/marker-end, switching to a
// selected-color marker while the edge is selected (mirroring .edge-line.selected's own stroke
// swap).
public class DiagramCanvasEdgeRoutingAndArrowheadsTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasEdgeRoutingAndArrowheadsTests()
    {
        SetupDiagramCanvasJsModule();
        JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");

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
            new Bounds(x, y, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static Edge AddEdgeBetween(
        Board board,
        ComponentInstance source,
        ComponentInstance target,
        EdgeRouting routingStyle = EdgeRouting.Straight,
        ArrowStyle sourceArrow = ArrowStyle.None,
        ArrowStyle targetArrow = ArrowStyle.Arrow
    )
    {
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left),
            routingStyle: routingStyle,
            sourceArrow: sourceArrow,
            targetArrow: targetArrow
        );
        board.AddEdge(edge);
        return edge;
    }

    [Fact]
    public void ADefaultEdgeRendersAsALineWithAnArrowMarkerOnlyAtTheTarget()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var line = canvas.Find(".edge-line");

        Assert.Equal("line", line.TagName, ignoreCase: true);
        Assert.Null(line.GetAttribute("marker-start"));
        Assert.Equal("url(#edge-arrow)", line.GetAttribute("marker-end"));
    }

    [Fact]
    public void NoArrowsOnEitherEndOmitsBothMarkerAttributes()
    {
        var board = new Board();
        AddEdgeBetween(
            board,
            AddInstance(board, 100, 100),
            AddInstance(board, 250, 100),
            sourceArrow: ArrowStyle.None,
            targetArrow: ArrowStyle.None
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var line = canvas.Find(".edge-line");

        Assert.Null(line.GetAttribute("marker-start"));
        Assert.Null(line.GetAttribute("marker-end"));
    }

    [Fact]
    public void ArrowsOnBothEndsSetsBothMarkerAttributes()
    {
        var board = new Board();
        AddEdgeBetween(
            board,
            AddInstance(board, 100, 100),
            AddInstance(board, 250, 100),
            sourceArrow: ArrowStyle.Arrow,
            targetArrow: ArrowStyle.Arrow
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var line = canvas.Find(".edge-line");

        Assert.Equal("url(#edge-arrow)", line.GetAttribute("marker-start"));
        Assert.Equal("url(#edge-arrow)", line.GetAttribute("marker-end"));
    }

    [Fact]
    public void ASelectedEdgeUsesTheSelectedColorArrowMarker()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".edge-line").Click();

        Assert.Equal(
            "url(#edge-arrow-selected)",
            canvas.Find(".edge-line").GetAttribute("marker-end")
        );
    }

    [Fact]
    public void AnOrthogonalEdgeRendersAsAPathWithARightAngleBendAtTheMidpoint()
    {
        var board = new Board();
        AddEdgeBetween(
            board,
            AddInstance(board, 100, 100),
            AddInstance(board, 300, 200),
            routingStyle: EdgeRouting.Orthogonal
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var edge = canvas.Find(".edge-line");

        Assert.Equal("path", edge.TagName, ignoreCase: true);
        var d = edge.GetAttribute("d");
        Assert.NotNull(d);
        // source port: right of (100,100,50,50) = (150,125); target port: left of (300,200,50,50) = (300,225)
        Assert.Equal("M 150 125 L 225 125 L 225 225 L 300 225", d);
    }

    [Fact]
    public void ACurvedEdgeRendersAsAPathWithACubicBezierCommand()
    {
        var board = new Board();
        AddEdgeBetween(
            board,
            AddInstance(board, 100, 100),
            AddInstance(board, 300, 200),
            routingStyle: EdgeRouting.Curved
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var edge = canvas.Find(".edge-line");

        Assert.Equal("path", edge.TagName, ignoreCase: true);
        var d = edge.GetAttribute("d");
        Assert.NotNull(d);
        Assert.Equal("M 150 125 C 225 125 225 225 300 225", d);
    }

    [Fact]
    public void OrthogonalAndCurvedEdgesStillCarryMarkerAndSelectionAttributes()
    {
        var board = new Board();
        AddEdgeBetween(
            board,
            AddInstance(board, 100, 100),
            AddInstance(board, 300, 200),
            routingStyle: EdgeRouting.Orthogonal,
            sourceArrow: ArrowStyle.Arrow,
            targetArrow: ArrowStyle.None
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".edge-line").Click();
        var edge = canvas.Find(".edge-line");

        Assert.Equal("url(#edge-arrow-selected)", edge.GetAttribute("marker-start"));
        Assert.Null(edge.GetAttribute("marker-end"));
        Assert.Equal("true", edge.GetAttribute("aria-selected"));
        Assert.Contains("selected", edge.ClassList);
    }

    [Fact]
    public void TwoEdgesWithDifferentStylesRenderIndependently()
    {
        var board = new Board();
        AddEdgeBetween(
            board,
            AddInstance(board, 0, 0),
            AddInstance(board, 100, 0),
            routingStyle: EdgeRouting.Straight,
            targetArrow: ArrowStyle.Arrow
        );
        AddEdgeBetween(
            board,
            AddInstance(board, 0, 200),
            AddInstance(board, 200, 300),
            routingStyle: EdgeRouting.Orthogonal,
            targetArrow: ArrowStyle.None
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var edges = canvas.FindAll(".edge-line");

        Assert.Equal(2, edges.Count);
        Assert.Equal("line", edges[0].TagName, ignoreCase: true);
        Assert.Equal("url(#edge-arrow)", edges[0].GetAttribute("marker-end"));
        Assert.Equal("path", edges[1].TagName, ignoreCase: true);
        Assert.Null(edges[1].GetAttribute("marker-end"));
    }

    [Fact]
    public void CommitEdgeStyleChangeAppliesTheChangeAsOneUndoableGesture()
    {
        var board = new Board();
        var edge = AddEdgeBetween(
            board,
            AddInstance(board, 100, 100),
            AddInstance(board, 250, 100)
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.InvokeAsync(
            () =>
                canvas.Instance.CommitEdgeStyleChange(
                    edge.Id,
                    before: new EdgeStyle(EdgeRouting.Straight, ArrowStyle.None, ArrowStyle.Arrow),
                    after: new EdgeStyle(EdgeRouting.Orthogonal, ArrowStyle.Arrow, ArrowStyle.None)
                )
        );

        Assert.Equal(EdgeRouting.Orthogonal, edge.RoutingStyle);
        Assert.Equal(ArrowStyle.Arrow, edge.SourceArrow);
        Assert.Equal(ArrowStyle.None, edge.TargetArrow);
        Assert.Equal("path", canvas.Find(".edge-line").TagName, ignoreCase: true);

        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(EdgeRouting.Straight, edge.RoutingStyle);
        Assert.Equal(ArrowStyle.None, edge.SourceArrow);
        Assert.Equal(ArrowStyle.Arrow, edge.TargetArrow);
        Assert.Equal("line", canvas.Find(".edge-line").TagName, ignoreCase: true);

        canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(EdgeRouting.Orthogonal, edge.RoutingStyle);
        Assert.Equal(ArrowStyle.Arrow, edge.SourceArrow);
        Assert.Equal(ArrowStyle.None, edge.TargetArrow);
    }
}
