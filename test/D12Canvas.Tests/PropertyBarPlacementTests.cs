using Xunit;

namespace D12Canvas.Tests;

public class PropertyBarPlacementTests
{
    private const double ContainerWidth = 800;
    private const double ContainerHeight = 600;

    private static (double Left, double Top) Place(PropertyBarAnchor anchor, double barWidth) =>
        PropertyBarPlacement.Place(anchor, barWidth, ContainerWidth, ContainerHeight);

    [Fact]
    public void ABarWithRoomIsCentredAboveTheSelectionsTop()
    {
        var (left, top) = Place(new PropertyBarAnchor(300, 200, 100), barWidth: 120);

        Assert.Equal(290, left);
        Assert.Equal(200 - PropertyBarPlacement.Gap - PropertyBarPlacement.Height, top);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(0)]
    [InlineData(-400)]
    public void WithNoRoomAboveTheBarSlidesAlongTheTopEdgeRatherThanFlippingBelow(
        double selectionTop
    )
    {
        var (left, top) = Place(new PropertyBarAnchor(300, selectionTop, 100), barWidth: 120);

        Assert.Equal(PropertyBarPlacement.Margin, top);
        Assert.Equal(290, left);
    }

    [Theory]
    [InlineData(-200, PropertyBarPlacement.Margin)]
    [InlineData(10, PropertyBarPlacement.Margin)]
    [InlineData(760, ContainerWidth - PropertyBarPlacement.Margin - 120)]
    [InlineData(2000, ContainerWidth - PropertyBarPlacement.Margin - 120)]
    public void ABarCrossingASideIsClampedInsideTheContainer(
        double selectionLeft,
        double expectedLeft
    )
    {
        var (left, _) = Place(new PropertyBarAnchor(selectionLeft, 300, 40), barWidth: 120);

        Assert.Equal(expectedLeft, left);
    }

    [Fact]
    public void ASelectionBelowTheContainerPinsTheBarToTheBottomMargin()
    {
        var (_, top) = Place(new PropertyBarAnchor(300, 2000, 100), barWidth: 120);

        Assert.Equal(
            ContainerHeight - PropertyBarPlacement.Margin - PropertyBarPlacement.Height,
            top
        );
    }

    [Fact]
    public void ABarWiderThanTheContainerStartsAtTheLeftMargin()
    {
        var (left, _) = Place(new PropertyBarAnchor(300, 300, 100), barWidth: 900);

        Assert.Equal(PropertyBarPlacement.Margin, left);
    }

    private static (double Left, double Top) Settle(
        (double Left, double Top) shown,
        (double Left, double Top) target
    ) => PropertyBarPlacement.Settle(shown, target, 120, ContainerWidth, ContainerHeight);

    [Fact]
    public void ABarStaysPutWhenItsNewPlaceIsCloserThanTheMinimumMove()
    {
        var shown = (300.0, 200.0);

        Assert.Equal(shown, Settle(shown, (310, 205)));
    }

    [Fact]
    public void ABarMovesOnceItsNewPlaceIsTheMinimumMoveAwayOrMore()
    {
        var target = (300.0 + PropertyBarPlacement.MinimumMove, 200.0);

        Assert.Equal(target, Settle((300, 200), target));
    }

    [Fact]
    public void ABarThatWouldStayOutsideTheMarginsMovesHoweverSmallTheMove()
    {
        var target = (PropertyBarPlacement.Margin, 200.0);

        Assert.Equal(target, Settle((PropertyBarPlacement.Margin - 4, 200), target));
    }

    [Fact]
    public void TheMinimumMoveIsAboveTheGapButSmallerThanACell()
    {
        Assert.True(PropertyBarPlacement.Gap < PropertyBarPlacement.MinimumMove);
        Assert.True(PropertyBarPlacement.MinimumMove < PropertyBarPlacement.Height);
    }
}
