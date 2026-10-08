using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class ObjectSnapTests
{
    private const double Tolerance = 8;

    [Fact]
    public void AMoversLeftEdgeNearACandidatesLeftEdgeSnapsToIt()
    {
        var mover = new Bounds(105, 300, 50, 50);
        Bounds[] candidates = [new Bounds(100, 0, 80, 40)];

        var snap = ObjectSnap.ForMove(mover, candidates, SnapAxis.X, Tolerance, held: null);

        Assert.Equal(new AxisSnap(SnapAnchor.Start, 100), snap);
        Assert.Equal(-5, ObjectSnap.Offset(mover, SnapAxis.X, snap!.Value));
    }

    [Fact]
    public void CentresPairWithCentres()
    {
        var mover = new Bounds(300, 112, 40, 40);
        Bounds[] candidates = [new Bounds(0, 100, 50, 60)];

        var snap = ObjectSnap.ForMove(mover, candidates, SnapAxis.Y, Tolerance, held: null);

        Assert.Equal(new AxisSnap(SnapAnchor.Centre, 130), snap);
    }

    [Fact]
    public void AMoversEndPairsWithACandidatesStartSoShapesCanAbut()
    {
        var mover = new Bounds(46, 300, 50, 50);
        Bounds[] candidates = [new Bounds(100, 0, 50, 50)];

        var snap = ObjectSnap.ForMove(mover, candidates, SnapAxis.X, Tolerance, held: null);

        Assert.Equal(new AxisSnap(SnapAnchor.End, 100), snap);
    }

    [Fact]
    public void NothingWithinToleranceIsNoSnap()
    {
        var mover = new Bounds(300, 300, 50, 50);
        Bounds[] candidates = [new Bounds(100, 0, 80, 40)];

        Assert.Null(ObjectSnap.ForMove(mover, candidates, SnapAxis.X, Tolerance, held: null));
    }

    [Fact]
    public void TheNearestMatchWins()
    {
        var mover = new Bounds(103, 300, 50, 50);
        Bounds[] candidates = [new Bounds(97, 0, 30, 30), new Bounds(102.5, 100, 30, 30)];

        var snap = ObjectSnap.ForMove(mover, candidates, SnapAxis.X, Tolerance, held: null);

        Assert.Equal(new AxisSnap(SnapAnchor.Start, 102.5), snap);
    }

    [Fact]
    public void AHeldSnapSurvivesPastToleranceUpToTheStickyDistance()
    {
        Bounds[] candidates = [new Bounds(100, 0, 2, 40)];
        var held = new AxisSnap(SnapAnchor.Start, 100);
        var sticky = Tolerance * ObjectSnap.StickyFactor;

        var stillHeld = ObjectSnap.ForMove(
            new Bounds(100 + sticky - 0.5, 300, 50, 50),
            candidates,
            SnapAxis.X,
            Tolerance,
            held
        );
        var letGo = ObjectSnap.ForMove(
            new Bounds(100 + sticky + 0.5, 300, 50, 50),
            candidates,
            SnapAxis.X,
            Tolerance,
            held
        );

        Assert.Equal(held, stillHeld);
        Assert.Null(letGo);
    }

    [Fact]
    public void AHeldSnapWhoseCandidateIsGoneIsDropped()
    {
        var held = new AxisSnap(SnapAnchor.Start, 100);

        var snap = ObjectSnap.ForMove(
            new Bounds(110, 300, 50, 50),
            [new Bounds(400, 0, 80, 40)],
            SnapAxis.X,
            Tolerance,
            held
        );

        Assert.Null(snap);
    }

    [Fact]
    public void AResizedEdgeOffersOnlyItselfSoTheBoxCentreNeverMatches()
    {
        var box = new Bounds(0, 0, 196, 50);
        var besideTheEnd = new Bounds(200, 200, 100, 100);
        var besideTheCentre = new Bounds(95, 300, 10, 10);

        var snap = ObjectSnap.ForEdge(
            box,
            SnapAnchor.End,
            [besideTheEnd, besideTheCentre],
            SnapAxis.X,
            Tolerance,
            held: null
        );

        Assert.Equal(new AxisSnap(SnapAnchor.End, 200), snap);
        Assert.Null(
            ObjectSnap.ForEdge(
                box,
                SnapAnchor.End,
                [besideTheCentre],
                SnapAxis.X,
                Tolerance,
                held: null
            )
        );
    }

    [Fact]
    public void AMoverBesideARowSnapsToTheRowsOwnGap()
    {
        Bounds[] row = [new Bounds(0, 0, 50, 50), new Bounds(90, 0, 50, 50)];
        var mover = new Bounds(183, 10, 50, 50);

        var snap = ObjectSnap.ForMove(mover, row, SnapAxis.X, Tolerance, held: null);

        Assert.Equal(new AxisSnap(SnapAnchor.Start, 180), snap);
    }

    [Fact]
    public void EqualSpacingAlsoOffersTheGapBeforeTheRow()
    {
        Bounds[] row = [new Bounds(100, 0, 50, 50), new Bounds(190, 0, 50, 50)];
        var mover = new Bounds(8, 10, 50, 50);

        var snap = ObjectSnap.ForMove(mover, row, SnapAxis.X, Tolerance, held: null);

        Assert.Equal(new AxisSnap(SnapAnchor.End, 60), snap);
    }

    [Fact]
    public void EqualSpacingCentresAMoverBetweenTwoWithRoomForIt()
    {
        Bounds[] row = [new Bounds(0, 0, 50, 50), new Bounds(200, 0, 50, 50)];

        var targets = ObjectSnap.EqualSpacingTargets(new Bounds(97, 0, 50, 50), row, SnapAxis.X);

        Assert.Contains(new AxisSnap(SnapAnchor.Start, 100), targets);
    }

    [Fact]
    public void ARowHoldsOnlyCandidatesOverlappingTheMoverOnThePerpendicularAxis()
    {
        var mover = new Bounds(300, 100, 50, 50);
        var inRow = new Bounds(0, 120, 50, 50);
        var above = new Bounds(100, 0, 50, 100);
        var below = new Bounds(200, 150, 50, 50);

        var row = ObjectSnap.Row(mover, [inRow, above, below], SnapAxis.X);

        Assert.Equal([inRow], row);
    }

    [Fact]
    public void AGapNeedsThePairToShareAPerpendicularExtent()
    {
        Bounds[] row = [new Bounds(0, 0, 50, 50), new Bounds(90, 60, 50, 50)];

        Assert.Empty(ObjectSnap.Gaps(row, SnapAxis.X));
    }

    [Fact]
    public void GapsAreBetweenNeighboursOnly()
    {
        var first = new Bounds(0, 0, 50, 50);
        var second = new Bounds(90, 0, 50, 50);
        var third = new Bounds(200, 0, 50, 50);

        var gaps = ObjectSnap.Gaps([third, first, second], SnapAxis.X);

        Assert.Equal(
            new[] { (first, second, 40.0), (second, third, 60.0) }.OrderBy(gap => gap.Item3),
            gaps.Select(gap => (gap.Before, gap.After, gap.Size)).OrderBy(gap => gap.Size)
        );
    }

    [Fact]
    public void TheSecondPassDrawsAGuideAlongEveryCoordinateTheSnappedMoverSharesWithACandidate()
    {
        var snapped = new Bounds(100, 300, 50, 50);
        Bounds[] candidates = [new Bounds(100, 0, 50, 40), new Bounds(125, 100, 20, 20)];

        var guides = ObjectSnap.GuidesForMove(snapped, candidates, SnapAxis.X).ToList();

        Assert.Equal(
            new SnapGuide[]
            {
                new AlignmentGuide(SnapAxis.X, 100),
                new AlignmentGuide(SnapAxis.X, 125),
                new AlignmentGuide(SnapAxis.X, 150),
            },
            guides
        );
    }

    [Fact]
    public void TheSecondPassDrawsTheMoversGapBesideTheGapItRepeats()
    {
        Bounds[] row = [new Bounds(0, 0, 50, 50), new Bounds(90, 0, 50, 50)];
        var snapped = new Bounds(180, 20, 50, 50);

        var guides = ObjectSnap.GuidesForMove(snapped, row, SnapAxis.X).ToList();

        Assert.Equal(
            new SnapGuide[]
            {
                new SpacingGuide(SnapAxis.X, 140, 180, 35),
                new SpacingGuide(SnapAxis.X, 50, 90, 25),
            },
            guides
        );
    }

    [Fact]
    public void AnUnrepeatedGapDrawsNothing()
    {
        Bounds[] row = [new Bounds(0, 0, 50, 50), new Bounds(90, 0, 50, 50)];

        Assert.Empty(ObjectSnap.GuidesForMove(new Bounds(185, 0, 50, 50), row, SnapAxis.X));
    }

    [Fact]
    public void ScreenPixelDistancesKeepTheirOrder()
    {
        Assert.True(ScreenPixels.DragThreshold < ScreenPixels.ObjectSnapTolerance);
        Assert.True(ScreenPixels.ObjectSnapTolerance < ScreenPixels.EdgeHitBand);
        Assert.True(ObjectSnap.StickyFactor > 1);
    }
}
