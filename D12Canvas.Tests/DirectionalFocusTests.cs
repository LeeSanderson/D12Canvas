using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class DirectionalFocusTests
{
    private static readonly Bounds Origin = new(100, 100, 50, 50);

    private static FocusCandidate Candidate(
        double x,
        double y,
        double width = 50,
        double height = 50
    ) => new(Guid.NewGuid(), new Bounds(x, y, width, height));

    [Fact]
    public void AStopInTheSameRowBeatsANearerOneOffIt()
    {
        var sameRowFarther = Candidate(400, 100);
        var offRowNearer = Candidate(160, 160);

        var nearest = DirectionalFocus.Nearest(
            Origin,
            FocusDirection.Right,
            [offRowNearer, sameRowFarther]
        );

        Assert.Equal(sameRowFarther.Id, nearest);
    }

    [Fact]
    public void TheNearestStopInTheRowWinsByGap()
    {
        var far = Candidate(400, 120);
        var near = Candidate(200, 80);

        Assert.Equal(near.Id, DirectionalFocus.Nearest(Origin, FocusDirection.Right, [far, near]));
    }

    [Fact]
    public void OffTheRowTheSmallestGapPlusOffsetWins()
    {
        var closeGapFarOffset = Candidate(160, 400);
        var biggerGapSmallOffset = Candidate(220, 170);

        var nearest = DirectionalFocus.Nearest(
            Origin,
            FocusDirection.Right,
            [closeGapFarOffset, biggerGapSmallOffset]
        );

        Assert.Equal(biggerGapSmallOffset.Id, nearest);
    }

    [Fact]
    public void AStopOverlappingTheOriginOnTheMovementAxisIsNeverChosen()
    {
        var overlapping = Candidate(140, 100);

        Assert.Null(DirectionalFocus.Nearest(Origin, FocusDirection.Right, [overlapping]));
    }

    [Fact]
    public void EqualScoresResolveInReadingOrder()
    {
        var below = Candidate(200, 200);
        var above = Candidate(200, 0);

        Assert.Equal(
            above.Id,
            DirectionalFocus.Nearest(Origin, FocusDirection.Right, [below, above])
        );
    }

    [Fact]
    public void NothingInThatDirectionIsNowhere()
    {
        var left = Candidate(0, 100);

        Assert.Null(DirectionalFocus.Nearest(Origin, FocusDirection.Right, [left]));
    }

    [Theory]
    [InlineData("ArrowRight", 300, 100)]
    [InlineData("ArrowLeft", -100, 100)]
    [InlineData("ArrowDown", 100, 300)]
    [InlineData("ArrowUp", 100, -100)]
    public void EachArrowReachesTheStopOnItsSide(string code, double x, double y)
    {
        var expected = Candidate(x, y);
        var others = new[]
        {
            Candidate(300, 100),
            Candidate(-100, 100),
            Candidate(100, 300),
            Candidate(100, -100),
        }.Where(candidate => candidate.Bounds != expected.Bounds);

        var nearest = DirectionalFocus.Nearest(
            Origin,
            DirectionalFocus.DirectionFor(code)!.Value,
            others.Append(expected)
        );

        Assert.Equal(expected.Id, nearest);
    }

    [Fact]
    public void APointOriginIsInTheRowOfAStopSpanningIt()
    {
        var point = new Bounds(125, 125, 0, 0);
        var spanning = Candidate(400, 100);
        var nearerOffRow = Candidate(150, 160);

        var nearest = DirectionalFocus.Nearest(
            point,
            FocusDirection.Right,
            [nearerOffRow, spanning]
        );

        Assert.Equal(spanning.Id, nearest);
    }
}
