using Xunit;

namespace D12Canvas.Tests;

public class ContextMenuPlacementTests
{
    private const double Height = 200;

    [Fact]
    public void OpensRightAndDownWhenItFits()
    {
        Assert.Equal((100, 50), ContextMenuPlacement.Place(100, 50, Height, 800, 600));
    }

    [Fact]
    public void FlipsLeftWhenItWouldCrossTheRightEdge()
    {
        var (left, top) = ContextMenuPlacement.Place(700, 50, Height, 800, 600);

        Assert.Equal(700 - ContextMenuPlacement.Width, left);
        Assert.Equal(50, top);
    }

    [Fact]
    public void FlipsUpWhenItWouldCrossTheBottomEdge()
    {
        Assert.Equal((100, 350), ContextMenuPlacement.Place(100, 550, Height, 800, 600));
    }

    [Fact]
    public void TouchingAnEdgeExactlyStillFits()
    {
        Assert.Equal(
            (800 - ContextMenuPlacement.Width, 400),
            ContextMenuPlacement.Place(800 - ContextMenuPlacement.Width, 400, Height, 800, 600)
        );
    }

    [Fact]
    public void ClampsOnlyWhenItFitsOnNeitherSide()
    {
        Assert.Equal((100, 50), ContextMenuPlacement.Place(100, 150, Height, 800, 250));
    }

    [Fact]
    public void AContainerSmallerThanTheMenuPinsItToTheTopLeft()
    {
        Assert.Equal((0, 0), ContextMenuPlacement.Place(100, 100, Height, 150, 150));
    }

    [Fact]
    public void AnUnmeasuredContainerPlacesItAtTheAnchor()
    {
        Assert.Equal((700, 550), ContextMenuPlacement.Place(700, 550, Height, 0, 0));
    }

    [Fact]
    public void HeightCountsRowsSeparatorsAndTheFrame()
    {
        var row = new ContextMenuRow(ContextMenuCommand.Delete, "Delete", null, null);

        var height = ContextMenuPlacement.HeightOf(
            [
                [row],
                [row, row],
            ]
        );

        Assert.Equal(
            ContextMenuPlacement.Frame
                + 3 * ContextMenuPlacement.RowHeight
                + ContextMenuPlacement.SeparatorHeight,
            height
        );
    }
}
