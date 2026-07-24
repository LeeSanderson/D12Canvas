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
}
