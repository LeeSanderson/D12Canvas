using Bunit;
using D12Canvas.Model;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace D12Canvas.Tests;

// Board extent and zoom range are unbounded by default - a host opts into a ceiling and/or floor
// via MinZoom/MaxZoom, which the canvas honours through the same mouse-wheel path every other
// zoom test already drives.
public class DiagramCanvasZoomLimitsTests : ComponentTestBase
{
    public DiagramCanvasZoomLimitsTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();
    }

    private static void ZoomIn(IRenderedComponent<DiagramCanvas> canvas, int times)
    {
        for (var i = 0; i < times; i++)
        {
            canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 });
        }
    }

    private static void ZoomOut(IRenderedComponent<DiagramCanvas> canvas, int times)
    {
        for (var i = 0; i < times; i++)
        {
            canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = 100 });
        }
    }

    [Fact]
    public void WithNoMinOrMaxZoomSetZoomingCanGoWellPastTheOldFixedRange()
    {
        var canvas = Render<DiagramCanvas>();

        ZoomIn(canvas, 100); // old fixed ceiling was 6.0x

        Assert.Equal(11.0, canvas.Instance.ZoomPanTracker.Scale, precision: 10);
    }

    [Fact]
    public void MaxZoomParameterCapsHowFarTheCanvasZoomsIn()
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.MaxZoom, 2.0));

        ZoomIn(canvas, 100);

        Assert.Equal(2.0, canvas.Instance.ZoomPanTracker.Scale);
    }

    [Fact]
    public void MinZoomParameterCapsHowFarTheCanvasZoomsOut()
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.MinZoom, 0.5));

        ZoomOut(canvas, 100);

        Assert.Equal(0.5, canvas.Instance.ZoomPanTracker.Scale);
    }

    [Fact]
    public void ChangingMaxZoomAtRuntimeReclampsAnAlreadyExceedingScale()
    {
        var canvas = Render<DiagramCanvas>();
        ZoomIn(canvas, 50); // scale -> 6.0, well past the old fixed ceiling

        canvas.Render(parameters => parameters.Add(p => p.MaxZoom, 3.0));

        Assert.Equal(3.0, canvas.Instance.ZoomPanTracker.Scale);
    }
}
