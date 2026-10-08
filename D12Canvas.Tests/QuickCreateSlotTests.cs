using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class QuickCreateSlotTests
{
    private const double Spacing = 20;
    private const double Gap = 2 * Spacing;
    private static readonly Bounds Source = new(100, 100, 120, 60);

    public static TheoryData<PortId, double, double> EmptyBoardSlots =>
        new()
        {
            { PortId.Right, Source.Right + Gap, Source.Y },
            { PortId.Left, Source.X - Gap - Source.Width, Source.Y },
            { PortId.Bottom, Source.X, Source.Bottom + Gap },
            { PortId.Top, Source.X, Source.Y - Gap - Source.Height },
        };

    [Theory]
    [MemberData(nameof(EmptyBoardSlots))]
    public void OnAnEmptyBoardTheCopyLandsOneGapPastThePressedSideCentredOnTheSource(
        PortId side,
        double expectedX,
        double expectedY
    )
    {
        var slot = QuickCreateSlot.For(Source, side, Gap, Spacing, [Source]);

        Assert.Equal(new Bounds(expectedX, expectedY, Source.Width, Source.Height), slot);
    }

    [Fact]
    public void TheCopyIsCentredOnTheSourcesPerpendicularAxisBeforeSnapping()
    {
        var narrow = new Bounds(100, 100, 40, 40);

        var slot = QuickCreateSlot.For(narrow, PortId.Bottom, Gap, gridSpacing: null, [narrow]);

        Assert.Equal(new Bounds(100, 180, 40, 40), slot);
    }

    [Fact]
    public void AnOffGridSourceGivesAnOnGridCopyWhenSnapping()
    {
        var offGrid = new Bounds(103, 97, 120, 60);

        var slot = QuickCreateSlot.For(offGrid, PortId.Right, Gap, Spacing, [offGrid]);

        Assert.Equal(0, slot.X % Spacing);
        Assert.Equal(0, slot.Y % Spacing);
        Assert.False(slot.Intersects(offGrid));
    }

    [Fact]
    public void AnOccupiedSlotStepsOneGapFurtherAlongTheSameAxis()
    {
        var occupant = new Bounds(Source.Right + Gap, Source.Y, 20, 20);

        var slot = QuickCreateSlot.For(Source, PortId.Right, Gap, Spacing, [Source, occupant]);

        Assert.Equal(Source.Right + 2 * Gap, slot.X);
        Assert.Equal(Source.Y, slot.Y);
    }

    [Fact]
    public void AnOccupiedSlotAboveStepsFurtherUp()
    {
        var occupant = new Bounds(Source.X + 30, Source.Y - 80, 20, 20);

        var slot = QuickCreateSlot.For(Source, PortId.Top, Gap, Spacing, [Source, occupant]);

        Assert.Equal(Source.X, slot.X);
        Assert.True(slot.Bottom < occupant.Y);
        Assert.Equal(0, (Source.Y - Gap - Source.Height - slot.Y) % Gap);
    }

    [Fact]
    public void AnObstacleOffTheAxisLeavesTheFirstSlotFree()
    {
        var aside = new Bounds(Source.Right + Gap, Source.Bottom + 200, 120, 60);

        var slot = QuickCreateSlot.For(Source, PortId.Right, Gap, Spacing, [Source, aside]);

        Assert.Equal(Source.Right + Gap, slot.X);
    }

    [Fact]
    public void WhereverTheObstaclesLieTheSlotFoundIntersectsNone()
    {
        var random = new Random(103);
        for (var run = 0; run < 200; run++)
        {
            var obstacles = Enumerable
                .Range(0, random.Next(1, 30))
                .Select(_ => new Bounds(
                    random.Next(-400, 800),
                    random.Next(-400, 800),
                    random.Next(10, 300),
                    random.Next(10, 300)
                ))
                .Append(Source)
                .ToList();
            var side = (PortId)random.Next(4);

            var slot = QuickCreateSlot.For(Source, side, Gap, Spacing, obstacles);

            Assert.DoesNotContain(obstacles, obstacle => obstacle.Intersects(slot));
            Assert.Equal((Source.Width, Source.Height), (slot.Width, slot.Height));
        }
    }
}
