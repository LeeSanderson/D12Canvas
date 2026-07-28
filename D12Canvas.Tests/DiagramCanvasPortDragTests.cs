using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 48: drag port-to-port creates an edge (ADR 0005). A connector drag is a distinct gesture
// from drag-move/resize - it never mutates Bounds, and Board only ever learns about the new Edge
// once the gesture resolves against a target port on release.
public class DiagramCanvasPortDragTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasPortDragTests()
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
    public void DraggingFromAPortToAnotherInstancesPortCreatesAnEdge()
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

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
    }

    [Fact]
    public void MidDragRendersAConnectorDragPreviewLineFollowingThePointer()
    {
        var board = new Board();
        AddInstance(board, 100, 100); // right port at (150, 125)
        AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var sourcePort = canvas.FindAll(".component-container")[0].QuerySelector(".port-right")!;
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 180, ClientY = 130 });

        Assert.Empty(canvas.FindAll(".edge-line"));
        var preview = canvas.Find(".connector-drag-preview");
        Assert.Equal("150", preview.GetAttribute("x1"));
        Assert.Equal("125", preview.GetAttribute("y1"));
        Assert.Equal("180", preview.GetAttribute("x2"));
        Assert.Equal("130", preview.GetAttribute("y2"));
    }

    [Fact]
    public void ACreatedEdgeRendersAsALineBetweenBothPortsAndThePreviewIsGone()
    {
        var board = new Board();
        AddInstance(board, 100, 100); // right port at (150, 125)
        AddInstance(board, 250, 100); // left port at (250, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0]
            .QuerySelector(".port-right")!
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var targetPort = containers[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 250, ClientY = 125 });

        Assert.Empty(canvas.FindAll(".connector-drag-preview"));
        var line = canvas.Find(".edge-line");
        Assert.Equal("150", line.GetAttribute("x1"));
        Assert.Equal("125", line.GetAttribute("y1"));
        Assert.Equal("250", line.GetAttribute("x2"));
        Assert.Equal("125", line.GetAttribute("y2"));
    }

    // Ticket 49: dropping on empty canvas now creates the edge with a floating endpoint at the
    // release point, rather than cancelling the gesture (ticket 48's original placeholder
    // behaviour) - see DiagramCanvasFloatingEndpointTests for the rest of ticket 49's coverage.
    [Fact]
    public void DroppingOnEmptyCanvasCreatesAnEdgeWithAFloatingEndpoint()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var sourcePort = canvas.FindAll(".component-container")[0].QuerySelector(".port-right")!;
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var background = canvas.Find(".diagram-canvas");
        background.MouseMove(new MouseEventArgs { ClientX = 190, ClientY = 400 });
        background.MouseUp(new MouseEventArgs { ClientX = 190, ClientY = 400 });

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new FloatingEndpoint(190, 400), edge.Target);
        Assert.Empty(canvas.FindAll(".connector-drag-preview"));
    }

    [Fact]
    public void DroppingBackOnTheSameStartingPortCreatesNoEdge()
    {
        var board = new Board();
        AddInstance(board, 100, 100); // right port at (150, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var sourcePort = canvas.Find(".component-container").QuerySelector(".port-right")!;
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        sourcePort.MouseUp(new MouseEventArgs { ClientX = 150, ClientY = 125 });

        Assert.Empty(board.Edges);
    }

    [Fact]
    public void StartingADragOnASelectedInstancesPortNeverInitiatesAMove()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100); // right port at (150, 125)
        AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.FindAll(".component-container")[0].Click();

        var sourcePort = canvas.FindAll(".component-container")[0].QuerySelector(".port-right")!;
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var background = canvas.Find(".diagram-canvas");
        background.MouseMove(new MouseEventArgs { ClientX = 400, ClientY = 400 });
        background.MouseUp(new MouseEventArgs { ClientX = 400, ClientY = 400 });

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public void EscapeCancelsAnInProgressConnectorDragAndFurtherMovementStillDoesNotMoveTheInstance()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100); // right port at (150, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.FindAll(".component-container")[0].Click();

        var sourcePort = canvas.FindAll(".component-container")[0].QuerySelector(".port-right")!;
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });

        canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        var container = canvas.Find(".component-container");
        container.MouseMove(new MouseEventArgs { ClientX = 400, ClientY = 400 });
        container.MouseUp(new MouseEventArgs { ClientX = 400, ClientY = 400 });

        Assert.Empty(board.Edges);
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public void AnAttachedEdgeTracksItsSourceInstanceAfterItMoves()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        AddInstance(board, 250, 100); // left port at (250, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0]
            .QuerySelector(".port-right")!
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var targetPort = containers[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        Assert.Single(board.Edges);

        // Select and drag-move the source instance (ticket 30's established gesture).
        canvas.FindAll(".component-container")[0].Click();
        var sourceContainer = canvas.FindAll(".component-container")[0];
        sourceContainer.MouseDown(new MouseEventArgs { ClientX = 120, ClientY = 120 });
        sourceContainer.MouseMove(new MouseEventArgs { ClientX = 220, ClientY = 170 });
        sourceContainer.MouseUp(new MouseEventArgs { ClientX = 220, ClientY = 170 });

        Assert.Equal(new Bounds(200, 150, 50, 50), source.Bounds);

        var line = canvas.Find(".edge-line");
        Assert.Equal("250", line.GetAttribute("x1")); // source's new right port: 200 + 50
        Assert.Equal("175", line.GetAttribute("y1")); // 150 + 50/2
    }

    [Fact]
    public void AnAttachedEdgeTracksItsSourceInstanceAfterItResizes()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        AddInstance(board, 250, 100); // left port at (250, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0]
            .QuerySelector(".port-right")!
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var targetPort = containers[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        Assert.Single(board.Edges);

        // Select and resize the source instance via its bottom-right handle (ticket 31's
        // established gesture).
        canvas.FindAll(".component-container")[0].Click();
        var handle = canvas
            .FindAll(".component-container")[0]
            .QuerySelector(".resize-handle.bottom-right")!;
        handle.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 150 });
        handle.MouseMove(new MouseEventArgs { ClientX = 190, ClientY = 170 });
        handle.MouseUp(new MouseEventArgs { ClientX = 190, ClientY = 170 });

        Assert.Equal(new Bounds(100, 100, 90, 70), source.Bounds);

        var line = canvas.Find(".edge-line");
        Assert.Equal("190", line.GetAttribute("x1")); // source's new right port: 100 + 90
        Assert.Equal("135", line.GetAttribute("y1")); // 100 + 70/2
    }

    [Fact]
    public void DraggingFromAPortToAnotherInstancesPortCreatesAnEdgeWhenZoomedIn()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        var target = AddInstance(board, 250, 100); // left port at (250, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 }); // zooms to scale 1.1

        // Client coordinates scale with zoom (same ToBoardPoint conversion every other gesture
        // uses) - board-space port positions don't move, so the client point landing exactly on
        // one scales by the same factor. Regression case for a fixed bug where the connector
        // drag's hit-tolerance was incorrectly descaled by zoom a second time, on top of this.
        var scale = 1.0 + 0.1;
        var containers = canvas.FindAll(".component-container");
        var sourcePort = containers[0].QuerySelector(".port-right")!;
        var targetPort = containers[1].QuerySelector(".port-left")!;

        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150 * scale, ClientY = 125 * scale });
        targetPort.MouseMove(new MouseEventArgs { ClientX = 250 * scale, ClientY = 125 * scale });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 250 * scale, ClientY = 125 * scale });

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
    }
}
