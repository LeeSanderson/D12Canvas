using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The wheel's meaning follows the wheel device profile: a mouse wheel zooms about the pointer and
// a trackpad swipe pans, Ctrl zooms on both, Alt pans on both and Shift pans horizontally on a
// mouse only. Auto takes the device from the granularity the listener classified the wheel
// gesture with. No wheel change enters history.
public class DiagramCanvasWheelTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";
    private static readonly (double X, double Y) Pointer = (240, 180);

    public DiagramCanvasWheelTests()
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

    private IRenderedComponent<DiagramCanvas> RenderCanvas(
        WheelDeviceProfile profile,
        Board? board = null
    ) =>
        Render<DiagramCanvas>(parameters =>
            parameters
                .Add(p => p.Board, board ?? new Board())
                .Add(p => p.WheelDeviceProfile, profile)
        );

    private static (double X, double Y) BoardPointUnder(
        IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) at
    )
    {
        var tracker = canvas.Instance.ZoomPanTracker;
        return ((at.X - tracker.PanX) / tracker.Scale, (at.Y - tracker.PanY) / tracker.Scale);
    }

    private static void AssertSamePoint((double X, double Y) expected, (double X, double Y) actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 8);
        Assert.Equal(expected.Y, actual.Y, precision: 8);
    }

    private static string? ContentStyle(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find(".canvas-content").GetAttribute("style");

    [Fact]
    public void TheProfileDefaultsToAuto()
    {
        var canvas = Render<DiagramCanvas>();

        Assert.Equal(WheelDeviceProfile.Auto, canvas.Instance.WheelDeviceProfile);
    }

    [Theory]
    [InlineData(-CanvasViewport.Notch, true)]
    [InlineData(CanvasViewport.Notch, false)]
    public void OnAMouseAWheelNotchZoomsAboutThePointer(double deltaY, bool zoomsIn)
    {
        var canvas = RenderCanvas(WheelDeviceProfile.Mouse);
        canvas.InvokeAsync(() => canvas.Instance.ZoomPanTracker.SetPanPosition(-35, 20));
        var underPointer = BoardPointUnder(canvas, Pointer);

        canvas.WheelAt(Pointer, deltaY);

        Assert.Equal(zoomsIn, canvas.Instance.ZoomPanTracker.Scale > 1);
        AssertSamePoint(underPointer, BoardPointUnder(canvas, Pointer));
    }

    [Fact]
    public void WheelZoomIsMultiplicativeSoEqualNotchesScaleByEqualFactors()
    {
        var canvas = RenderCanvas(WheelDeviceProfile.Mouse);
        var tracker = canvas.Instance.ZoomPanTracker;

        var before = tracker.Scale;
        canvas.WheelAt(Pointer, -CanvasViewport.Notch);
        var afterOne = tracker.Scale;
        canvas.WheelAt(Pointer, -CanvasViewport.Notch);
        var afterTwo = tracker.Scale;

        Assert.Equal(afterOne / before, afterTwo / afterOne, precision: 10);
    }

    [Fact]
    public void AFinerDeltaZoomsByAFinerFactor()
    {
        var coarse = RenderCanvas(WheelDeviceProfile.Mouse);
        var fine = RenderCanvas(WheelDeviceProfile.Mouse);

        coarse.WheelAt(Pointer, -CanvasViewport.Notch);
        fine.WheelAt(Pointer, -CanvasViewport.Notch / 10);

        Assert.InRange(fine.Instance.ZoomPanTracker.Scale, 1, coarse.Instance.ZoomPanTracker.Scale);
        Assert.NotEqual(1, fine.Instance.ZoomPanTracker.Scale);
    }

    [Fact]
    public void OnAMouseShiftWheelPansHorizontallyOnly()
    {
        var canvas = RenderCanvas(WheelDeviceProfile.Mouse);
        var tracker = canvas.Instance.ZoomPanTracker;

        canvas.WheelAt(Pointer, CanvasViewport.Notch, shift: true);

        Assert.True(tracker.PanX < 0);
        Assert.Equal(0, tracker.PanY);
        Assert.Equal(1, tracker.Scale);
    }

    [Fact]
    public void OnAMouseAltWheelPansVertically()
    {
        var canvas = RenderCanvas(WheelDeviceProfile.Mouse);
        var tracker = canvas.Instance.ZoomPanTracker;

        canvas.WheelAt(Pointer, CanvasViewport.Notch, alt: true);

        Assert.Equal(0, tracker.PanX);
        Assert.True(tracker.PanY < 0);
        Assert.Equal(1, tracker.Scale);
    }

    [Fact]
    public void OnATrackpadAPlainSwipePansBothAxesByTheSwipe()
    {
        var canvas = RenderCanvas(WheelDeviceProfile.Trackpad);
        var tracker = canvas.Instance.ZoomPanTracker;

        canvas.WheelAt(Pointer, deltaY: -7.25, deltaX: 12.5, coarse: false);

        Assert.Equal(-12.5, tracker.PanX);
        Assert.Equal(7.25, tracker.PanY);
        Assert.Equal(1, tracker.Scale);
    }

    [Theory]
    [InlineData(WheelDeviceProfile.Mouse, true, false)]
    [InlineData(WheelDeviceProfile.Mouse, false, true)]
    [InlineData(WheelDeviceProfile.Trackpad, true, false)]
    [InlineData(WheelDeviceProfile.Trackpad, false, true)]
    public void CtrlOrMetaWheelZoomsAboutThePointerOnEitherDevice(
        WheelDeviceProfile profile,
        bool ctrl,
        bool meta
    )
    {
        var canvas = RenderCanvas(profile);
        var underPointer = BoardPointUnder(canvas, Pointer);

        canvas.WheelAt(Pointer, -3.5, coarse: false, ctrl: ctrl, meta: meta);

        Assert.True(canvas.Instance.ZoomPanTracker.Scale > 1);
        AssertSamePoint(underPointer, BoardPointUnder(canvas, Pointer));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void OnATrackpadShiftAndAltLeaveTheSwipeAsAPlainPan(bool shift, bool alt)
    {
        var plain = RenderCanvas(WheelDeviceProfile.Trackpad);
        var modified = RenderCanvas(WheelDeviceProfile.Trackpad);

        plain.WheelAt(Pointer, deltaY: 9, deltaX: 4, coarse: false);
        modified.WheelAt(Pointer, deltaY: 9, deltaX: 4, coarse: false, shift: shift, alt: alt);

        Assert.Equal(plain.Instance.ZoomPanTracker.PanX, modified.Instance.ZoomPanTracker.PanX);
        Assert.Equal(plain.Instance.ZoomPanTracker.PanY, modified.Instance.ZoomPanTracker.PanY);
        Assert.Equal(1, modified.Instance.ZoomPanTracker.Scale);
    }

    [Fact]
    public void AutoZoomsOnACoarseWheelGestureAndPansOnAFineOne()
    {
        var coarse = RenderCanvas(WheelDeviceProfile.Auto);
        var fine = RenderCanvas(WheelDeviceProfile.Auto);

        coarse.WheelAt(Pointer, CanvasViewport.Notch, coarse: true);
        fine.WheelAt(Pointer, CanvasViewport.Notch, coarse: false);

        Assert.NotEqual(1, coarse.Instance.ZoomPanTracker.Scale);
        Assert.Equal(1, fine.Instance.ZoomPanTracker.Scale);
        Assert.True(fine.Instance.ZoomPanTracker.PanY < 0);
    }

    [Theory]
    [InlineData(WheelDeviceProfile.Mouse, false)]
    [InlineData(WheelDeviceProfile.Trackpad, true)]
    public void APinnedProfileIgnoresTheClassifiedGranularity(
        WheelDeviceProfile profile,
        bool coarse
    )
    {
        var canvas = RenderCanvas(profile);

        canvas.WheelAt(Pointer, CanvasViewport.Notch, coarse: coarse);

        Assert.Equal(
            profile == WheelDeviceProfile.Mouse,
            canvas.Instance.ZoomPanTracker.Scale != 1
        );
    }

    [Fact]
    public void TheTrackpadsAmbientTransitionIsShorterThanTheMouses()
    {
        Assert.True(
            WheelMapping.AmbientTransitionFor(WheelDeviceProfile.Trackpad)
                < WheelMapping.AmbientTransitionFor(WheelDeviceProfile.Mouse)
        );
    }

    [Fact]
    public async Task OnlyAMouseWheelEasesTheTransformAndAPointerPanNeverDoes()
    {
        var canvas = RenderCanvas(WheelDeviceProfile.Auto);

        Assert.DoesNotContain("transition", ContentStyle(canvas));

        canvas.WheelAt(Pointer, CanvasViewport.Notch, coarse: true);
        Assert.Contains("transition", ContentStyle(canvas));

        canvas.WheelAt(Pointer, CanvasViewport.Notch, coarse: false);
        Assert.DoesNotContain("transition", ContentStyle(canvas));

        canvas.WheelAt(Pointer, CanvasViewport.Notch, coarse: true);
        await canvas.Pan((100, 100), (140, 120));
        Assert.DoesNotContain("transition", ContentStyle(canvas));
    }

    [Fact]
    public async Task UndoAfterAWheelZoomUndoesTheLastBoardEditAndLeavesTheZoom()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(100, 100, 50, 50)
        );
        board.AddComponent(instance);
        var canvas = RenderCanvas(WheelDeviceProfile.Mouse, board);
        canvas.DragOn(canvas.Find(".component-container"), (120, 120), (160, 150));
        canvas.WheelAt(Pointer, -CanvasViewport.Notch);
        var zoomed = canvas.Instance.ZoomPanTracker.Scale;

        await canvas.InvokeAsync(canvas.Instance.OnUndoPressed);

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
        Assert.Equal(zoomed, canvas.Instance.ZoomPanTracker.Scale);
    }

    [Fact]
    public async Task AWheelZoomWithNoBoardEditsLeavesNothingToUndo()
    {
        var board = new Board();
        var canvas = RenderCanvas(WheelDeviceProfile.Mouse, board);

        canvas.WheelAt(Pointer, -CanvasViewport.Notch);
        var zoomed = canvas.Instance.ZoomPanTracker.Scale;
        await canvas.InvokeAsync(canvas.Instance.OnUndoPressed);

        Assert.Equal(zoomed, canvas.Instance.ZoomPanTracker.Scale);
    }
}
