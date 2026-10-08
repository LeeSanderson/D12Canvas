namespace D12Canvas;

// The menu's size is fixed by its stylesheet rather than measured, so it can be placed on the
// render that opens it: a fixed width, a fixed row height, a fixed separator, and the root's
// padding plus border. These numbers and the menu's style block describe the same box.
internal static class ContextMenuPlacement
{
    public const double Width = 224;
    public const double RowHeight = 28;
    public const double SeparatorHeight = 9;
    public const double Frame = 10;

    public static double HeightOf(IReadOnlyList<IReadOnlyList<ContextMenuRow>> sections) =>
        Frame
        + sections.Sum(section =>
            section.Count(row => !row.Glyph) + (section.Any(row => row.Glyph) ? 1 : 0)
        ) * RowHeight
        + Math.Max(0, sections.Count - 1) * SeparatorHeight;

    // Opens right and down from the anchor by default, and on each axis flips to the other side
    // of the anchor when that is the side it fits on, so the anchor is never covered. Only when it
    // fits on neither side is it clamped inside the container. An unmeasured container places it
    // at the anchor.
    public static (double Left, double Top) Place(
        double anchorX,
        double anchorY,
        double height,
        double containerWidth,
        double containerHeight
    ) => (PlaceAlong(anchorX, Width, containerWidth), PlaceAlong(anchorY, height, containerHeight));

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
