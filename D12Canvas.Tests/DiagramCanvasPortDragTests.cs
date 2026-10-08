using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Drag port-to-port creates an edge. A connector drag is a distinct gesture from drag-move/resize
// - it never mutates Bounds, and Board only ever learns about the new Edge once the gesture
// resolves what lies under the pointer on release.
public class DiagramCanvasPortDragTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasPortDragTests()
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

    // From the first instance's right port at (150, 125) to the second's left port at (250, 125).
    private static void ConnectRightToLeft(IRenderedComponent<DiagramCanvas> canvas)
    {
        var containers = canvas.FindAll(".component-container");
        canvas.DragConnector(
            containers[0].QuerySelector(".port-right")!,
            (150, 125),
            (250, 125),
            over: containers[1].QuerySelector(".port-left")
        );
    }

    [Fact]
    public void DraggingFromAPortToAnotherInstancesPortCreatesAnEdge()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        ConnectRightToLeft(canvas);

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
    }

    [Fact]
    public async Task CreatingAnEdgeIsOneHistoryEntry()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        ConnectRightToLeft(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Empty(board.Edges);
    }

    [Fact]
    public void MidDragRendersAConnectorDragPreviewLineFollowingThePointer()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var sourcePort = canvas.FindAll(".component-container")[0].QuerySelector(".port-right")!;
        canvas.PressElement(sourcePort, (150, 125));
        canvas.MoveTo((180, 130));

        Assert.Empty(canvas.FindAll(".edge-line"));
        var preview = canvas.Find(".connector-drag-preview");
        Assert.Equal("150", preview.GetAttribute("x1"));
        Assert.Equal("125", preview.GetAttribute("y1"));
        Assert.Equal("180", preview.GetAttribute("x2"));
        Assert.Equal("130", preview.GetAttribute("y2"));
    }

    // A press below the threshold draws nothing, and every port shows only once the line does.
    [Fact]
    public void PortsShowOnEveryShapeOnlyWhileTheLineIsDrawn()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        var sourcePort = canvas.Find(".component-container").QuerySelector(".port-right")!;

        canvas.PressElement(sourcePort, (150, 125));
        Assert.Empty(canvas.FindAll(".connector-drag-preview"));
        Assert.DoesNotContain("connecting", canvas.Find(".diagram-canvas").ClassName);

        canvas.MoveTo((180, 130));
        Assert.Contains("connecting", canvas.Find(".diagram-canvas").ClassName);

        canvas.ReleaseOver((180, 130));
        Assert.DoesNotContain("connecting", canvas.Find(".diagram-canvas").ClassName);
    }

    [Fact]
    public void ACreatedEdgeRendersAsALineBetweenBothPortsAndThePreviewIsGone()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        ConnectRightToLeft(canvas);

        Assert.Empty(canvas.FindAll(".connector-drag-preview"));
        var line = canvas.Find(".edge-line");
        Assert.Equal("150", line.GetAttribute("x1"));
        Assert.Equal("125", line.GetAttribute("y1"));
        Assert.Equal("250", line.GetAttribute("x2"));
        Assert.Equal("125", line.GetAttribute("y2"));
    }

    // Dropping on empty canvas creates the edge with a floating endpoint at the release point -
    // see DiagramCanvasFloatingEndpointTests for the rest of that coverage.
    [Fact]
    public void DroppingOnEmptyCanvasCreatesAnEdgeWithAFloatingEndpoint()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var sourcePort = canvas.FindAll(".component-container")[0].QuerySelector(".port-right")!;
        canvas.DragConnector(sourcePort, (150, 125), (190, 400));

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new FloatingEndpoint(190, 400), edge.Target);
        Assert.Empty(canvas.FindAll(".connector-drag-preview"));
    }

    [Fact]
    public void DroppingOnAnotherShapesBodyAttachesAtTheSideFacingTheSourceAndKeepsChoosing()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var containers = canvas.FindAll(".component-container");
        canvas.DragConnector(
            containers[0].QuerySelector(".port-right")!,
            (150, 125),
            (270, 120),
            over: containers[1]
        );

        Assert.Equal(new AutoPortEndpoint(target.Id), Assert.Single(board.Edges).Target);
        var line = canvas.Find(".edge-line");
        Assert.Equal(("250", "125"), (line.GetAttribute("x2"), line.GetAttribute("y2")));

        target.Bounds = new Bounds(100, 300, 50, 50);
        canvas.Render();

        line = canvas.Find(".edge-line");
        Assert.Equal(("125", "300"), (line.GetAttribute("x2"), line.GetAttribute("y2")));
    }

    [Fact]
    public void DroppingOnTheSourceShapesOwnBodyCreatesNoEdge()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var container = canvas.Find(".component-container");
        canvas.DragConnector(
            container.QuerySelector(".port-right")!,
            (150, 125),
            (120, 120),
            over: container
        );

        Assert.Empty(board.Edges);
    }

    [Fact]
    public void DroppingBackOnTheSameStartingPortCreatesNoEdge()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var sourcePort = canvas.Find(".component-container").QuerySelector(".port-right")!;
        canvas.PressElement(sourcePort, (150, 125));
        canvas.MoveTo((200, 125));
        canvas.MoveTo((150, 125));
        canvas.ReleaseOver((150, 125), sourcePort);

        Assert.Empty(board.Edges);
    }

    [Fact]
    public void AClickOnAPortCreatesNothing()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ClickElement(canvas.Find(".component-container").QuerySelector(".port-right")!);

        Assert.Empty(board.Edges);
    }

    [Fact]
    public void StartingADragOnASelectedInstancesPortNeverInitiatesAMove()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        var sourcePort = canvas.FindAll(".component-container")[0].QuerySelector(".port-right")!;
        canvas.DragConnector(sourcePort, (150, 125), (400, 400));

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public void EscapeCancelsAnInProgressConnectorDragAndTheReleaseCreatesNothing()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        var sourcePort = canvas.FindAll(".component-container")[0].QuerySelector(".port-right")!;
        canvas.PressElement(sourcePort, (150, 125));
        canvas.MoveTo((300, 300));

        canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        Assert.Empty(canvas.FindAll(".connector-drag-preview"));

        canvas.MoveTo((400, 400));
        canvas.ReleaseOver((400, 400));

        Assert.Empty(board.Edges);
        Assert.Empty(canvas.FindAll(".connector-drag-preview"));
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public void AnAttachedEdgeTracksItsSourceInstanceAfterItMoves()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        ConnectRightToLeft(canvas);
        Assert.Single(board.Edges);

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        var sourceContainer = canvas.FindAll(".component-container")[0];
        canvas.DragOn(sourceContainer, (120, 120), (220, 170));

        Assert.Equal(new Bounds(200, 150, 50, 50), source.Bounds);

        var line = canvas.Find(".edge-line");
        Assert.Equal("250", line.GetAttribute("x1")); // source's new right port: 200 + 50
        Assert.Equal("175", line.GetAttribute("y1")); // 150 + 50/2
    }

    [Fact]
    public void AnAttachedEdgeTracksItsSourceInstanceAfterItResizes()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        AddInstance(board, 250, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        ConnectRightToLeft(canvas);
        Assert.Single(board.Edges);

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        canvas.DragHandle(
            canvas.FindAll(".component-container")[0].QuerySelector(".resize-handle.bottom-right")!,
            (150, 150),
            (190, 170)
        );

        Assert.Equal(new Bounds(100, 100, 90, 70), source.Bounds);

        var line = canvas.Find(".edge-line");
        Assert.Equal("190", line.GetAttribute("x1")); // source's new right port: 100 + 90
        Assert.Equal("135", line.GetAttribute("y1")); // 100 + 70/2
    }

    [Fact]
    public void ThePreviewEndsAtTheBoardPointUnderThePointerWhenZoomedIn()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ZoomIn(); // zooms to scale 1.1

        // Container coordinates scale with zoom (same ToBoardPoint conversion every other gesture
        // uses), so a pointer at 1.1 times a board point is over that point.
        var sourcePort = canvas.Find(".component-container").QuerySelector(".port-right")!;
        canvas.PressElement(sourcePort, (150 * 1.1, 125 * 1.1));
        canvas.MoveTo((200 * 1.1, 300 * 1.1));

        var preview = canvas.Find(".connector-drag-preview");
        Assert.Equal(200, double.Parse(preview.GetAttribute("x2")!), 6);
        Assert.Equal(300, double.Parse(preview.GetAttribute("y2")!), 6);
    }
}
