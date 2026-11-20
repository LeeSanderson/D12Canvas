namespace D12Canvas;

internal static class ContextMenuPlacement
{
    public const double Width = 224;
    public const double RowHeight = 28;
    public const double SeparatorHeight = 9;
    public const double Frame = 10;

    public static double HeightOf(IReadOnlyList<IReadOnlyList<ContextMenuRow>> sections) =>
        Frame
        + sections.Sum(section =>
            section.Count(row => !row.InStrip) + (section.Any(row => row.InStrip) ? 1 : 0)
        ) * RowHeight
        + Math.Max(0, sections.Count - 1) * SeparatorHeight;

    public static MenuBox Fit(
        double anchorX,
        double anchorY,
        double height,
        double containerWidth,
        double containerHeight
    )
    {
        var width = containerWidth > 0 ? Math.Min(Width, containerWidth) : Width;
        var shownHeight = containerHeight > 0 ? Math.Min(height, containerHeight) : height;
        return new MenuBox(
            PlaceAlong(anchorX, width, containerWidth),
            PlaceAlong(anchorY, shownHeight, containerHeight),
            width < Width ? width : null,
            shownHeight < height ? shownHeight : null
        );
    }

    public readonly record struct MenuBox(
        double Left,
        double Top,
        double? Width,
        double? MaxHeight
    );

    private static double PlaceAlong(double anchor, double size, double extent)
    {
        if (extent <= 0 || anchor + size <= extent)
        {
            return anchor;
        }

        if (anchor - size >= 0)
        {
            return anchor - size;
        }

        return Math.Max(0, extent - size);
    }
}
