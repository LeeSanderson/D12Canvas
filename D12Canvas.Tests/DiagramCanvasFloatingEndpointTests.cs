using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Floating endpoints. Releasing a connector drag on empty canvas creates an edge with a floating
// endpoint (extending the port-to-port-only gesture); an existing edge's endpoint - attached or
// floating - can be re-dragged to reattach or detach it.
public class DiagramCanvasFloatingEndpointTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasFloatingEndpointTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();

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
        double width = 50,
        double height = 50
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
    public void AFloatingEndpointRendersAsAMarkerAtTheDropPoint()
    {
        var board = new Board();
        AddInstance(board, 100, 100); // right port at (150, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var sourcePort = canvas.Find(".component-container").QuerySelector(".port-right")!;
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var background = canvas.Find(".diagram-canvas");
        background.MouseMove(new MouseEventArgs { ClientX = 190, ClientY = 400 });
        background.MouseUp(new MouseEventArgs { ClientX = 190, ClientY = 400 });

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
        AddInstance(board, 100, 100); // right port at (150, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var sourcePort = canvas.Find(".component-container").QuerySelector(".port-right")!;
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var background = canvas.Find(".diagram-canvas");
        background.MouseMove(new MouseEventArgs { ClientX = 190, ClientY = 400 });
        background.MouseUp(new MouseEventArgs { ClientX = 190, ClientY = 400 });

        var transformBefore = canvas.Find(".canvas-content").GetAttribute("style");

        background.Wheel(new WheelEventArgs { DeltaY = -100 });

        var transformAfter = canvas.Find(".canvas-content").GetAttribute("style");
        Assert.NotEqual(transformBefore, transformAfter); // the zoom actually happened

        var marker = canvas.Find(".floating-endpoint");
        Assert.Equal("190", marker.GetAttribute("cx"));
        Assert.Equal("400", marker.GetAttribute("cy"));
    }

    // Each floating marker is independent - re-dragging one end of an edge shouldn't hide the
    // OTHER end's own floating marker, even though that edge's normal .edge-line is suppressed.
    [Fact]
    public void RepositioningOneEndpointDoesNotHideTheOtherEndsFloatingMarker()
    {
        var board = new Board();
        AddInstance(board, 100, 100); // right port at (150, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var sourcePort = canvas.Find(".component-container").QuerySelector(".port-right")!;
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var background = canvas.Find(".diagram-canvas");
        background.MouseMove(new MouseEventArgs { ClientX = 190, ClientY = 400 });
        background.MouseUp(new MouseEventArgs { ClientX = 190, ClientY = 400 });
        Assert.Single(canvas.FindAll(".floating-endpoint"));

        // Re-grab the SOURCE side (attached to the port) while the TARGET side is floating.
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });

        var marker = canvas.Find(".floating-endpoint");
        Assert.Equal("190", marker.GetAttribute("cx"));
        Assert.Equal("400", marker.GetAttribute("cy"));
    }

    [Fact]
    public void DraggingAFloatingEndpointOntoAPortAttachesIt()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        var target = AddInstance(board, 250, 100); // left port at (250, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0]
            .QuerySelector(".port-right")!
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var background = canvas.Find(".diagram-canvas");
        background.MouseMove(new MouseEventArgs { ClientX = 190, ClientY = 400 });
        background.MouseUp(new MouseEventArgs { ClientX = 190, ClientY = 400 });
        Assert.Single(board.Edges);

        var marker = canvas.Find(".floating-endpoint");
        marker.MouseDown(new MouseEventArgs { ClientX = 190, ClientY = 400 });
        var targetPort = canvas.FindAll(".component-container")[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 250, ClientY = 125 });

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
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        var target = AddInstance(board, 250, 100); // left port at (250, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0]
            .QuerySelector(".port-right")!
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var targetPort = containers[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        Assert.Single(board.Edges);

        // Grab the already-attached target port and drag it away to empty canvas.
        targetPort.MouseDown(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        var background = canvas.Find(".diagram-canvas");
        background.MouseMove(new MouseEventArgs { ClientX = 400, ClientY = 400 });
        background.MouseUp(new MouseEventArgs { ClientX = 400, ClientY = 400 });

        var edge = Assert.Single(board.Edges); // detaching moves the endpoint, never creates a new edge
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new FloatingEndpoint(400, 400), edge.Target);
        var marker = canvas.Find(".floating-endpoint");
        Assert.Equal("400", marker.GetAttribute("cx"));
        Assert.Equal("400", marker.GetAttribute("cy"));
    }

    [Fact]
    public void EscapeWhileRepositioningAnExistingEdgesEndpointLeavesItUnchanged()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        var target = AddInstance(board, 250, 100); // left port at (250, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0]
            .QuerySelector(".port-right")!
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var targetPort = containers[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 250, ClientY = 125 });

        targetPort.MouseDown(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        var background = canvas.Find(".diagram-canvas");
        background.MouseMove(new MouseEventArgs { ClientX = 400, ClientY = 400 });
        background.MouseUp(new MouseEventArgs { ClientX = 400, ClientY = 400 });

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
        Assert.Empty(canvas.FindAll(".floating-endpoint"));
    }

    // Guard: repositioning one endpoint onto the port the edge's OTHER endpoint already occupies
    // would collapse the edge onto a single point - left unchanged instead of creating a self-loop.
    [Fact]
    public void DraggingAnEndpointOntoTheEdgesOtherEndpointsPortLeavesTheEdgeUnchanged()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        var target = AddInstance(board, 250, 100); // left port at (250, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        var sourcePort = containers[0].QuerySelector(".port-right")!;
        var targetPort = containers[1].QuerySelector(".port-left")!;
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        targetPort.MouseMove(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 250, ClientY = 125 });

        // Re-grab the source port (now attached) and drop it exactly on the target's own port.
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        targetPort.MouseMove(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 250, ClientY = 125 });

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
    }
}
