using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class PortPlacementTests
{
    private const double Step = 25;
    private static readonly Bounds Shape = new(100, 100, 200, 100);

    private static double StepBy(double coordinate, int direction) => coordinate + direction * Step;

    private static (int X, int Y) Arrow(string arrow) =>
        arrow switch
        {
            "Left" => (-1, 0),
            "Right" => (1, 0),
            "Up" => (0, -1),
            "Down" => (0, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(arrow), arrow, null),
        };

    private static ProvisionalPort Press(ProvisionalPort port, string arrow)
    {
        var (x, y) = Arrow(arrow);
        return PortPlacement.Move(port, Shape, x, y, StepBy);
    }

    public static TheoryData<PortId, double, string, PortId, double> Walk =>
        new()
        {
            { PortId.Top, 0.5, "Right", PortId.Top, 0.625 },
            { PortId.Top, 0.5, "Left", PortId.Top, 0.375 },
            { PortId.Top, 0.5, "Down", PortId.Bottom, 0.5 },
            { PortId.Top, 0.5, "Up", PortId.Top, 0.5 },
            { PortId.Bottom, 0.5, "Up", PortId.Top, 0.5 },
            { PortId.Bottom, 0.5, "Down", PortId.Bottom, 0.5 },
            { PortId.Left, 0.5, "Down", PortId.Left, 0.75 },
            { PortId.Left, 0.5, "Up", PortId.Left, 0.25 },
            { PortId.Left, 0.5, "Right", PortId.Right, 0.5 },
            { PortId.Left, 0.5, "Left", PortId.Left, 0.5 },
            { PortId.Right, 0.5, "Left", PortId.Left, 0.5 },
            { PortId.Right, 0.5, "Right", PortId.Right, 0.5 },
            { PortId.Top, 0.95, "Right", PortId.Top, 1 },
            { PortId.Top, 0.05, "Left", PortId.Top, 0 },
            { PortId.Left, 0.9, "Down", PortId.Left, 1 },
            { PortId.Top, 1, "Right", PortId.Right, 0 },
            { PortId.Top, 0, "Left", PortId.Left, 0 },
            { PortId.Bottom, 1, "Right", PortId.Right, 1 },
            { PortId.Bottom, 0, "Left", PortId.Left, 1 },
            { PortId.Left, 0, "Up", PortId.Top, 0 },
            { PortId.Left, 1, "Down", PortId.Bottom, 0 },
            { PortId.Right, 0, "Up", PortId.Top, 1 },
            { PortId.Right, 1, "Down", PortId.Bottom, 1 },
            { PortId.Top, 1, "Down", PortId.Right, 0.25 },
            { PortId.Top, 0, "Down", PortId.Left, 0.25 },
            { PortId.Bottom, 1, "Up", PortId.Right, 0.75 },
            { PortId.Left, 0, "Right", PortId.Top, 0.125 },
            { PortId.Right, 1, "Left", PortId.Bottom, 0.875 },
            { PortId.Left, 0, "Left", PortId.Left, 0 },
            { PortId.Top, 0, "Up", PortId.Top, 0 },
            { PortId.Right, 0, "Right", PortId.Right, 0 },
        };

    [Theory]
    [MemberData(nameof(Walk))]
    public void AnArrowSlidesAlongTheSideTurnsAtACornerOrCrossesWhenItPointsInward(
        PortId side,
        double along,
        string arrow,
        PortId expectedSide,
        double expectedAlong
    )
    {
        var moved = Press(new ProvisionalPort(side, along), arrow);

        Assert.Equal(new ProvisionalPort(expectedSide, expectedAlong), moved);
    }

    [Theory]
    [InlineData("Left")]
    [InlineData("Right")]
    [InlineData("Up")]
    [InlineData("Down")]
    public void HoldingAnArrowEndsAtACornerAndStaysThere(string arrow)
    {
        var port = new ProvisionalPort(PortId.Top, 0.5);
        for (var press = 0; press < 40; press++)
        {
            port = Press(port, arrow);
        }

        var settled = Press(port, arrow);

        Assert.Equal(port, settled);
        Assert.Contains(port.Fractions.X, new[] { 0.0, 0.5, 1.0 });
    }

    [Fact]
    public void HoldingLeftOnTheTopSideEndsAtTheTopOfTheLeftSide()
    {
        var port = new ProvisionalPort(PortId.Top, 0.5);
        for (var press = 0; press < 10; press++)
        {
            port = Press(port, "Left");
        }

        Assert.Equal(new ProvisionalPort(PortId.Left, 0), port);
        Assert.Equal((0.0, 0.0), port.Fractions);
    }

    [Fact]
    public void ARunThatOvershootsTheCornerStopsOnIt()
    {
        var moved = PortPlacement.Move(
            new ProvisionalPort(PortId.Top, 0.5),
            Shape,
            1,
            0,
            (coordinate, direction) => coordinate + direction * 1000
        );

        Assert.Equal(new ProvisionalPort(PortId.Top, 1), moved);
    }

    [Fact]
    public void TheStartIsAQuarterAlongTheTopSide()
    {
        Assert.Equal(
            new ProvisionalPort(PortId.Top, 0.25),
            PortPlacement.Start(Shape, gridSpacing: null)
        );
    }

    [Fact]
    public void UnderSnapTheStartIsRoundedToTheNearestGridLine()
    {
        var start = PortPlacement.Start(new Bounds(100, 100, 220, 100), gridSpacing: 20);

        Assert.Equal(new ProvisionalPort(PortId.Top, 60.0 / 220), start);
    }

    public static TheoryData<PortId, double, double, double> Fractions =>
        new()
        {
            { PortId.Top, 0.3, 0.3, 0 },
            { PortId.Bottom, 0.3, 0.3, 1 },
            { PortId.Left, 0.3, 0, 0.3 },
            { PortId.Right, 0.3, 1, 0.3 },
        };

    [Theory]
    [MemberData(nameof(Fractions))]
    public void APortOnASideSitsOnThatSideOfTheBounds(
        PortId side,
        double along,
        double expectedX,
        double expectedY
    )
    {
        var port = new ProvisionalPort(side, along);

        Assert.Equal((expectedX, expectedY), port.Fractions);
        Assert.Equal(side, BorderPartition.SideOf(new PortDef(expectedX, expectedY)));
    }
}
