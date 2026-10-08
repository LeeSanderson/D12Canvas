using Xunit;

namespace D12Canvas.Tests;

public class DuplicateRunTests
{
    private static readonly HashSet<Guid> NoEdges = [];

    [Fact]
    public void TheFirstDuplicateStepsTwentyEachWay()
    {
        var run = new DuplicateRun();

        Assert.Equal(
            (20, 20),
            run.OffsetFor(new HashSet<Guid> { Guid.NewGuid() }, NoEdges, (0, 0))
        );
    }

    [Fact]
    public void ADuplicateOfTheLastOutputRepeatsWhereThatOutputNowSitsFromItsSource()
    {
        var run = new DuplicateRun();
        var copy = Guid.NewGuid();
        run.RecordDuplicate([copy], [], (100, 40));

        Assert.Equal((137.5, -13), run.OffsetFor(new HashSet<Guid> { copy }, NoEdges, (237.5, 27)));
    }

    [Fact]
    public void ASelectionOtherThanTheLastOutputStartsAgainAtTheFirstStep()
    {
        var run = new DuplicateRun();
        var copy = Guid.NewGuid();
        run.RecordDuplicate([copy], [], (100, 40));

        Assert.Equal(
            (20, 20),
            run.OffsetFor(new HashSet<Guid> { copy, Guid.NewGuid() }, NoEdges, (300, 40))
        );
    }

    [Fact]
    public void TheEdgeSetMustMatchToo()
    {
        var run = new DuplicateRun();
        var copy = Guid.NewGuid();
        var edge = Guid.NewGuid();
        run.RecordDuplicate([copy], [edge], (0, 0));

        Assert.Equal((20, 20), run.OffsetFor(new HashSet<Guid> { copy }, NoEdges, (50, 0)));
        Assert.Equal(
            (50, 0),
            run.OffsetFor(new HashSet<Guid> { copy }, new HashSet<Guid> { edge }, (50, 0))
        );
    }

    [Fact]
    public void AnEdgeOnlyOutputContinuesTheRun()
    {
        var run = new DuplicateRun();
        var edge = Guid.NewGuid();
        run.RecordDuplicate([], [edge], (10, 10));

        Assert.Equal(
            (40, 0),
            run.OffsetFor(new HashSet<Guid>(), new HashSet<Guid> { edge }, (50, 10))
        );
    }

    [Fact]
    public void ASelectionChangeEndsTheRunEvenIfTheOutputIsSelectedAgainLater()
    {
        var run = new DuplicateRun();
        var copy = Guid.NewGuid();
        run.RecordDuplicate([copy], [], (0, 0));

        run.SelectionChanged(new HashSet<Guid> { Guid.NewGuid() }, NoEdges);
        run.SelectionChanged(new HashSet<Guid> { copy }, NoEdges);

        Assert.Equal((20, 20), run.OffsetFor(new HashSet<Guid> { copy }, NoEdges, (90, 0)));
    }

    [Fact]
    public void ReselectingExactlyTheOutputKeepsTheRun()
    {
        var run = new DuplicateRun();
        var copy = Guid.NewGuid();
        run.RecordDuplicate([copy], [], (0, 0));

        run.SelectionChanged(new HashSet<Guid> { copy }, NoEdges);

        Assert.Equal((90, 0), run.OffsetFor(new HashSet<Guid> { copy }, NoEdges, (90, 0)));
    }

    [Fact]
    public void EndingTheRunReturnsToTheFirstStep()
    {
        var run = new DuplicateRun();
        var copy = Guid.NewGuid();
        run.RecordDuplicate([copy], [], (0, 0));

        run.End();

        Assert.Equal((20, 20), run.OffsetFor(new HashSet<Guid> { copy }, NoEdges, (90, 0)));
    }

    [Fact]
    public void EachDuplicateInTheRunMeasuresFromThePreviousOutput()
    {
        var run = new DuplicateRun();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        run.RecordDuplicate([first], [], (0, 0));
        run.RecordDuplicate([second], [], (75, 0));

        Assert.Equal((80, 5), run.OffsetFor(new HashSet<Guid> { second }, NoEdges, (155, 5)));
    }

    [Fact]
    public void ACopyThatNoLongerExistsEndsTheRun()
    {
        var run = new DuplicateRun();
        var copy = Guid.NewGuid();
        run.RecordDuplicate([copy], [], (0, 0));

        run.BoardChanged(_ => false);

        Assert.Equal((20, 20), run.OffsetFor(new HashSet<Guid> { copy }, NoEdges, (90, 0)));
    }

    [Fact]
    public void ABoardChangeThatKeepsEveryCopyKeepsTheRun()
    {
        var run = new DuplicateRun();
        var copy = Guid.NewGuid();
        var edge = Guid.NewGuid();
        run.RecordDuplicate([copy], [edge], (0, 0));

        run.BoardChanged(_ => true);

        Assert.Equal(
            (90, 0),
            run.OffsetFor(new HashSet<Guid> { copy }, new HashSet<Guid> { edge }, (90, 0))
        );
    }
}
