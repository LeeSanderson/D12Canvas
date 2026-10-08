using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class AlignDistributeTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly Guid C = Guid.NewGuid();
    private static readonly Guid D = Guid.NewGuid();

    private static ArrangedEntity Entity(
        Guid id,
        double x,
        double y,
        double width,
        double height
    ) => new(id, new Bounds(x, y, width, height));

    private static readonly ArrangedEntity[] Staggered =
    [
        Entity(A, 10, 0, 40, 20),
        Entity(B, 30, 50, 100, 30),
        Entity(C, 70, 100, 20, 60),
    ];

    private static Dictionary<Guid, (double Dx, double Dy)> ByEntity(
        IEnumerable<ArrangeDelta> deltas
    ) => deltas.ToDictionary(delta => delta.Id, delta => (delta.Dx, delta.Dy));

    [Fact]
    public void AlignLeftMovesEveryLeftEdgeToTheLeftmostAndLeavesThatEntityOut()
    {
        var deltas = ByEntity(AlignDistribute.Align(Staggered, AlignEdge.Left, null));

        Assert.Equal(
            new Dictionary<Guid, (double, double)> { [B] = (-20, 0), [C] = (-60, 0) },
            deltas
        );
    }

    [Fact]
    public void AlignRightMovesEveryRightEdgeToTheRightmost()
    {
        var deltas = ByEntity(AlignDistribute.Align(Staggered, AlignEdge.Right, null));

        Assert.Equal(
            new Dictionary<Guid, (double, double)> { [A] = (80, 0), [C] = (40, 0) },
            deltas
        );
    }

    [Fact]
    public void AlignCentreMovesEveryCentreToTheSelectionBoxCentre()
    {
        var deltas = ByEntity(AlignDistribute.Align(Staggered, AlignEdge.Centre, null));

        Assert.Equal(
            new Dictionary<Guid, (double, double)>
            {
                [A] = (40, 0),
                [B] = (-10, 0),
                [C] = (-10, 0),
            },
            deltas
        );
    }

    [Fact]
    public void AlignTopMovesEveryTopEdgeToTheTopmost()
    {
        var deltas = ByEntity(AlignDistribute.Align(Staggered, AlignEdge.Top, null));

        Assert.Equal(
            new Dictionary<Guid, (double, double)> { [B] = (0, -50), [C] = (0, -100) },
            deltas
        );
    }

    [Fact]
    public void AlignMiddleMovesEveryMiddleToTheSelectionBoxMiddle()
    {
        var deltas = ByEntity(AlignDistribute.Align(Staggered, AlignEdge.Middle, null));

        Assert.Equal(
            new Dictionary<Guid, (double, double)>
            {
                [A] = (0, 70),
                [B] = (0, 15),
                [C] = (0, -50),
            },
            deltas
        );
    }

    [Fact]
    public void AlignBottomMovesEveryBottomEdgeToTheBottommost()
    {
        var deltas = ByEntity(AlignDistribute.Align(Staggered, AlignEdge.Bottom, null));

        Assert.Equal(
            new Dictionary<Guid, (double, double)> { [A] = (0, 140), [B] = (0, 80) },
            deltas
        );
    }

    [Theory]
    [InlineData("Left")]
    [InlineData("Centre")]
    [InlineData("Right")]
    [InlineData("Top")]
    [InlineData("Middle")]
    [InlineData("Bottom")]
    public void AligningTwiceProducesNothingTheSecondTime(string edgeName)
    {
        var edge = Enum.Parse<AlignEdge>(edgeName);
        var aligned = Apply(Staggered, AlignDistribute.Align(Staggered, edge, 20));

        Assert.Empty(AlignDistribute.Align(aligned, edge, 20));
    }

    [Fact]
    public void AlignNeedsTwoEntities()
    {
        Assert.Empty(AlignDistribute.Align([Entity(A, 5, 5, 10, 10)], AlignEdge.Right, null));
    }

    [Fact]
    public void UnderSnapAlignLeftRoundsTheTargetToTheNearestLine()
    {
        ArrangedEntity[] entities = [Entity(A, 13, 0, 40, 20), Entity(B, 47, 50, 30, 20)];

        var deltas = ByEntity(AlignDistribute.Align(entities, AlignEdge.Left, 20));

        Assert.Equal(
            new Dictionary<Guid, (double, double)> { [A] = (7, 0), [B] = (-27, 0) },
            deltas
        );
    }

    [Fact]
    public void UnderSnapAlignRightKeepsEveryRightEdgeExactlyOnTheSnappedLine()
    {
        ArrangedEntity[] entities = [Entity(A, 13, 0, 47, 20), Entity(B, 30, 50, 33, 20)];

        var aligned = Apply(entities, AlignDistribute.Align(entities, AlignEdge.Right, 20));

        Assert.All(aligned, entity => Assert.Equal(60, entity.Bounds.Right));
    }

    [Fact]
    public void UnderSnapAlignCentreKeepsEveryCentreExactlyOnTheSnappedLine()
    {
        ArrangedEntity[] entities = [Entity(A, 0, 0, 50, 20), Entity(B, 80, 50, 30, 20)];

        var aligned = Apply(entities, AlignDistribute.Align(entities, AlignEdge.Centre, 20));

        Assert.All(aligned, entity => Assert.Equal(60, entity.Bounds.X + entity.Bounds.Width / 2));
    }

    private static readonly ArrangedEntity[] MixedRow =
    [
        Entity(A, 0, 0, 40, 20),
        Entity(B, 50, 0, 100, 20),
        Entity(C, 230, 0, 20, 20),
        Entity(D, 280, 0, 60, 20),
    ];

    [Fact]
    public void DistributeHorizontallyEqualisesTheGapsAndKeepsBothEndsInPlace()
    {
        var distributed = Apply(
            MixedRow,
            AlignDistribute.Distribute(MixedRow, DistributeAxis.Horizontal, null)
        );

        Assert.Equal([40d, 40d, 40d], Gaps(distributed, horizontal: true));
        Assert.Equal(0, distributed.Single(entity => entity.Id == A).Bounds.X);
        Assert.Equal(340, distributed.Single(entity => entity.Id == D).Bounds.Right);
    }

    [Fact]
    public void DistributeVerticallyEqualisesTheGapsDown()
    {
        ArrangedEntity[] column =
        [
            Entity(A, 0, 0, 20, 10),
            Entity(B, 0, 15, 20, 50),
            Entity(C, 0, 100, 20, 20),
        ];

        var distributed = Apply(
            column,
            AlignDistribute.Distribute(column, DistributeAxis.Vertical, null)
        );

        Assert.Equal([20d, 20d], Gaps(distributed, horizontal: false));
    }

    [Fact]
    public void DistributeOrdersByCentreNotByLeadingEdge()
    {
        ArrangedEntity[] entities =
        [
            Entity(A, 0, 0, 200, 20),
            Entity(B, 10, 0, 20, 20),
            Entity(C, 300, 0, 20, 20),
        ];

        var distributed = Apply(
            entities,
            AlignDistribute.Distribute(entities, DistributeAxis.Horizontal, null)
        );

        Assert.Equal(10, distributed.Single(entity => entity.Id == B).Bounds.X);
        Assert.Equal(
            distributed.Single(entity => entity.Id == C).Bounds.X
                - distributed.Single(entity => entity.Id == A).Bounds.Right,
            distributed.Single(entity => entity.Id == A).Bounds.X
                - distributed.Single(entity => entity.Id == B).Bounds.Right
        );
    }

    [Fact]
    public void UnderSnapEachGapIsAWholeGridStepAndTheFirstEntityStaysPut()
    {
        var distributed = Apply(
            MixedRow,
            AlignDistribute.Distribute(MixedRow, DistributeAxis.Horizontal, 30)
        );

        Assert.Equal([30d, 30d, 30d], Gaps(distributed, horizontal: true));
        Assert.Equal(0, distributed.Single(entity => entity.Id == A).Bounds.X);
    }

    [Fact]
    public void UnderSnapAPositiveGapNeverRoundsDownToZero()
    {
        ArrangedEntity[] tight =
        [
            Entity(A, 0, 0, 10, 10),
            Entity(B, 11, 0, 10, 10),
            Entity(C, 24, 0, 10, 10),
        ];

        var distributed = Apply(
            tight,
            AlignDistribute.Distribute(tight, DistributeAxis.Horizontal, 200)
        );

        Assert.Equal([200d, 200d], Gaps(distributed, horizontal: true));
    }

    [Fact]
    public void UnderSnapANegativeGapRoundsToTheNearestMultipleUnclamped()
    {
        ArrangedEntity[] overlapping =
        [
            Entity(A, 0, 0, 100, 10),
            Entity(B, 60, 0, 100, 10),
            Entity(C, 110, 0, 100, 10),
        ];

        var distributed = Apply(
            overlapping,
            AlignDistribute.Distribute(overlapping, DistributeAxis.Horizontal, 20)
        );

        Assert.Equal([-40d, -40d], Gaps(distributed, horizontal: true));
    }

    [Fact]
    public void DistributingAnEvenRowProducesNothing()
    {
        var distributed = Apply(
            MixedRow,
            AlignDistribute.Distribute(MixedRow, DistributeAxis.Horizontal, null)
        );

        Assert.Empty(AlignDistribute.Distribute(distributed, DistributeAxis.Horizontal, null));
    }

    [Fact]
    public void DistributeNeedsThreeEntities()
    {
        Assert.Empty(AlignDistribute.Distribute(MixedRow[..2], DistributeAxis.Horizontal, null));
    }

    private static ArrangedEntity[] Apply(
        IEnumerable<ArrangedEntity> entities,
        IEnumerable<ArrangeDelta> deltas
    )
    {
        var byId = ByEntity(deltas);
        return entities
            .Select(entity =>
                byId.TryGetValue(entity.Id, out var delta)
                    ? entity with
                    {
                        Bounds = entity.Bounds with
                        {
                            X = entity.Bounds.X + delta.Dx,
                            Y = entity.Bounds.Y + delta.Dy,
                        },
                    }
                    : entity
            )
            .ToArray();
    }

    private static double[] Gaps(IEnumerable<ArrangedEntity> entities, bool horizontal)
    {
        var ordered = entities
            .Select(entity => entity.Bounds)
            .OrderBy(bounds => horizontal ? bounds.X : bounds.Y)
            .ToArray();
        return ordered
            .Zip(
                ordered.Skip(1),
                (before, after) => horizontal ? after.X - before.Right : after.Y - before.Bottom
            )
            .ToArray();
    }
}
