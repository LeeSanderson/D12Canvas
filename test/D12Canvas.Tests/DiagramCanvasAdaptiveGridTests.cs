using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasAdaptiveGridTests : ComponentTestBase
{
    public DiagramCanvasAdaptiveGridTests()
    {
        SetupDiagramCanvasJsModule();
    }

    private static void ZoomIn(IRenderedComponent<DiagramCanvas> canvas, int times)
    {
        for (var i = 0; i < times; i++)
        {
            canvas.ZoomIn();
        }
    }

    private static void ZoomOut(IRenderedComponent<DiagramCanvas> canvas, int times)
    {
        for (var i = 0; i < times; i++)
        {
            canvas.ZoomOut();
        }
    }

    private static double ExtractOpacity(string style) =>
        double.Parse(
            Regex.Match(style, @"opacity: ([\d.Ee+-]+);").Groups[1].Value,
            CultureInfo.InvariantCulture
        );

    private static double ExtractBackgroundSize(string style) =>
        double.Parse(
            Regex.Match(style, @"background-size: ([\d.Ee+-]+)px").Groups[1].Value,
            CultureInfo.InvariantCulture
        );

    [Fact]
    public void DefaultZoomRendersASingleFullyOpaqueLayerMatchingTheLegacyGrid()
    {
        var canvas = Render<DiagramCanvas>();

        var layer = Assert.Single(canvas.FindAll(".grid-layer"));
        var style = layer.GetAttribute("style")!;

        Assert.Equal(20, ExtractBackgroundSize(style), precision: 6);
        Assert.Equal(1.0, ExtractOpacity(style), precision: 6);
        Assert.Contains("background-position: 0px 0px", style);
    }

    [Fact]
    public void ZoomingOutIntoATransitionZoneRendersTwoCrossfadingLayersTenTimesApart()
    {
        var canvas = Render<DiagramCanvas>();

        ZoomOut(canvas, 5);

        var layers = canvas.FindAll(".grid-layer");
        Assert.Equal(2, layers.Count);

        var sizes = layers
            .Select(l => ExtractBackgroundSize(l.GetAttribute("style")!))
            .OrderBy(s => s)
            .ToList();
        Assert.Equal(10, sizes[0], precision: 3);
        Assert.Equal(100, sizes[1], precision: 3);

        var opacities = layers.Select(l => ExtractOpacity(l.GetAttribute("style")!)).ToList();
        Assert.Equal(1.0, opacities.Sum(), precision: 6);
        Assert.All(opacities, o => Assert.InRange(o, 0.01, 0.99));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(12)]
    [InlineData(15)]
    public void RenderedLayerOpacitiesAlwaysSumToOneNoMatterHowFarZoomedOut(int zoomOutSteps)
    {
        var canvas = Render<DiagramCanvas>();

        ZoomOut(canvas, zoomOutSteps);

        var total = canvas
            .FindAll(".grid-layer")
            .Select(l => ExtractOpacity(l.GetAttribute("style")!))
            .Sum();

        Assert.Equal(1.0, total, precision: 6);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(20)]
    [InlineData(90)]
    public void RenderedLayerOpacitiesAlwaysSumToOneNoMatterHowFarZoomedIn(int zoomInSteps)
    {
        var canvas = Render<DiagramCanvas>();

        ZoomIn(canvas, zoomInSteps);

        var total = canvas
            .FindAll(".grid-layer")
            .Select(l => ExtractOpacity(l.GetAttribute("style")!))
            .Sum();

        Assert.Equal(1.0, total, precision: 6);
    }

    [Fact]
    public async Task PanningShiftsTheGridPhaseWithoutChangingItsSpacing()
    {
        var canvas = Render<DiagramCanvas>();

        await canvas.Pan(from: (0, 0), to: (15, -5));

        var layer = Assert.Single(canvas.FindAll(".grid-layer"));
        var style = layer.GetAttribute("style")!;

        Assert.Equal(20, ExtractBackgroundSize(style), precision: 6);
        Assert.Contains("background-position: 15px 15px", style);
    }

    [Fact]
    public void ExtremeZoomOutSettlesOnASingleLayerAtTheNumericalStabilityFloor()
    {
        var canvas = Render<DiagramCanvas>();

        ZoomOut(canvas, 30);

        var layer = Assert.Single(canvas.FindAll(".grid-layer"));
        var style = layer.GetAttribute("style")!;

        Assert.Equal(20, ExtractBackgroundSize(style), precision: 3);
        Assert.Equal(1.0, ExtractOpacity(style), precision: 6);
    }

    [Fact]
    public void ExtremeZoomInStaysNumericallyStable()
    {
        var canvas = Render<DiagramCanvas>();

        ZoomIn(canvas, 200);

        var styles = canvas.FindAll(".grid-layer").Select(l => l.GetAttribute("style")!).ToList();

        Assert.NotEmpty(styles);
        Assert.All(
            styles,
            style =>
            {
                Assert.DoesNotContain("NaN", style);
                Assert.DoesNotContain("Infinity", style);
                Assert.True(ExtractBackgroundSize(style) > 0);
            }
        );
    }
}
