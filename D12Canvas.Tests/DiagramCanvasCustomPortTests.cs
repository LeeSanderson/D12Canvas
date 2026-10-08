using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Custom ports - instance-scoped runtime state an end user adds via a double-press on one of the
// four border strips (ComponentContainer.razor), fractionally positioned so they survive
// move/resize, attachable exactly like a standard port.
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

    private static ComponentInstance AddInstance(
        Board board,
        double x,
        double y,
        double width = 100,
        double height = 100
    )
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, width, height)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void ABorderStripIsOnlyRenderedForASelectedNotMultiSelectedInstance()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        Assert.Empty(canvas.FindAll(".port-strip"));

        canvas.ClickOn(canvas.Find(".component-container"));

        Assert.NotEmpty(canvas.FindAll(".port-strip"));
    }

    [Fact]
    public void DoublePressingTheLeftBorderStripAddsACustomPortAtTheClickedFraction()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, width: 100, height: 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));

        canvas.DoubleClickElement(canvas.Find(".port-strip-left"), at: (100, 125));

        var port = Assert.Single(instance.CustomPorts);
        Assert.Equal(0, port.FractionX);
        Assert.Equal(0.25, port.FractionY);
        Assert.Single(canvas.FindAll(".custom-port"));
    }

    [Fact]
    public void DoublePressingTheTopBorderStripAddsACustomPortAtTheClickedFraction()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, width: 100, height: 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));

        canvas.DoubleClickElement(canvas.Find(".port-strip-top"), at: (175, 100));

        var port = Assert.Single(instance.CustomPorts);
        Assert.Equal(0.75, port.FractionX);
        Assert.Equal(0, port.FractionY);
    }

    [Fact]
    public void AddingACustomPortIsAnUndoableGesture()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));

        canvas.DoubleClickElement(canvas.Find(".port-strip-left"), at: (100, 125));
        Assert.Single(instance.CustomPorts);

        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Empty(instance.CustomPorts);
        Assert.Empty(canvas.FindAll(".custom-port"));

        canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        Assert.Single(instance.CustomPorts);
    }

    [Fact]
    public void DraggingFromACustomPortToAnotherInstancesPortCreatesAnEdge()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100, width: 100, height: 100);
        var target = AddInstance(board, 300, 100); // left port at (300, 150)
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        // Custom port at fraction (1, 0.25) of the 100x100 source - board point (200, 125).
        canvas.DoubleClickElement(canvas.Find(".port-strip-right"), at: (200, 125));
        var port = Assert.Single(source.CustomPorts);
        var customPort = canvas.Find(".custom-port");

        canvas.PressElement(customPort, (200, 125));
        var targetPort = canvas.FindAll(".component-container")[1].QuerySelector(".port-left")!;
        canvas.MoveTo((300, 150));
        canvas.ReleaseOver((300, 150), targetPort);

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new CustomPortEndpoint(source.Id, port.Id), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
    }

    [Fact]
    public void ACustomPortAttachedEdgeTracksItsInstanceAfterItMoves()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100, width: 100, height: 100);
        AddInstance(board, 300, 100); // left port at (300, 150)
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        canvas.DoubleClickElement(canvas.Find(".port-strip-right"), at: (200, 125));

        canvas.PressElement(canvas.Find(".custom-port"), (200, 125));
        var targetPort = canvas.FindAll(".component-container")[1].QuerySelector(".port-left")!;
        canvas.MoveTo((300, 150));
        canvas.ReleaseOver((300, 150), targetPort);
        Assert.Single(board.Edges);

        // Move the source instance (already selected) via a drag on its own body.
        var sourceContainer = canvas.FindAll(".component-container")[0];
        canvas.DragOn(sourceContainer, (120, 120), (220, 170));

        Assert.Equal(new Bounds(200, 150, 100, 100), source.Bounds);
        var line = canvas.Find(".edge-line");
        Assert.Equal("300", line.GetAttribute("x1")); // fraction (1, 0.25) of the moved Bounds
        Assert.Equal("175", line.GetAttribute("y1")); // 150 + 100*0.25
    }

    [Fact]
    public void ACustomPortAttachedEdgeTracksItsInstanceAfterItResizes()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100, width: 100, height: 100);
        AddInstance(board, 300, 100); // left port at (300, 150)
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        canvas.DoubleClickElement(canvas.Find(".port-strip-right"), at: (200, 125));

        canvas.PressElement(canvas.Find(".custom-port"), (200, 125));
        var targetPort = canvas.FindAll(".component-container")[1].QuerySelector(".port-left")!;
        canvas.MoveTo((300, 150));
        canvas.ReleaseOver((300, 150), targetPort);
        Assert.Single(board.Edges);

        canvas.DragHandle(
            canvas.FindAll(".component-container")[0].QuerySelector(".resize-handle.bottom-right")!,
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
        var source = AddInstance(board, 100, 100, width: 100, height: 100);
        AddInstance(board, 300, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        canvas.DoubleClickElement(canvas.Find(".port-strip-right"), at: (200, 125));
        var port = Assert.Single(source.CustomPorts);

        canvas.PressElement(canvas.Find(".custom-port"), (200, 125));
        var targetPort = canvas.FindAll(".component-container")[1].QuerySelector(".port-left")!;
        canvas.MoveTo((300, 150));
        canvas.ReleaseOver((300, 150), targetPort);

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
