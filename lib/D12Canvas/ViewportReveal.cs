using D12Canvas.Model;

namespace D12Canvas;

internal static class ViewportReveal
{
    public static (double X, double Y) ShiftToReveal(Bounds viewport, Bounds target) =>
        (
            Shift(viewport.X, viewport.Right, target.X, target.Right),
            Shift(viewport.Y, viewport.Bottom, target.Y, target.Bottom)
        );

    private static double Shift(double viewStart, double viewEnd, double start, double end)
    {
        if (start < viewStart || end - start > viewEnd - viewStart)
        {
            return start - viewStart;
        }

        return end > viewEnd ? end - viewEnd : 0;
    }
}
