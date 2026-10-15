using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The viewport stays live while a pointer gesture owns the press, and whatever the pointer holds
// stays under it: every viewport change re-runs the live gesture at the last pointer position,
// promotes a press still pointing to active, and re-anchors a held pan so the two movements add.
public class DiagramCanvasLiveViewportTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasLiveViewportTests()
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
            new Bounds(x, y, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(
        Board board,
        WheelDeviceProfile profile = WheelDeviceProfile.Mouse
    ) =>
        Render<DiagramCanvas>(parameters =>
                parameters
                    .Add(p => p.Board, board)
                    .Add(p => p.SnapToGrid, false)
                    .Add(p => p.WheelDeviceProfile, profile)
            )
            .ReturnToOrigin();

    private static (double X, double Y) BoardPointUnder(
        IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) at
    )
    {
        var tracker = canvas.Instance.ZoomPanTracker;
        return ((at.X - tracker.PanX) / tracker.Scale, (at.Y - tracker.PanY) / tracker.Scale);
    }

    [Fact]
    public void WheelZoomingMidDragKeepsTheDraggedShapeUnderThePointerAndTheDragContinues()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board);

        canvas.PressOn(canvas.Find(".component-container"), (120, 120));
        canvas.MoveTo((200, 200));
        canvas.WheelAt((200, 200), -CanvasViewport.Notch);
        canvas.MoveTo((260, 230));
        canvas.ReleaseAt((260, 230));

        var underPointer = BoardPointUnder(canvas, (260, 230));
        Assert.NotEqual(1, canvas.Instance.ZoomPanTracker.Scale);
        Assert.Equal(underPointer.X - 20, instance.Bounds.X, precision: 8);
        Assert.Equal(underPointer.Y - 20, instance.Bounds.Y, precision: 8);
    }

    [Fact]
    public void APageUpZoomMidDragKeepsTheDraggedShapeUnderAStillPointer()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board);

        canvas.PressOn(canvas.Find(".component-container"), (120, 120));
        canvas.MoveTo((200, 200));
        canvas.ZoomIn();
        canvas.ReleaseAt((200, 200));

        var underPointer = BoardPointUnder(canvas, (200, 200));
        Assert.Equal(underPointer.X - 20, instance.Bounds.X, precision: 8);
        Assert.Equal(underPointer.Y - 20, instance.Bounds.Y, precision: 8);
    }

    [Fact]
    public void AViewportChangePromotesAPressStillPointingAndTellsTheListener()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board, WheelDeviceProfile.Trackpad);

        canvas.PressOn(canvas.Find(".component-container"), (120, 120));
        canvas.WheelAt((120, 120), deltaY: 100, coarse: false);
        canvas.ReleaseAt((120, 120));

        Assert.Equal(new Bounds(100, 200, 50, 50), instance.Bounds);
        PointerListener.VerifyInvoke("promote", calledTimes: 1);
    }

    [Fact]
    public void AnActivePressIsNotPromotedAgain()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board, WheelDeviceProfile.Trackpad);

        canvas.PressOn(canvas.Find(".component-container"), (120, 120));
        canvas.MoveTo((150, 150));
        canvas.WheelAt((150, 150), deltaY: 100, coarse: false);
        canvas.WheelAt((150, 150), deltaY: 100, coarse: false);

        PointerListener.VerifyNotInvoke("promote");
    }

    [Fact]
    public async Task ASecondaryPressFollowedByViewportInputBecomesAPanAndOpensNoMenu()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board, WheelDeviceProfile.Trackpad);

        await canvas.Press(
            120,
            120,
            PointerPress.SecondaryButton,
            role: HitRole.Instance,
            entityId: instance.Id
        );
        canvas.WheelAt((120, 120), deltaY: 40, coarse: false);
        await canvas.Release(120, 120, PointerPress.SecondaryButton);

        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public async Task AWheelPanDuringAHeldPanAddsToIt()
    {
        var canvas = RenderCanvas(new Board());
        var tracker = canvas.Instance.ZoomPanTracker;

        await canvas.Press(100, 100, PointerPress.MiddleButton);
        await canvas.Move(150, 100);
        canvas.WheelAt((150, 100), 30, shift: true);
        await canvas.Move(160, 100);

        Assert.Equal(30, tracker.PanX, precision: 8);
    }

    [Fact]
    public async Task AWheelZoomDuringAHeldPanKeepsTheGrabbedPointUnderThePointer()
    {
        var canvas = RenderCanvas(new Board());

        await canvas.Press(100, 100, PointerPress.MiddleButton);
        await canvas.Move(150, 120);
        canvas.WheelAt((150, 120), -CanvasViewport.Notch);
        await canvas.Move(170, 120);

        var underPointer = BoardPointUnder(canvas, (170, 120));
        Assert.NotEqual(1, canvas.Instance.ZoomPanTracker.Scale);
        Assert.Equal(100, underPointer.X, precision: 8);
        Assert.Equal(100, underPointer.Y, precision: 8);
    }

    [Fact]
    public async Task AMarqueeBandKeepsItsStartCornerOnTheBoardAsTheViewportPans()
    {
        var board = new Board();
        var instance = AddInstance(board, 300, 100);
        var canvas = RenderCanvas(board, WheelDeviceProfile.Trackpad);

        await canvas.Press(100, 100);
        await canvas.Move(150, 150);
        canvas.WheelAt((150, 150), deltaY: 0, deltaX: 200, coarse: false);
        await canvas.Release(150, 150);

        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
        Assert.Equal(new Bounds(300, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public void AnEdgePressStillSelectsTheEdgeAfterAViewportChange()
    {
        var board = new Board();
        board.AddEdge(new Edge(new FloatingEndpoint(100, 100), new FloatingEndpoint(300, 100)));
        var canvas = RenderCanvas(board, WheelDeviceProfile.Trackpad);

        canvas.PressElement(canvas.Find(".edge-hit"), (200, 100));
        canvas.WheelAt((200, 100), deltaY: 20, coarse: false);
        canvas.ReleaseAt((200, 100));

        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));
        PointerListener.VerifyNotInvoke("promote");
    }
}
