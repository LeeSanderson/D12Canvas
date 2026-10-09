using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Drag port-to-port creates an edge. A connector drag is a distinct gesture from drag-move/resize
// - it never mutates Bounds, and Board only ever learns about the new Edge once the gesture
// resolves what lies under the pointer on release. Ports show on a selected shape, so each drag
// starts by selecting its source, and on the shape under the pointer while the line is drawn.
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
            )
            .ReturnToOrigin();

    // From the source's right port at (200, 150) to the target's left port at (300, 150).
    private static void ConnectRightToLeft(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance source,
        ComponentInstance target
    ) =>
        canvas.DragConnectorToPort(
            canvas.SelectedPortSpan(source.Id, "Right"),
            (200, 150),
            (300, 150),
            target.Id,
            "Left"
        );

    [Fact]
    public void DraggingFromAPortToAnotherInstancesPortCreatesAnEdge()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var canvas = RenderBoard(board);

        ConnectRightToLeft(canvas, source, target);

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
    }

    [Fact]
    public async Task CreatingAnEdgeIsOneHistoryEntry()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var canvas = RenderBoard(board);

        ConnectRightToLeft(canvas, source, target);
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Empty(board.Edges);
    }

    [Fact]
    public void MidDragRendersAConnectorDragPreviewLineFollowingThePointer()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        AddInstance(board, 300, 100);
        var canvas = RenderBoard(board);

        canvas.PressElement(canvas.SelectedPortSpan(source.Id, "Right"), (200, 150));
        canvas.MoveTo((230, 155));

        Assert.Empty(canvas.FindAll(".edge-line"));
        var preview = canvas.Find(".connector-drag-preview");
        Assert.Equal("200", preview.GetAttribute("x1"));
        Assert.Equal("150", preview.GetAttribute("y1"));
        Assert.Equal("230", preview.GetAttribute("x2"));
        Assert.Equal("155", preview.GetAttribute("y2"));
    }

    [Fact]
    public void AShapeShowsNoPortsUntilItIsSelected()
    {
        var board = new Board();
        var shape = AddInstance(board, 100, 100);
        var canvas = RenderBoard(board);

        Assert.Empty(canvas.FindAll(".port"));
        Assert.Empty(canvas.FindAll(".port-span"));

        canvas.ClickOn(canvas.ContainerOf(shape.Id));

        Assert.Equal(4, canvas.FindAll(".port").Count);
        Assert.Equal(4, canvas.FindAll(".port-span").Count);
    }

    // One number sizes every span: C# partitions with it and the stylesheet reads it from here.
    [Fact]
    public void ThePortTargetIsPublishedOnTheCanvasContent()
    {
        var canvas = RenderBoard(new Board());

        Assert.Contains(
            $"--d12-port-target: {ScreenPixels.PortTarget}px;",
            canvas.Find(".canvas-content").GetAttribute("style")
        );
    }

    [Fact]
    public void AMemberOfAMultiSelectionShowsNoPorts()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = RenderBoard(board);

        canvas.ClickOn(canvas.ContainerOf(first.Id));
        canvas.ClickOn(canvas.ContainerOf(second.Id), shift: true);

        Assert.Empty(canvas.FindAll(".port"));
        Assert.Empty(canvas.FindAll(".port-span"));
    }

    // Only the shape under the pointer lights up, and only while the line is drawn there. It shows
    // its ports and nothing else, since resizing is not what the drag is doing.
    [Fact]
    public void TheShapeUnderThePointerShowsItsPortsWhileTheLineIsOverIt()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var other = AddInstance(board, 300, 300);
        var canvas = RenderBoard(board);
        canvas.PressElement(canvas.SelectedPortSpan(source.Id, "Right"), (200, 150));

        canvas.MoveOver((350, 150), canvas.ContainerOf(target.Id));

        var lit = canvas.ContainerOf(target.Id);
        Assert.Equal(4, lit.QuerySelectorAll(".port-span").Length);
        Assert.Empty(lit.QuerySelectorAll(".resize-span"));
        Assert.Empty(lit.QuerySelectorAll(".resize-handle"));
        Assert.Empty(canvas.ContainerOf(other.Id).QuerySelectorAll(".port"));

        canvas.MoveOver((250, 250), null);
        Assert.Empty(canvas.ContainerOf(target.Id).QuerySelectorAll(".port"));

        canvas.MoveOver((350, 150), canvas.ContainerOf(target.Id));
        canvas.ReleaseOver((350, 150), canvas.ContainerOf(target.Id));
        Assert.Empty(canvas.ContainerOf(target.Id).QuerySelectorAll(".port"));
    }

    [Fact]
    public void ACreatedEdgeRendersAsALineBetweenBothPortsAndThePreviewIsGone()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var canvas = RenderBoard(board);

        ConnectRightToLeft(canvas, source, target);

        Assert.Empty(canvas.FindAll(".connector-drag-preview"));
        var line = canvas.Find(".edge-line");
        Assert.Equal("200", line.GetAttribute("x1"));
        Assert.Equal("150", line.GetAttribute("y1"));
        Assert.Equal("300", line.GetAttribute("x2"));
        Assert.Equal("150", line.GetAttribute("y2"));
    }

    // Dropping on empty canvas creates the edge with a floating endpoint at the release point -
    // see DiagramCanvasFloatingEndpointTests for the rest of that coverage.
    [Fact]
    public void DroppingOnEmptyCanvasCreatesAnEdgeWithAFloatingEndpoint()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        AddInstance(board, 300, 100);
        var canvas = RenderBoard(board);

        canvas.DragConnector(canvas.SelectedPortSpan(source.Id, "Right"), (200, 150), (240, 400));

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new FloatingEndpoint(240, 400), edge.Target);
        Assert.Empty(canvas.FindAll(".connector-drag-preview"));
    }

    [Fact]
    public void DroppingOnAnotherShapesBodyAttachesAtTheSideFacingTheSourceAndKeepsChoosing()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var canvas = RenderBoard(board);

        canvas.DragConnector(
            canvas.SelectedPortSpan(source.Id, "Right"),
            (200, 150),
            (340, 140),
            over: canvas.ContainerOf(target.Id)
        );

        Assert.Equal(new AutoPortEndpoint(target.Id), Assert.Single(board.Edges).Target);
        var line = canvas.Find(".edge-line");
        Assert.Equal(("300", "150"), (line.GetAttribute("x2"), line.GetAttribute("y2")));

        target.Bounds = new Bounds(100, 400, 100, 100);
        canvas.Render();

        line = canvas.Find(".edge-line");
        Assert.Equal(("150", "400"), (line.GetAttribute("x2"), line.GetAttribute("y2")));
    }

    [Fact]
    public void DroppingOnTheSourceShapesOwnBodyCreatesNoEdge()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var canvas = RenderBoard(board);

        canvas.DragConnector(
            canvas.SelectedPortSpan(source.Id, "Right"),
            (200, 150),
            (140, 140),
            over: canvas.ContainerOf(source.Id)
        );

        Assert.Empty(board.Edges);
    }

    [Fact]
    public void DroppingBackOnTheSameStartingPortCreatesNoEdge()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var canvas = RenderBoard(board);

        var sourcePort = canvas.SelectedPortSpan(source.Id, "Right");
        canvas.PressElement(sourcePort, (200, 150));
        canvas.MoveTo((250, 150));
        canvas.MoveTo((200, 150));
        canvas.ReleaseOver((200, 150), sourcePort);

        Assert.Empty(board.Edges);
    }

    [Fact]
    public void StartingADragOnASelectedInstancesPortNeverInitiatesAMove()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        AddInstance(board, 300, 100);
        var canvas = RenderBoard(board);

        canvas.DragConnector(canvas.SelectedPortSpan(source.Id, "Right"), (200, 150), (400, 400));

        Assert.Equal(new Bounds(100, 100, 100, 100), source.Bounds);
    }

    // The border is shared between connecting and resizing: the stretch around a side's midpoint
    // pulls a connector, and the stretches between it and the corners resize that side.
    [Fact]
    public void ASidesMiddlePullsAConnectorAndTheStretchNearerACornerResizes()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var canvas = RenderBoard(board);
        canvas.ClickOn(canvas.ContainerOf(source.Id));
        var container = canvas.ContainerOf(source.Id);

        Assert.Equal(
            "Right",
            container.QuerySelector(".port-span-right")!.GetAttribute("data-d12-part")
        );
        Assert.Equal(
            ["right", "right"],
            container
                .QuerySelectorAll(".resize-span-right")
                .Select(span => span.GetAttribute("data-d12-part"))
        );

        canvas.DragHandle(container.QuerySelector(".resize-span-right")!, (200, 130), (240, 130));

        Assert.Empty(board.Edges);
        Assert.Equal(new Bounds(100, 100, 140, 100), source.Bounds);
    }

    [Fact]
    public void EscapeCancelsAnInProgressConnectorDragAndTheReleaseCreatesNothing()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var canvas = RenderBoard(board);

        canvas.PressElement(canvas.SelectedPortSpan(source.Id, "Right"), (200, 150));
        canvas.MoveTo((300, 300));

        canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        Assert.Empty(canvas.FindAll(".connector-drag-preview"));

        canvas.MoveTo((400, 400));
        canvas.ReleaseOver((400, 400));

        Assert.Empty(board.Edges);
        Assert.Empty(canvas.FindAll(".connector-drag-preview"));
        Assert.Equal(new Bounds(100, 100, 100, 100), source.Bounds);
    }

    [Fact]
    public void AnAttachedEdgeTracksItsSourceInstanceAfterItMoves()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var canvas = RenderBoard(board);

        ConnectRightToLeft(canvas, source, target);
        Assert.Single(board.Edges);

        canvas.DragOn(canvas.ContainerOf(source.Id), (120, 120), (220, 170));

        Assert.Equal(new Bounds(200, 150, 100, 100), source.Bounds);

        var line = canvas.Find(".edge-line");
        Assert.Equal("300", line.GetAttribute("x1"));
        Assert.Equal("200", line.GetAttribute("y1"));
    }

    [Fact]
    public void AnAttachedEdgeTracksItsSourceInstanceAfterItResizes()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 300, 100);
        var canvas = RenderBoard(board);

        ConnectRightToLeft(canvas, source, target);
        Assert.Single(board.Edges);

        canvas.DragHandle(
            canvas.ContainerOf(source.Id).QuerySelector(".resize-handle.bottom-right")!,
            (200, 200),
            (240, 220)
        );

        Assert.Equal(new Bounds(100, 100, 140, 120), source.Bounds);

        var line = canvas.Find(".edge-line");
        Assert.Equal("240", line.GetAttribute("x1"));
        Assert.Equal("160", line.GetAttribute("y1"));
    }

    [Fact]
    public void ThePreviewEndsAtTheBoardPointUnderThePointerWhenZoomedIn()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var canvas = RenderBoard(board);

        canvas.ZoomIn(); // zooms to scale 1.1

        // Container coordinates scale with zoom (same ToBoardPoint conversion every other gesture
        // uses), so a pointer at 1.1 times a board point is over that point.
        canvas.PressElement(canvas.SelectedPortSpan(source.Id, "Right"), (200 * 1.1, 150 * 1.1));
        canvas.MoveTo((250 * 1.1, 300 * 1.1));

        var preview = canvas.Find(".connector-drag-preview");
        Assert.Equal(250, double.Parse(preview.GetAttribute("x2")!), 6);
        Assert.Equal(300, double.Parse(preview.GetAttribute("y2")!), 6);
    }
}
