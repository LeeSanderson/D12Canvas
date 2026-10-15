using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// A frame's edge work is proportional to the edges touching what moved, not to the board: a pan
// routes nothing and redraws no edge, and a drag redraws only the dragged shape's edges.
public class DiagramCanvasEdgeRenderCostTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasEdgeRenderCostTests()
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

    private static ComponentInstance AddShape(Board board, double x, double y)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 100, 100)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static Edge Connect(Board board, ComponentInstance source, ComponentInstance target)
    {
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new AutoPortEndpoint(target.Id),
            routingStyle: EdgeRouting.Orthogonal
        );
        board.AddEdge(edge);
        return edge;
    }

    private static List<ComponentInstance> AddShapeGrid(Board board, int count) =>
        Enumerable
            .Range(0, count)
            .Select(i => AddShape(board, 200 * (i % 10), 200 * (i / 10)))
            .ToList();

    private static Dictionary<Guid, int> EdgeRenderCounts(
        IRenderedComponent<DiagramCanvas> canvas
    ) =>
        canvas
            .FindComponents<EdgeView>()
            .ToDictionary(edge => edge.Instance.EdgeId, edge => edge.RenderCount);

    [Fact]
    public async Task PanningABoardOfFiveHundredEdgesRoutesNothingAndRedrawsNoEdge()
    {
        var board = new Board();
        var shapes = AddShapeGrid(board, 100);
        for (var i = 0; i < 500; i++)
        {
            Connect(board, shapes[i % 100], shapes[(i * 7 + 1 + i / 100) % 100]);
        }

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var routesBefore = canvas.Instance.RoutesComputed;
        var rendersBefore = EdgeRenderCounts(canvas);
        Assert.Equal(500, rendersBefore.Count);

        await canvas.Pan((400, 300), (250, 180));

        Assert.Equal(routesBefore, canvas.Instance.RoutesComputed);
        Assert.Equal(rendersBefore, EdgeRenderCounts(canvas));
    }

    [Fact]
    public void DraggingOneShapeRedrawsOnlyTheEdgesAttachedToIt()
    {
        var board = new Board();
        var shapes = AddShapeGrid(board, 30);
        var hub = shapes[0];
        var hubEdges = shapes
            .Skip(1)
            .Take(10)
            .Select(other => Connect(board, hub, other).Id)
            .ToHashSet();
        for (var i = 11; i < 29; i++)
        {
            Connect(board, shapes[i], shapes[i + 1]);
        }

        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        var before = EdgeRenderCounts(canvas);

        canvas.DragOn(canvas.ContainerOf(hub.Id), (50, 50), (80, 130));

        var after = EdgeRenderCounts(canvas);
        var redrawn = after.Where(edge => edge.Value != before[edge.Key]).Select(edge => edge.Key);
        Assert.Equal(hubEdges.OrderBy(id => id), redrawn.OrderBy(id => id));
    }
}
