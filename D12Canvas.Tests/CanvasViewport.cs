using Bunit;

namespace D12Canvas.Tests;

// Moves a rendered DiagramCanvas's viewport the ways a user does without a pointer button: the
// wheel entry point the browser-side listener calls, with container-relative pointer coordinates,
// deltas in pixels and the granularity the listener classified the wheel gesture with, and the
// keyboard zoom entry points, which step the scale by a fixed amount about the board origin.
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
}
