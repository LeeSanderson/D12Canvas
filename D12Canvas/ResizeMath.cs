using D12Canvas.Model;

namespace D12Canvas;

// The anchor/clamp math behind a resize: the pointer's ResizeSelection gesture applies it to the
// selection's bounding box, snapping the edges it moves, and the keyboard's Alt+Arrow resize
// applies the unsnapped clamp to a single instance.
internal static class ResizeMath
{
    // The floor every instance keeps under any resize. A multi-selection's box derives its own,
    // larger floor from it.
    public const double DefaultMinWidth = 50;
    public const double DefaultMinHeight = 50;

    private const double GridLineTolerance = 1e-9;

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

    // The edges the direction moves, rounded to the nearest grid line that keeps the size at or
    // above the minimum, so the result always ends on a line. The edges it anchors are left
    // exactly where start had them.
    public static Bounds SnapMovingEdges(
        Bounds resized,
        Bounds start,
        ResizeDirection direction,
        double minWidth,
        double minHeight,
        double spacing
    )
    {
        var (left, top, right, bottom) = (resized.X, resized.Y, resized.Right, resized.Bottom);

        if (
            direction
            is ResizeDirection.Left
                or ResizeDirection.TopLeft
                or ResizeDirection.BottomLeft
        )
        {
            left = Math.Min(
                NearestLine(left, spacing),
                LineAtOrBelow(start.Right - minWidth, spacing)
            );
        }

        if (
            direction
            is ResizeDirection.Right
                or ResizeDirection.TopRight
                or ResizeDirection.BottomRight
        )
        {
            right = Math.Max(
                NearestLine(right, spacing),
                LineAtOrAbove(start.X + minWidth, spacing)
            );
        }

        if (direction is ResizeDirection.Top or ResizeDirection.TopLeft or ResizeDirection.TopRight)
        {
            top = Math.Min(
                NearestLine(top, spacing),
                LineAtOrBelow(start.Bottom - minHeight, spacing)
            );
        }

        if (
            direction
            is ResizeDirection.Bottom
                or ResizeDirection.BottomLeft
                or ResizeDirection.BottomRight
        )
        {
            bottom = Math.Max(
                NearestLine(bottom, spacing),
                LineAtOrAbove(start.Y + minHeight, spacing)
            );
        }

        return new Bounds(left, top, right - left, bottom - top);
    }

    private static double NearestLine(double coordinate, double spacing) =>
        Math.Round(coordinate / spacing) * spacing;

    private static double LineAtOrBelow(double coordinate, double spacing) =>
        Math.Floor(coordinate / spacing + GridLineTolerance) * spacing;

    private static double LineAtOrAbove(double coordinate, double spacing) =>
        Math.Ceiling(coordinate / spacing - GridLineTolerance) * spacing;

    // The smallest a box can shrink to while every member, scaled proportionally inside it, stays
    // at or above the per-instance floor. Width and height scale independently, so each takes the
    // most restrictive member on its own axis.
    public static (double MinWidth, double MinHeight) MinimumBoxSizeFor(
        Bounds box,
        IEnumerable<Bounds> members
    )
    {
        var minWidth = DefaultMinWidth;
        var minHeight = DefaultMinHeight;

        foreach (var member in members)
        {
            if (member.Width > 0)
            {
                minWidth = Math.Max(minWidth, DefaultMinWidth * box.Width / member.Width);
            }

            if (member.Height > 0)
            {
                minHeight = Math.Max(minHeight, DefaultMinHeight * box.Height / member.Height);
            }
        }

        return (minWidth, minHeight);
    }

    // A member's bounds at the start, re-expressed at the same relative position and size inside
    // the box's current extent. Each edge is placed by its fraction of the box, so a member edge
    // that touched the box still touches it exactly.
    public static Bounds ScaleWithinBox(Bounds memberStart, Bounds boxStart, Bounds boxCurrent)
    {
        var (left, right) = ScaleAxis(
            memberStart.X,
            memberStart.Right,
            boxStart.X,
            boxStart.Width,
            boxCurrent.X,
            boxCurrent.Width
        );
        var (top, bottom) = ScaleAxis(
            memberStart.Y,
            memberStart.Bottom,
            boxStart.Y,
            boxStart.Height,
            boxCurrent.Y,
            boxCurrent.Height
        );

        return new Bounds(left, top, right - left, bottom - top);
    }

    private static (double Low, double High) ScaleAxis(
        double low,
        double high,
        double boxStart,
        double boxStartSize,
        double boxCurrent,
        double boxCurrentSize
    )
    {
        if (boxStartSize <= 0)
        {
            return (boxCurrent + low - boxStart, boxCurrent + high - boxStart);
        }

        return (
            boxCurrent + boxCurrentSize * (low - boxStart) / boxStartSize,
            boxCurrent + boxCurrentSize * (high - boxStart) / boxStartSize
        );
    }
}
