using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class EdgeRouterTests
{
    private static readonly Bounds Upper = new(0, 0, 100, 100);
    private static readonly Bounds LowerAndRight = new(50, 200, 100, 100);

    public static TheoryData<PortId, PortId> SidePairs()
    {
        var pairs = new TheoryData<PortId, PortId>();
        foreach (var source in StandardPorts.All)
        {
            foreach (var target in StandardPorts.All)
            {
                pairs.Add(source, target);
            }
        }

        return pairs;
    }

    private static RouteEnd PortEnd(Bounds shape, PortId side)
    {
        var (fractionX, fractionY) = StandardPorts.FractionOf(side);
        var (x, y) = shape.PointAtFraction(fractionX, fractionY);
        return new RouteEnd(x, y, side, shape);
    }

    private static EdgeRoute Orthogonal(RouteEnd source, RouteEnd target) =>
        EdgeRouter.Route(new RouteRequest(EdgeRouting.Orthogonal, source, target));

    private static EdgeRoute Curved(RouteEnd source, RouteEnd target) =>
        EdgeRouter.Route(new RouteRequest(EdgeRouting.Curved, source, target));

    private static (double X, double Y) Direction(
        (double X, double Y) from,
        (double X, double Y) to
    )
    {
        var length = Math.Sqrt(
            (to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y)
        );
        return ((to.X - from.X) / length, (to.Y - from.Y) / length);
    }

    private static double Length((double X, double Y) from, (double X, double Y) to) =>
        Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y);

    private static void AssertLeavesAlong(
        (double X, double Y) end,
        (double X, double Y) next,
        PortId side
    )
    {
        Assert.Equal(EdgeRouter.Normal(side), Direction(end, next));
        Assert.True(
            Length(end, next) >= EdgeRouter.Stub,
            $"stub of {Length(end, next)} is shorter than {EdgeRouter.Stub}"
        );
    }

    private static void AssertOrthogonal(EdgeRoute route)
    {
        for (var i = 1; i < route.Points.Count; i++)
        {
            var (from, to) = (route.Points[i - 1], route.Points[i]);
            Assert.True(from.X == to.X || from.Y == to.Y, $"segment {i} is diagonal");
        }
    }

    private static bool EntersInterior(EdgeRoute route, Bounds shape)
    {
        for (var i = 1; i < route.Points.Count; i++)
        {
            var (from, to) = (route.Points[i - 1], route.Points[i]);
            if (
                Math.Max(from.X, to.X) > shape.X
                && Math.Min(from.X, to.X) < shape.Right
                && Math.Max(from.Y, to.Y) > shape.Y
                && Math.Min(from.Y, to.Y) < shape.Bottom
            )
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void TheStubIsTheBaseGridStep() =>
        Assert.Equal(DiagramCanvas.GridBaseSpacing, EdgeRouter.Stub);

    [Fact]
    public void AnOrthogonalEdgeFromATopPortLeavesUpward()
    {
        var route = Orthogonal(
            PortEnd(new Bounds(0, 100, 100, 100), PortId.Top),
            PortEnd(new Bounds(300, 0, 100, 100), PortId.Left)
        );

        AssertLeavesAlong(route.Points[0], route.Points[1], PortId.Top);
    }

    [Theory]
    [MemberData(nameof(SidePairs))]
    public void BetweenShapesInOverlappingColumnsEveryPairOfSidesLeavesAlongBothAndRoutesAround(
        PortId sourceSide,
        PortId targetSide
    )
    {
        var route = Orthogonal(PortEnd(Upper, sourceSide), PortEnd(LowerAndRight, targetSide));

        AssertOrthogonal(route);
        AssertLeavesAlong(route.Points[0], route.Points[1], sourceSide);
        AssertLeavesAlong(route.Points[^1], route.Points[^2], targetSide);
        Assert.False(EntersInterior(route, Upper));
        Assert.False(EntersInterior(route, LowerAndRight));
    }

    [Theory]
    [MemberData(nameof(SidePairs))]
    public void BetweenShapesSideBySideEveryPairOfSidesLeavesAlongBothAndRoutesAround(
        PortId sourceSide,
        PortId targetSide
    )
    {
        var right = new Bounds(300, 40, 100, 100);
        var route = Orthogonal(PortEnd(Upper, sourceSide), PortEnd(right, targetSide));

        AssertOrthogonal(route);
        AssertLeavesAlong(route.Points[0], route.Points[1], sourceSide);
        AssertLeavesAlong(route.Points[^1], route.Points[^2], targetSide);
        Assert.False(EntersInterior(route, Upper));
        Assert.False(EntersInterior(route, right));
    }

    [Fact]
    public void AnEdgeBetweenOverlappingColumnsGoesAroundRatherThanThroughEitherShape()
    {
        var route = Orthogonal(PortEnd(Upper, PortId.Top), PortEnd(LowerAndRight, PortId.Bottom));

        Assert.False(EntersInterior(route, Upper));
        Assert.False(EntersInterior(route, LowerAndRight));
        Assert.True(route.Points.Count > 2);
    }

    [Fact]
    public void APortFacingAwayRoutesAroundItsOwnShape()
    {
        var left = new Bounds(0, 0, 100, 100);
        var right = new Bounds(300, 0, 100, 100);

        var route = Orthogonal(PortEnd(right, PortId.Right), PortEnd(left, PortId.Left));

        AssertLeavesAlong(route.Points[0], route.Points[1], PortId.Right);
        Assert.False(EntersInterior(route, left));
        Assert.False(EntersInterior(route, right));
    }

    [Fact]
    public void ThePathCostsBendsSoAStraightRunBeatsAShorterOneWithMoreTurns()
    {
        var route = Orthogonal(
            PortEnd(new Bounds(0, 0, 100, 100), PortId.Right),
            PortEnd(new Bounds(300, 0, 100, 100), PortId.Left)
        );

        Assert.Equal([(100.0, 50.0), (300.0, 50.0)], route.Points);
    }

    [Fact]
    public void ADiagonalPairBendsInTheGapBetweenTheShapes()
    {
        var route = Orthogonal(
            PortEnd(new Bounds(100, 100, 50, 50), PortId.Right),
            PortEnd(new Bounds(300, 200, 50, 50), PortId.Left)
        );

        Assert.Equal("M 150 125 L 225 125 L 225 225 L 300 225", route.PathData);
    }

    [Fact]
    public void OverlappingShapesFallBackToAPathThatIgnoresThemButStillLeavesAlongEachSide()
    {
        var first = new Bounds(0, 0, 100, 100);
        var second = new Bounds(50, 50, 100, 100);

        var route = Orthogonal(PortEnd(first, PortId.Right), PortEnd(second, PortId.Left));

        AssertOrthogonal(route);
        AssertLeavesAlong(route.Points[0], route.Points[1], PortId.Right);
        AssertLeavesAlong(route.Points[^1], route.Points[^2], PortId.Left);
    }

    [Fact]
    public void AStubPointInsideTheOtherShapeFallsBackRatherThanFailing()
    {
        var route = Orthogonal(
            PortEnd(new Bounds(0, 0, 100, 100), PortId.Right),
            PortEnd(new Bounds(110, 0, 100, 100), PortId.Right)
        );

        AssertOrthogonal(route);
        AssertLeavesAlong(route.Points[0], route.Points[1], PortId.Right);
        AssertLeavesAlong(route.Points[^1], route.Points[^2], PortId.Right);
    }

    [Theory]
    [InlineData(100, 10, PortId.Right)]
    [InlineData(-100, 10, PortId.Left)]
    [InlineData(10, 100, PortId.Bottom)]
    [InlineData(10, -100, PortId.Top)]
    [InlineData(50, 50, PortId.Right)]
    [InlineData(-50, -50, PortId.Left)]
    public void AFloatingEndFacesTheOtherEndAlongTheAxisThatPointsMoreTowardsIt(
        double dx,
        double dy,
        PortId expected
    ) => Assert.Equal(expected, EdgeRouter.PseudoSide((0, 0), (dx, dy)));

    [Fact]
    public void AFloatingEndsFirstSegmentFollowsItsPseudoSide()
    {
        var floating = new RouteEnd(0, 0, EdgeRouter.PseudoSide((0, 0), (50, 300)), null);

        var route = Orthogonal(floating, PortEnd(new Bounds(0, 300, 100, 100), PortId.Left));

        AssertLeavesAlong(route.Points[0], route.Points[1], PortId.Bottom);
    }

    [Fact]
    public void TwoFloatingEndsCloserThanTwoStubsLoopOutRatherThanDoublingBack()
    {
        var route = Orthogonal(
            new RouteEnd(0, 0, PortId.Right, null),
            new RouteEnd(10, 0, PortId.Left, null)
        );

        AssertOrthogonal(route);
        AssertLeavesAlong(route.Points[0], route.Points[1], PortId.Right);
        AssertLeavesAlong(route.Points[^1], route.Points[^2], PortId.Left);
        for (var i = 2; i < route.Points.Count; i++)
        {
            var (a, b, c) = (route.Points[i - 2], route.Points[i - 1], route.Points[i]);
            var dot = (b.X - a.X) * (c.X - b.X) + (b.Y - a.Y) * (c.Y - b.Y);
            Assert.True(dot >= 0, $"the path turns straight back at {b}");
        }
    }

    [Theory]
    [InlineData(0, 0, PortId.Top)]
    [InlineData(1, 0, PortId.Top)]
    [InlineData(1, 1, PortId.Right)]
    [InlineData(0, 1, PortId.Bottom)]
    [InlineData(0, 0.3, PortId.Left)]
    [InlineData(1, 0.3, PortId.Right)]
    [InlineData(0.7, 1, PortId.Bottom)]
    public void ACustomPortTakesTheBorderItsFractionLiesOn(
        double fractionX,
        double fractionY,
        PortId expected
    ) => Assert.Equal(expected, EdgeRouter.SideOfFraction(fractionX, fractionY));

    [Theory]
    [MemberData(nameof(SidePairs))]
    public void ACurvedEdgesControlPointsLieOnEachEndsNormal(PortId sourceSide, PortId targetSide)
    {
        var source = PortEnd(Upper, sourceSide);
        var target = PortEnd(LowerAndRight, targetSide);

        var route = Curved(source, target);

        Assert.Equal(EdgeRouter.Normal(sourceSide), Direction(route.Points[0], route.Points[1]));
        Assert.Equal(EdgeRouter.Normal(targetSide), Direction(route.Points[3], route.Points[2]));
    }

    [Fact]
    public void ACurvedEdgesControlOffsetIsFourTenthsOfTheDistance()
    {
        var route = Curved(
            PortEnd(new Bounds(0, 0, 100, 100), PortId.Right),
            PortEnd(new Bounds(400, 0, 100, 100), PortId.Left)
        );

        Assert.Equal((100 + 0.4 * 300, 50.0), route.Points[1]);
        Assert.Equal((400 - 0.4 * 300, 50.0), route.Points[2]);
    }

    [Fact]
    public void ACurvedEdgesControlOffsetIsNeverBelowTheStub()
    {
        var route = Curved(
            PortEnd(new Bounds(0, 0, 100, 100), PortId.Right),
            PortEnd(new Bounds(110, 0, 100, 100), PortId.Left)
        );

        Assert.Equal((100 + EdgeRouter.Stub, 50.0), route.Points[1]);
        Assert.Equal((110 - EdgeRouter.Stub, 50.0), route.Points[2]);
    }

    [Fact]
    public void ACurvedEdgesLabelSitsAtTheMiddleOfTheCurve()
    {
        var route = Curved(
            PortEnd(new Bounds(0, 0, 100, 100), PortId.Right),
            PortEnd(new Bounds(400, 200, 100, 100), PortId.Left)
        );

        var (p0, p1, p2, p3) = (route.Points[0], route.Points[1], route.Points[2], route.Points[3]);
        Assert.Equal(
            ((p0.X + 3 * p1.X + 3 * p2.X + p3.X) / 8, (p0.Y + 3 * p1.Y + 3 * p2.Y + p3.Y) / 8),
            route.LabelAnchor
        );
    }

    [Theory]
    [MemberData(nameof(SidePairs))]
    public void AnOrthogonalEdgesLabelSitsOnThePathHalfwayAlongIt(
        PortId sourceSide,
        PortId targetSide
    )
    {
        var route = Orthogonal(PortEnd(Upper, sourceSide), PortEnd(LowerAndRight, targetSide));
        var anchor = route.LabelAnchor;

        var walked = 0.0;
        var total = 0.0;
        for (var i = 1; i < route.Points.Count; i++)
        {
            total += Length(route.Points[i - 1], route.Points[i]);
        }

        for (var i = 1; i < route.Points.Count; i++)
        {
            var (from, to) = (route.Points[i - 1], route.Points[i]);
            var onSegment =
                anchor.X >= Math.Min(from.X, to.X) - 1e-9
                && anchor.X <= Math.Max(from.X, to.X) + 1e-9
                && anchor.Y >= Math.Min(from.Y, to.Y) - 1e-9
                && anchor.Y <= Math.Max(from.Y, to.Y) + 1e-9;
            if (onSegment)
            {
                Assert.Equal(total / 2, walked + Length(from, anchor), 6);
                return;
            }

            walked += Length(from, to);
        }

        Assert.Fail($"label anchor {anchor} is on no segment of the path");
    }

    [Fact]
    public void AStraightEdgeIsTheLineBetweenItsEndsWithTheLabelAtItsMidpoint()
    {
        var route = EdgeRouter.Route(
            new RouteRequest(
                EdgeRouting.Straight,
                new RouteEnd(0, 0, PortId.Right, null),
                new RouteEnd(100, 40, PortId.Left, null)
            )
        );

        Assert.Equal([(0.0, 0.0), (100.0, 40.0)], route.Points);
        Assert.Equal((50.0, 20.0), route.LabelAnchor);
    }
}
