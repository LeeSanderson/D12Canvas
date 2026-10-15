using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

// One side of an instance, in screen pixels: the run between the two corner reserves, a span for
// each port nearest it and capped at the port target, resize in what is left, and nothing below
// the floor.
public class BorderPartitionTests
{
    private static readonly IReadOnlyList<(Guid Id, double Along)> NoCustomPorts = [];

    private static (double Start, double End, BorderSpanKind Kind, Guid? CustomPortId)[] Layout(
        double length,
        IReadOnlyList<(Guid Id, double Along)> customPorts
    ) =>
        BorderPartition
            .OfSide(PortId.Top, length, customPorts)
            .Select(span =>
                (
                    Math.Round(span.Start.ScreenAt(length), 6),
                    Math.Round(span.End.ScreenAt(length), 6),
                    span.Kind,
                    span.CustomPortId
                )
            )
            .ToArray();

    [Fact]
    public void ALongSideHoldsTheStandardPortBetweenTwoResizeSpans()
    {
        Assert.Equal(
            [
                (24, 88, BorderSpanKind.Resize, null),
                (88, 112, BorderSpanKind.Port, null),
                (112, 176, BorderSpanKind.Resize, null),
            ],
            Layout(200, NoCustomPorts)
        );
    }

    [Fact]
    public void ASideWhoseRunHoldsOneSpanIsAllPort()
    {
        Assert.Equal([(24, 48, BorderSpanKind.Port, null)], Layout(72, NoCustomPorts));
    }

    [Fact]
    public void AResizeSpanBelowTheFloorIsLeftOut()
    {
        Assert.Equal([(33, 57, BorderSpanKind.Port, null)], Layout(90, NoCustomPorts));
    }

    [Fact]
    public void ARunBelowTheFloorHoldsNothing()
    {
        Assert.Empty(Layout(59, NoCustomPorts));
        Assert.Equal([(24, 36, BorderSpanKind.Port, null)], Layout(60, NoCustomPorts));
    }

    [Fact]
    public void TwoPortsCloserThanThePortTargetSplitAtTheirMidpoint()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var spans = Layout(400, [(first, 0.25), (second, 0.3)]);

        Assert.Contains((88, 110, BorderSpanKind.Port, first), spans);
        Assert.Contains((110, 132, BorderSpanKind.Port, second), spans);
    }

    [Fact]
    public void AStandardPortSqueezedBelowTheFloorKeepsItsFullWidthAndClipsTheCustomPorts()
    {
        var before = Guid.NewGuid();
        var after = Guid.NewGuid();

        Assert.Equal(
            [
                (24, 88, BorderSpanKind.Resize, null),
                (88, 112, BorderSpanKind.Port, null),
                (112, 176, BorderSpanKind.Resize, null),
            ],
            Layout(200, [(before, 0.48), (after, 0.52)])
        );
    }

    [Fact]
    public void ACustomPortInsideACornerReserveHasNoSpan()
    {
        var nearCorner = Guid.NewGuid();

        Assert.DoesNotContain(
            Layout(200, [(nearCorner, 0.02)]),
            span => span.CustomPortId == nearCorner
        );
    }

    [Fact]
    public void EachCustomPortIsPartitionedOnTheSideItLiesOn()
    {
        var onRight = new PortDef(1, 0.25);
        var onBottom = new PortDef(0.75, 1);
        var inside = new PortDef(0.5, 0.5);

        var spans = BorderPartition.Of(200, 200, 1, [onRight, onBottom, inside]);

        Assert.Equal(
            PortId.Right,
            Assert.Single(spans, span => span.CustomPortId == onRight.Id).Side
        );
        Assert.Equal(
            PortId.Bottom,
            Assert.Single(spans, span => span.CustomPortId == onBottom.Id).Side
        );
        Assert.DoesNotContain(spans, span => span.CustomPortId == inside.Id);
    }

    [Fact]
    public void SpansAreMeasuredInScreenPixelsSoZoomingOutDropsResizeFirst()
    {
        var atFullSize = BorderPartition.Of(200, 200, 1, []);
        var zoomedOut = BorderPartition.Of(200, 200, 0.45, []);

        Assert.Equal(12, atFullSize.Count);
        Assert.All(zoomedOut, span => Assert.Equal(BorderSpanKind.Port, span.Kind));
        Assert.Equal(4, zoomedOut.Count);
    }

    public static TheoryData<int> Seeds => new() { 1, 2, 3, 4, 5, 6, 7, 8 };

    // The WCAG spacing exception as a property: over side lengths from nothing to large and any
    // crowding of custom ports, every surviving region on the border is at least the floor, lies
    // inside the run, and touches no other region, the four corner targets included.
    [Theory]
    [MemberData(nameof(Seeds))]
    public void NoTwoRegionsOnABorderIntersectAndNoneIsBelowTheFloor(int seed)
    {
        var random = new Random(seed);
        for (var trial = 0; trial < 200; trial++)
        {
            var width = random.NextDouble() * 400;
            var height = random.NextDouble() * 400;
            var scale = 0.05 + random.NextDouble() * 4;
            var ports = Enumerable
                .Range(0, random.Next(0, 9))
                .Select(_ => RandomBorderPort(random))
                .ToList();

            var spans = BorderPartition.Of(width, height, scale, ports);

            var screenWidth = width * scale;
            var screenHeight = height * scale;
            foreach (var span in spans)
            {
                var length = span.Side is PortId.Top or PortId.Bottom ? screenWidth : screenHeight;
                var start = span.Start.ScreenAt(length);
                var end = span.End.ScreenAt(length);
                Assert.True(end - start >= ScreenPixels.AffordanceFloor - 1e-9);
                Assert.True(start >= ScreenPixels.CornerReserve - 1e-9);
                Assert.True(end <= length - ScreenPixels.CornerReserve + 1e-9);
            }

            var regions = spans
                .Select(span => RegionOf(span, screenWidth, screenHeight))
                .Concat(CornerRegions(screenWidth, screenHeight))
                .ToList();
            for (var i = 0; i < regions.Count; i++)
            {
                for (var j = i + 1; j < regions.Count; j++)
                {
                    Assert.False(
                        Overlap(regions[i], regions[j]),
                        $"Regions {regions[i]} and {regions[j]} intersect at {width}x{height}, scale {scale}."
                    );
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void TheStandardPortSurvivesAnyCrowdingOnASideWhoseRunHoldsAPortTarget(int seed)
    {
        var random = new Random(seed);
        for (var trial = 0; trial < 200; trial++)
        {
            var length = 3 * ScreenPixels.PortTarget + random.NextDouble() * 400;
            var crowd = Enumerable
                .Range(0, random.Next(1, 9))
                .Select(_ => (Guid.NewGuid(), 0.4 + random.NextDouble() * 0.2))
                .ToList();

            var standard = Assert.Single(
                BorderPartition.OfSide(PortId.Left, length, crowd),
                span => span.Kind == BorderSpanKind.Port && span.CustomPortId is null
            );

            Assert.InRange(
                standard.End.ScreenAt(length) - standard.Start.ScreenAt(length),
                ScreenPixels.AffordanceFloor - 1e-9,
                ScreenPixels.PortTarget + 1e-9
            );
        }
    }

    private static PortDef RandomBorderPort(Random random)
    {
        var along = random.NextDouble();
        return random.Next(4) switch
        {
            0 => new PortDef(along, 0),
            1 => new PortDef(1, along),
            2 => new PortDef(along, 1),
            _ => new PortDef(0, along),
        };
    }

    private readonly record struct Region(double Left, double Top, double Right, double Bottom);

    private static bool Overlap(Region a, Region b) =>
        a.Left < b.Right - 1e-9
        && b.Left < a.Right - 1e-9
        && a.Top < b.Bottom - 1e-9
        && b.Top < a.Bottom - 1e-9;

    // A span reaches half a port target outward, and inward no further than halfway across the
    // instance, as the span's style does, so opposite sides of a thin instance never meet.
    private static Region RegionOf(BorderSpan span, double width, double height)
    {
        var half = ScreenPixels.PortTarget / 2;
        var length = span.Side is PortId.Top or PortId.Bottom ? width : height;
        var inward = Math.Min(
            half,
            (span.Side is PortId.Top or PortId.Bottom ? height : width) / 2
        );
        var start = span.Start.ScreenAt(length);
        var end = span.End.ScreenAt(length);
        return span.Side switch
        {
            PortId.Top => new Region(start, -half, end, inward),
            PortId.Bottom => new Region(start, height - inward, end, height + half),
            PortId.Left => new Region(-half, start, inward, end),
            _ => new Region(width - inward, start, width + half, end),
        };
    }

    // A corner target is the port target square, narrowed on a side too short to hold two of them,
    // as the corner handle's style does.
    private static IEnumerable<Region> CornerRegions(double width, double height)
    {
        var halfWidth = Math.Min(ScreenPixels.CornerTarget, width / 2) / 2;
        var halfHeight = Math.Min(ScreenPixels.CornerTarget, height / 2) / 2;
        foreach (var (x, y) in new[] { (0.0, 0.0), (width, 0.0), (width, height), (0.0, height) })
        {
            yield return new Region(x - halfWidth, y - halfHeight, x + halfWidth, y + halfHeight);
        }
    }
}
