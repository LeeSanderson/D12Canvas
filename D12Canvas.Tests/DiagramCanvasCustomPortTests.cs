using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Custom ports - instance-scoped runtime state an end user adds via a double-click on one of the
// four border strips (ComponentContainer.razor), fractionally positioned so they survive
// move/resize, attachable exactly like a standard port.
public class DiagramCanvasCustomPortTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasCustomPortTests()
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Empty(canvas.FindAll(".port-strip"));

        canvas.Find(".component-container").Click();

        Assert.NotEmpty(canvas.FindAll(".port-strip"));
    }

    [Fact]
    public void DoubleClickingTheLeftBorderStripAddsACustomPortAtTheClickedFraction()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, width: 100, height: 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        canvas
            .Find(".port-strip-left")
            .DoubleClick(new MouseEventArgs { OffsetX = 0, OffsetY = 25 });

        var port = Assert.Single(instance.CustomPorts);
        Assert.Equal(0, port.FractionX);
        Assert.Equal(0.25, port.FractionY);
        Assert.Single(canvas.FindAll(".custom-port"));
    }

    [Fact]
    public void DoubleClickingTheTopBorderStripAddsACustomPortAtTheClickedFraction()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, width: 100, height: 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        canvas
            .Find(".port-strip-top")
            .DoubleClick(new MouseEventArgs { OffsetX = 75, OffsetY = 0 });

        var port = Assert.Single(instance.CustomPorts);
        Assert.Equal(0.75, port.FractionX);
        Assert.Equal(0, port.FractionY);
    }

    [Fact]
    public void AddingACustomPortIsAnUndoableGesture()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        canvas.Find(".port-strip-left").DoubleClick(new MouseEventArgs { OffsetY = 25 });
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Click();

        // Custom port at fraction (1, 0.25) of the 100x100 source - board point (200, 125).
        canvas.Find(".port-strip-right").DoubleClick(new MouseEventArgs { OffsetY = 25 });
        var port = Assert.Single(source.CustomPorts);
        var customPort = canvas.Find(".custom-port");

        customPort.MouseDown(new MouseEventArgs { ClientX = 200, ClientY = 125 });
        var targetPort = canvas.FindAll(".component-container")[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 300, ClientY = 150 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 300, ClientY = 150 });

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Click();
        canvas.Find(".port-strip-right").DoubleClick(new MouseEventArgs { OffsetY = 25 });

        canvas.Find(".custom-port").MouseDown(new MouseEventArgs { ClientX = 200, ClientY = 125 });
        var targetPort = canvas.FindAll(".component-container")[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 300, ClientY = 150 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 300, ClientY = 150 });
        Assert.Single(board.Edges);

        // Move the source instance (already selected) via a drag on its own body.
        var sourceContainer = canvas.FindAll(".component-container")[0];
        sourceContainer.MouseDown(new MouseEventArgs { ClientX = 120, ClientY = 120 });
        sourceContainer.MouseMove(new MouseEventArgs { ClientX = 220, ClientY = 170 });
        sourceContainer.MouseUp(new MouseEventArgs { ClientX = 220, ClientY = 170 });

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Click();
        canvas.Find(".port-strip-right").DoubleClick(new MouseEventArgs { OffsetY = 25 });

        canvas.Find(".custom-port").MouseDown(new MouseEventArgs { ClientX = 200, ClientY = 125 });
        var targetPort = canvas.FindAll(".component-container")[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 300, ClientY = 150 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 300, ClientY = 150 });
        Assert.Single(board.Edges);

        var handle = canvas
            .FindAll(".component-container")[0]
            .QuerySelector(".resize-handle.bottom-right")!;
        handle.MouseDown(new MouseEventArgs { ClientX = 200, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 240, ClientY = 220 });
        handle.MouseUp(new MouseEventArgs { ClientX = 240, ClientY = 220 });

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Click();
        canvas.Find(".port-strip-right").DoubleClick(new MouseEventArgs { OffsetY = 25 });
        var port = Assert.Single(source.CustomPorts);

        canvas.Find(".custom-port").MouseDown(new MouseEventArgs { ClientX = 200, ClientY = 125 });
        var targetPort = canvas.FindAll(".component-container")[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 300, ClientY = 150 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 300, ClientY = 150 });

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
