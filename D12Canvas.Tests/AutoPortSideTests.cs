using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class AutoPortSideTests
{
    private static readonly Bounds Wide = new(100, 100, 200, 100);

    [Theory]
    [InlineData(200, 0, PortId.Top)]
    [InlineData(500, 150, PortId.Right)]
    [InlineData(200, 400, PortId.Bottom)]
    [InlineData(-100, 150, PortId.Left)]
    public void ThePointAimedAtNamesTheSideTheAimingLineCrosses(double x, double y, PortId expected)
    {
        Assert.Equal(expected, AutoPortSide.Facing(Wide, (x, y)));
    }

    [Theory]
    [InlineData(400, 60, PortId.Right)]
    [InlineData(380, 0, PortId.Top)]
    public void AWideShapeCrossesItsShortSideOnlyBeyondItsOwnDiagonal(
        double x,
        double y,
        PortId expected
    )
    {
        Assert.Equal(expected, AutoPortSide.Facing(Wide, (x, y)));
    }

    [Theory]
    [InlineData(400, 50, PortId.Top)]
    [InlineData(400, 250, PortId.Right)]
    [InlineData(0, 250, PortId.Bottom)]
    [InlineData(0, 50, PortId.Top)]
    public void AnAimingLineThroughACornerTakesTheSideListedFirst(
        double x,
        double y,
        PortId expected
    )
    {
        Assert.Equal(expected, AutoPortSide.Facing(Wide, (x, y)));
    }

    [Theory]
    [InlineData(200, 110, PortId.Top)]
    [InlineData(290, 150, PortId.Right)]
    [InlineData(200, 195, PortId.Bottom)]
    [InlineData(105, 150, PortId.Left)]
    public void APointInsideTheShapeTakesTheNearestSide(double x, double y, PortId expected)
    {
        Assert.Equal(expected, AutoPortSide.Facing(Wide, (x, y)));
    }

    [Fact]
    public void TheCentreItselfResolvesWithoutASpecialCase()
    {
        var square = new Bounds(0, 0, 100, 100);

        Assert.Equal(PortId.Top, AutoPortSide.Facing(square, (50, 50)));
    }

    [Fact]
    public void AZeroAreaShapeStillResolves()
    {
        var point = new Bounds(10, 10, 0, 0);

        Assert.Equal(PortId.Right, AutoPortSide.Facing(point, (50, 10)));
        Assert.Equal(PortId.Top, AutoPortSide.Facing(point, (10, 10)));
    }
}
