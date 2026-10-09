using Xunit;

namespace D12Canvas.Tests;

public class ContextMenuPlacementTests
{
    private const double Height = 200;

    private static (double Left, double Top) Place(
        double anchorX,
        double anchorY,
        double height,
        double containerWidth,
        double containerHeight
    )
    {
        var box = ContextMenuPlacement.Fit(
            anchorX,
            anchorY,
            height,
            containerWidth,
            containerHeight
        );
        return (box.Left, box.Top);
    }

    [Fact]
    public void OpensRightAndDownWhenItFits()
    {
        Assert.Equal((100, 50), Place(100, 50, Height, 800, 600));
    }

    [Fact]
    public void FlipsLeftWhenItWouldCrossTheRightEdge()
    {
        var (left, top) = Place(700, 50, Height, 800, 600);

        Assert.Equal(700 - ContextMenuPlacement.Width, left);
        Assert.Equal(50, top);
    }

    [Fact]
    public void FlipsUpWhenItWouldCrossTheBottomEdge()
    {
        Assert.Equal((100, 350), Place(100, 550, Height, 800, 600));
    }

    [Fact]
    public void TouchingAnEdgeExactlyStillFits()
    {
        Assert.Equal(
            (800 - ContextMenuPlacement.Width, 400),
            Place(800 - ContextMenuPlacement.Width, 400, Height, 800, 600)
        );
    }

    [Fact]
    public void ClampsOnlyWhenItFitsOnNeitherSide()
    {
        Assert.Equal((100, 50), Place(100, 150, Height, 800, 250));
    }

    [Fact]
    public void AContainerSmallerThanTheMenuPinsItToTheTopLeft()
    {
        Assert.Equal((0, 0), Place(100, 100, Height, 150, 150));
    }

    [Fact]
    public void AnUnmeasuredContainerPlacesItAtTheAnchor()
    {
        Assert.Equal((700, 550), Place(700, 550, Height, 0, 0));
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

    [Fact]
    public void AStripOfGlyphsIsOneRowTall()
    {
        var row = new ContextMenuRow(ContextMenuCommand.BringToFront, "Bring to Front", null, null);
        var glyph = new ContextMenuRow(
            ContextMenuCommand.AlignLeft,
            "Align left",
            null,
            null,
            true
        );

        var height = ContextMenuPlacement.HeightOf(
            [
                [glyph, glyph, glyph, row],
            ]
        );

        Assert.Equal(ContextMenuPlacement.Frame + 2 * ContextMenuPlacement.RowHeight, height);
    }

    [Fact]
    public void FitKeepsTheMenusOwnSizeWhereTheContainerHoldsIt()
    {
        Assert.Equal(
            new ContextMenuPlacement.MenuBox(700 - ContextMenuPlacement.Width, 50, null, null),
            ContextMenuPlacement.Fit(700, 50, Height, 800, 600)
        );
    }

    [Fact]
    public void AContainerNarrowerThanTheMenuShrinksItToTheContainersWidth()
    {
        var box = ContextMenuPlacement.Fit(60, 50, Height, 150, 600);

        Assert.Equal(0, box.Left);
        Assert.Equal(150, box.Width);
        Assert.Null(box.MaxHeight);
    }

    [Fact]
    public void AContainerShorterThanTheMenuCapsItsHeightAtTheContainers()
    {
        var box = ContextMenuPlacement.Fit(100, 60, Height, 800, 150);

        Assert.Equal(0, box.Top);
        Assert.Equal(150, box.MaxHeight);
        Assert.Null(box.Width);
    }

    [Fact]
    public void AnUnmeasuredContainerFitsTheMenuAtTheAnchorAtItsOwnSize()
    {
        Assert.Equal(
            new ContextMenuPlacement.MenuBox(700, 550, null, null),
            ContextMenuPlacement.Fit(700, 550, Height, 0, 0)
        );
    }
}
