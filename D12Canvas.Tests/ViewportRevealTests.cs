using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class ViewportRevealTests
{
    private static readonly Bounds Viewport = new(0, 0, 800, 600);

    [Fact]
    public void ABoxAlreadyInsideNeedsNoShift() =>
        Assert.Equal((0, 0), ViewportReveal.ShiftToReveal(Viewport, new Bounds(100, 100, 200, 40)));

    [Fact]
    public void ABoxTouchingTheEdgesFromInsideNeedsNoShift() =>
        Assert.Equal((0, 0), ViewportReveal.ShiftToReveal(Viewport, new Bounds(0, 0, 800, 600)));

    [Fact]
    public void ABoxPastTheRightAndBottomEdgesShiftsByTheOverhangOnly() =>
        Assert.Equal(
            (50, 30),
            ViewportReveal.ShiftToReveal(Viewport, new Bounds(700, 570, 150, 60))
        );

    [Fact]
    public void ABoxPastTheLeftAndTopEdgesShiftsBackByTheOverhangOnly() =>
        Assert.Equal(
            (-40, -25),
            ViewportReveal.ShiftToReveal(Viewport, new Bounds(-40, -25, 100, 100))
        );

    [Fact]
    public void AnOffsetViewportMeasuresFromItsOwnEdges() =>
        Assert.Equal(
            (20, 0),
            ViewportReveal.ShiftToReveal(
                new Bounds(-300, 200, 400, 300),
                new Bounds(0, 250, 120, 50)
            )
        );

    [Fact]
    public void ABoxWiderThanTheViewportAlignsItsLeadingEdge() =>
        Assert.Equal(
            (-100, 0),
            ViewportReveal.ShiftToReveal(Viewport, new Bounds(-100, 10, 1000, 40))
        );
}
