using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// The minimap's points reach the gesture as minimap pixels, mapped here at a tenth of board scale
// with the board origin 50 pixels in from the minimap's corner, so minimap (60, 70) is board
// (100, 200). The canvas container is 800 by 600, so a centred board point (x, y) at scale 1 is pan
// (400 - x, 300 - y), and the viewport at pan zero is board (0, 0) to (800, 600), which minimap
// (50, 50) to (130, 110) shows. A press at minimap (10, 10) lands outside that rect.
public class MinimapPanGestureTests
{
    private static (double X, double Y) ToBoardPoint(double x, double y) =>
        ((x - 50) * 10, (y - 50) * 10);

    private static MinimapPanGesture PressAt(FakeGestureContext context, double x, double y)
    {
        var gesture = new MinimapPanGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, x, y),
            context,
            ToBoardPoint
        );
        gesture.Begin();
        return gesture;
    }

    [Fact]
    public void AClickFliesTheViewportCentreToThePressedPointAndKeepsTheScale()
    {
        var context = new FakeGestureContext(new Board());
        context.ZoomPan.Scale = 0.5;
        var gesture = PressAt(context, 60, 70);

        gesture.Release(PointerEvents.Release(PointerPress.PrimaryButton, 61, 70));

        Assert.Equal([true], context.ViewportCentrings);
        Assert.Equal(0.5, context.ZoomPan.Scale);
        Assert.Equal((350, 200), (context.ZoomPan.PanX, context.ZoomPan.PanY));
    }

    [Fact]
    public void ADragFromInsideTheViewportRectKeepsTheRectWhereItWasGrabbed()
    {
        var context = new FakeGestureContext(new Board());
        var gesture = PressAt(context, 60, 70);

        gesture.Move(PointerEvents.Move(65, 70));
        gesture.Move(PointerEvents.Move(80, 90));

        Assert.Equal([false, false], context.ViewportCentrings);
        Assert.Equal((-200, -200), (context.ZoomPan.PanX, context.ZoomPan.PanY));
    }

    [Fact]
    public void ADragFromOutsideTheViewportRectCentresTheViewportOnThePointerWithoutAnimating()
    {
        var context = new FakeGestureContext(new Board());
        var gesture = PressAt(context, 10, 10);

        gesture.Move(PointerEvents.Move(65, 70));
        gesture.Move(PointerEvents.Move(80, 90));

        Assert.Equal([false, false], context.ViewportCentrings);
        Assert.Equal((100, -100), (context.ZoomPan.PanX, context.ZoomPan.PanY));
    }

    [Fact]
    public void ReleasingFromADragMovesNothingFurther()
    {
        var context = new FakeGestureContext(new Board());
        var gesture = PressAt(context, 10, 10);
        gesture.Move(PointerEvents.Move(80, 90));

        gesture.Release(PointerEvents.Release(PointerPress.PrimaryButton, 90, 95));

        Assert.Equal([false], context.ViewportCentrings);
        Assert.Equal((100, -100), (context.ZoomPan.PanX, context.ZoomPan.PanY));
    }

    [Fact]
    public void AViewportChangeUnderTheDragReanchorsSoTheTwoMovementsAdd()
    {
        var context = new FakeGestureContext(new Board());
        var gesture = PressAt(context, 10, 10);
        gesture.Move(PointerEvents.Move(80, 90));

        context.ZoomPan.Pan(-30, 0);
        gesture.ViewportMoved(PointerEvents.Move(80, 90));
        gesture.Move(PointerEvents.Move(81, 90));

        Assert.Equal((60, -100), (context.ZoomPan.PanX, context.ZoomPan.PanY));
    }

    [Fact]
    public void AViewportChangeUnderAStillPressPromotesItSoItsReleaseFliesNowhere()
    {
        var context = new FakeGestureContext(new Board());
        var gesture = PressAt(context, 60, 70);

        context.ZoomPan.Pan(-30, 0);
        var promoted = gesture.ViewportMoved(PointerEvents.Move(60, 70));
        gesture.Release(PointerEvents.Release(PointerPress.PrimaryButton, 60, 70));

        Assert.True(promoted);
        Assert.Empty(context.ViewportCentrings);
        Assert.Equal(-30, context.ZoomPan.PanX);
    }

    [Fact]
    public void ACancelledDragIgnoresFurtherMovesAndItsReleaseAndLeavesTheViewportWhereItIs()
    {
        var context = new FakeGestureContext(new Board());
        var gesture = PressAt(context, 10, 10);
        gesture.Move(PointerEvents.Move(80, 90));

        gesture.MarkCancelled();
        gesture.Move(PointerEvents.Move(100, 100));
        gesture.Release(PointerEvents.Release(PointerPress.PrimaryButton, 100, 100));

        Assert.Equal([false], context.ViewportCentrings);
        Assert.Equal((100, -100), (context.ZoomPan.PanX, context.ZoomPan.PanY));
    }
}
