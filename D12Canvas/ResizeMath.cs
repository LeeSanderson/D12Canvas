using D12Canvas.Model;

namespace D12Canvas;

// The anchor/clamp math behind a handle-drag resize: shared by ComponentContainer's own
// per-instance resize (ticket 31) and DiagramCanvas's group resize (ticket 33), which apply the
// same 8-direction logic to two different things (one instance's bounds vs. a selection's combined
// bounding box).
internal static class ResizeMath
{
    // The floor a single component instance's own handle-drag resize has always enforced (ticket
    // 31). Shared here so DiagramCanvas's group resize can derive its own bounding-box floor from
    // the same value instead of encoding it a second time.
    public const double DefaultMinWidth = 50;
    public const double DefaultMinHeight = 50;

    public static Bounds Apply(
        Bounds start,
        ResizeDirection direction,
        double deltaX,
        double deltaY,
        double minWidth,
        double minHeight
    )
    {
        switch (direction)
        {
            case ResizeDirection.TopLeft:
                double widthTL = Math.Max(start.Width - deltaX, minWidth);
                double heightTL = Math.Max(start.Height - deltaY, minHeight);
                return new Bounds(
                    start.X + (start.Width - widthTL),
                    start.Y + (start.Height - heightTL),
                    widthTL,
                    heightTL
                );

            case ResizeDirection.Top:
                double heightT = Math.Max(start.Height - deltaY, minHeight);
                return new Bounds(
                    start.X,
                    start.Y + (start.Height - heightT),
                    start.Width,
                    heightT
                );

            case ResizeDirection.TopRight:
                double widthTR = Math.Max(start.Width + deltaX, minWidth);
                double heightTR = Math.Max(start.Height - deltaY, minHeight);
                return new Bounds(start.X, start.Y + (start.Height - heightTR), widthTR, heightTR);

            case ResizeDirection.Right:
                return start with { Width = Math.Max(start.Width + deltaX, minWidth) };

            case ResizeDirection.BottomRight:
                return start with
                {
                    Width = Math.Max(start.Width + deltaX, minWidth),
                    Height = Math.Max(start.Height + deltaY, minHeight),
                };

            case ResizeDirection.Bottom:
                return start with { Height = Math.Max(start.Height + deltaY, minHeight) };

            case ResizeDirection.BottomLeft:
                double widthBL = Math.Max(start.Width - deltaX, minWidth);
                return new Bounds(
                    start.X + (start.Width - widthBL),
                    start.Y,
                    widthBL,
                    Math.Max(start.Height + deltaY, minHeight)
                );

            case ResizeDirection.Left:
                double widthL = Math.Max(start.Width - deltaX, minWidth);
                return new Bounds(start.X + (start.Width - widthL), start.Y, widthL, start.Height);

            default:
                return start;
        }
    }
}
