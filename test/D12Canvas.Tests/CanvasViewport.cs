using Bunit;

namespace D12Canvas.Tests;

internal static class CanvasViewport
{
    public const double Notch = 100;

    public static void WheelAt(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) at,
        double deltaY,
        double deltaX = 0,
        bool coarse = true,
        bool shift = false,
        bool ctrl = false,
        bool alt = false,
        bool meta = false
    ) =>
        canvas
            .InvokeAsync(
                () =>
                    canvas.Instance.OnWheel(
                        new WheelInput(at.X, at.Y, deltaX, deltaY, coarse, shift, ctrl, alt, meta)
                    )
            )
            .GetAwaiter()
            .GetResult();

    public static void ZoomIn(this IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(canvas.Instance.OnZoomIn).GetAwaiter().GetResult();

    public static void ZoomOut(this IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(canvas.Instance.OnZoomOut).GetAwaiter().GetResult();

    public static IRenderedComponent<DiagramCanvas> ReturnToOrigin(
        this IRenderedComponent<DiagramCanvas> canvas
    )
    {
        canvas
            .InvokeAsync(() =>
            {
                canvas.Instance.ZoomPanTracker.Scale = 1.0;
                canvas.Instance.ZoomPanTracker.SetPanPosition(0, 0);
            })
            .GetAwaiter()
            .GetResult();
        return canvas;
    }
}
