using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class BoundsTests
{
    [Fact]
    public void ExpandedByGrowsEachSideByTheMargin()
    {
        var bounds = new Bounds(10, 10, 20, 20);

        var expanded = bounds.ExpandedBy(5);

        Assert.Equal(new Bounds(5, 5, 30, 30), expanded);
    }

    [Fact]
    public void IntersectsIsTrueWhenBoundsOverlap()
    {
        var first = new Bounds(0, 0, 10, 10);
        var second = new Bounds(5, 5, 10, 10);

        Assert.True(first.Intersects(second));
    }

    [Fact]
    public void IntersectsIsTrueWhenBoundsOnlyTouchAtAnEdge()
    {
        var first = new Bounds(0, 0, 10, 10);
        var second = new Bounds(10, 0, 10, 10);

        Assert.True(first.Intersects(second));
    }

    [Fact]
    public void IntersectsIsFalseWhenBoundsAreFullySeparate()
    {
        var first = new Bounds(0, 0, 10, 10);
        var second = new Bounds(100, 100, 10, 10);

        Assert.False(first.Intersects(second));
    }

    [Fact]
    public void IntersectsIsSymmetric()
    {
        var first = new Bounds(0, 0, 10, 10);
        var second = new Bounds(5, 5, 10, 10);

        Assert.Equal(first.Intersects(second), second.Intersects(first));
    }

    [Theory]
    [InlineData(0.5, 0, 60, 10)] // top center
    [InlineData(1, 0.5, 110, 35)] // right center
    [InlineData(0.5, 1, 60, 60)] // bottom center
    [InlineData(0, 0.5, 10, 35)] // left center
    [InlineData(0, 0, 10, 10)] // origin
    public void PointAtFractionResolvesAFractionOfTheBox(
        double fractionX,
        double fractionY,
        double expectedX,
        double expectedY
    )
    {
        var bounds = new Bounds(10, 10, 100, 50);

        var point = bounds.PointAtFraction(fractionX, fractionY);

        Assert.Equal((expectedX, expectedY), point);
    }

    [Fact]
    public void PointAtFractionTracksAMovedAndResizedBox()
    {
        var bounds = new Bounds(200, 300, 40, 80);

        var point = bounds.PointAtFraction(0.5, 0);

        Assert.Equal((220.0, 300.0), point);
    }
}
