using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Custom ports - instance-scoped runtime state, fractionally positioned on a border so they
// survive move/resize, shown and attachable exactly like a standard port.
public class DiagramCanvasCustomPortTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasCustomPortTests()
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

    private IRenderedComponent<DiagramCanvas> RenderBoard(Board board) =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

    // A custom port at fraction (1, 0.25) of the 100x100 source, board point (200, 125), dragged
    // to the target's left port at (300, 150).
    private static void ConnectFromCustomPort(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance source,
        PortDef port,
        ComponentInstance target
    ) =>
        canvas.DragConnectorToPort(
            canvas.SelectedPortSpan(source.Id, port.Id.ToString()),
            (200, 125),
            (300, 150),
            target.Id,
            "Left"
        );

    private static PortDef AddRightPort(ComponentInstance instance)
    {
        var port = new PortDef(1, 0.25);
        instance.CustomPorts.Add(port);
        return port;
    }

    [Fact]
    public void ACustomPortShowsItsDotAndSpanOnlyOnASelectedInstance()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var port = AddRightPort(instance);
        var canvas = RenderBoard(board);

        Assert.Empty(canvas.FindAll(".custom-port"));

        canvas.ClickOn(canvas.ContainerOf(instance.Id));

        Assert.Single(canvas.FindAll(".custom-port"));
        Assert.Equal(
            "port-span port-span-right",
            canvas.PortSpanOf(instance.Id, port.Id.ToString()).ClassName
        );
    }

    [Fact]
    public void ADoublePressOnAPortSpanAddsNoPort()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderBoard(board);

        canvas.DoubleClickElement(canvas.SelectedPortSpan(instance.Id, "Left"), at: (100, 150));

        Assert.Empty(instance.CustomPorts);
    }

    [Fact]
    public void DraggingFromACustomPortToAnotherInstancesPortCreatesAnEdge()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var port = AddRightPort(source);
        var canvas = RenderBoard(board);

        ConnectFromCustomPort(canvas, source, port, target);

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new CustomPortEndpoint(source.Id, port.Id), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
    }

    [Fact]
    public void ACustomPortAttachedEdgeTracksItsInstanceAfterItMoves()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var port = AddRightPort(source);
        var canvas = RenderBoard(board);
        ConnectFromCustomPort(canvas, source, port, target);
        Assert.Single(board.Edges);

        canvas.DragOn(canvas.ContainerOf(source.Id), (120, 120), (220, 170));

        Assert.Equal(new Bounds(200, 150, 100, 100), source.Bounds);
        var line = canvas.Find(".edge-line");
        Assert.Equal("300", line.GetAttribute("x1")); // fraction (1, 0.25) of the moved Bounds
        Assert.Equal("175", line.GetAttribute("y1")); // 150 + 100*0.25
    }

    [Fact]
    public void ACustomPortAttachedEdgeTracksItsInstanceAfterItResizes()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var port = AddRightPort(source);
        var canvas = RenderBoard(board);
        ConnectFromCustomPort(canvas, source, port, target);
        Assert.Single(board.Edges);

        canvas.DragHandle(
            canvas.ContainerOf(source.Id).QuerySelector(".resize-handle.bottom-right")!,
            (200, 200),
            (240, 220)
        );

        Assert.Equal(new Bounds(100, 100, 140, 120), source.Bounds);
        var line = canvas.Find(".edge-line");
        Assert.Equal("240", line.GetAttribute("x1")); // fraction (1, 0.25): 100 + 140
        Assert.Equal("130", line.GetAttribute("y1")); // 100 + 120*0.25
    }

    [Fact]
    public void ACustomPortAttachedEdgeRoundTripsThroughJsonSerialization()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var port = AddRightPort(source);
        var canvas = RenderBoard(board);
        ConnectFromCustomPort(canvas, source, port, target);

        var serializer = new D12Canvas.Persistence.BoardJsonSerializer(
            Services.GetRequiredService<IComponentRegistry>()
        );
        var json = serializer.Serialize(board);
        var reloaded = serializer.Deserialize(json);

        var reloadedSource = reloaded.GetComponent(source.Id)!;
        Assert.Equal(new[] { port }, reloadedSource.CustomPorts);
        var reloadedEdge = Assert.Single(reloaded.Edges);
        Assert.Equal(new CustomPortEndpoint(source.Id, port.Id), reloadedEdge.Source);
    }
}
