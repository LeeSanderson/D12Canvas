using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

// Framing scales a board rect to fit within 0.9 of the container, never past 1.0, clamps to the
// host's zoom limits and centres the rect at the scale it got, raising one change for all three.
public class ZoomPanTrackerFramingTests
{
    private static ZoomPanTracker TrackerOf(int width, int height)
    {
        var tracker = new ZoomPanTracker();
        tracker.SetContainerSize(width, height);
        return tracker;
    }

    private static (double X, double Y) ContainerPointOf(
        ZoomPanTracker tracker,
        double boardX,
        double boardY
    ) => (boardX * tracker.Scale + tracker.PanX, boardY * tracker.Scale + tracker.PanY);

    private static void AssertCentred(ZoomPanTracker tracker, Bounds target)
    {
        var centre = ContainerPointOf(
            tracker,
            target.X + target.Width / 2,
            target.Y + target.Height / 2
        );
        Assert.Equal(tracker.ContainerWidth / 2, centre.X, precision: 9);
        Assert.Equal(tracker.ContainerHeight / 2, centre.Y, precision: 9);
    }

    public static TheoryData<Bounds, double> Fits =>
        new()
        {
            { new Bounds(0, 0, 100, 100), 1.0 },
            { new Bounds(50000, 50000, 300, 200), 1.0 },
            { new Bounds(-4000, 0, 4000, 100), 0.9 * 1000 / 4000 },
            { new Bounds(0, 0, 100, 3000), 0.9 * 600 / 3000 },
            { new Bounds(0, 0, 2000, 1500), 0.9 * 600 / 1500 },
            { new Bounds(10, 20, 0, 0), 1.0 },
            { new Bounds(10, 20, 0, 1200), 0.9 * 600 / 1200 },
        };

    [Theory]
    [MemberData(nameof(Fits))]
    public void FramingContainsTheTargetWithinTheMarginCappedAtOneAndCentred(
        Bounds target,
        double expectedScale
    )
    {
        var tracker = TrackerOf(1000, 600);

        Assert.True(tracker.Frame(target));

        Assert.Equal(expectedScale, tracker.Scale, precision: 9);
        AssertCentred(tracker, target);
    }

    [Fact]
    public void FramingRaisesOneChangeCarryingTheNewScaleAndPanTogether()
    {
        var tracker = TrackerOf(1000, 600);
        var changes = new List<ZoomPanChangedEventArgs>();
        tracker.Changed += (_, args) => changes.Add(args);

        tracker.Frame(new Bounds(0, 0, 4000, 100));

        var change = Assert.Single(changes);
        Assert.Equal(tracker.Scale, change.Scale);
        Assert.Equal(tracker.PanX, change.PanX);
        Assert.Equal(tracker.PanY, change.PanY);
    }

    [Fact]
    public void AHostCeilingBelowTheFitWinsAndTheTargetIsStillCentred()
    {
        var tracker = TrackerOf(1000, 600);
        tracker.SetZoomLimits(null, 0.5);
        var target = new Bounds(0, 0, 100, 100);

        tracker.Frame(target);

        Assert.Equal(0.5, tracker.Scale);
        AssertCentred(tracker, target);
    }

    [Fact]
    public void AHostFloorAboveTheFitWinsAndTheTargetsCentreIsCentred()
    {
        var tracker = TrackerOf(1000, 600);
        tracker.SetZoomLimits(0.5, null);
        var target = new Bounds(0, 0, 10000, 10000);

        tracker.Frame(target);

        Assert.Equal(0.5, tracker.Scale);
        AssertCentred(tracker, target);
    }

    [Fact]
    public void FramingBeforeTheContainerSizeIsKnownDoesNothing()
    {
        var tracker = new ZoomPanTracker();
        var changed = false;
        tracker.Changed += (_, _) => changed = true;

        Assert.False(tracker.Frame(new Bounds(500, 500, 100, 100)));

        Assert.False(changed);
        Assert.Equal((1.0, 0.0, 0.0), (tracker.Scale, tracker.PanX, tracker.PanY));
    }

    [Fact]
    public void FramingWhatIsAlreadyFramedRaisesNothing()
    {
        var tracker = TrackerOf(1000, 600);
        var target = new Bounds(100, 100, 200, 200);
        tracker.Frame(target);
        var changed = false;
        tracker.Changed += (_, _) => changed = true;

        Assert.False(tracker.Frame(target));

        Assert.False(changed);
    }

    [Fact]
    public void FramingFromAnyStartingViewLandsOnTheSameDestination()
    {
        var target = new Bounds(300, -200, 800, 400);
        var fresh = TrackerOf(1000, 600);
        var moved = TrackerOf(1000, 600);
        moved.SetScaleAbout(0, 0, 3.7);
        moved.SetPanPosition(-9000, 4000);

        fresh.Frame(target);
        moved.Frame(target);

        Assert.Equal(fresh.Scale, moved.Scale, precision: 12);
        Assert.Equal(fresh.PanX, moved.PanX, precision: 9);
        Assert.Equal(fresh.PanY, moved.PanY, precision: 9);
    }

    [Fact]
    public void SettingTheScaleAboutAPointKeepsTheBoardPointUnderIt()
    {
        var tracker = TrackerOf(1000, 600);
        tracker.SetScaleAbout(0, 0, 0.25);
        tracker.SetPanPosition(130, -70);
        var before = ((500 - tracker.PanX) / tracker.Scale, (300 - tracker.PanY) / tracker.Scale);

        Assert.True(tracker.SetScaleAbout(500, 300, 1.0));

        Assert.Equal(1.0, tracker.Scale);
        Assert.Equal(before.Item1, 500 - tracker.PanX, precision: 9);
        Assert.Equal(before.Item2, 300 - tracker.PanY, precision: 9);
    }
}
