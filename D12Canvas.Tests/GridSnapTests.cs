using Xunit;

namespace D12Canvas.Tests;

public class GridSnapTests
{
    [Theory]
    [InlineData(38, 20, 40)]
    [InlineData(29, 20, 20)]
    [InlineData(-31, 20, -40)]
    public void RoundsToTheNearestLine(double coordinate, double spacing, double expected) =>
        Assert.Equal(expected, GridSnap.NearestLine(coordinate, spacing));

    [Fact]
    public void AHalfStepBelowZeroRoundsToPositiveZero() =>
        Assert.False(double.IsNegative(GridSnap.NearestLine(-10, 20)));
}
