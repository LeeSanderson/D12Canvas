namespace D12Canvas;

// The selection's top edge in container pixels: where it starts, how wide it is and its top.
public readonly record struct PropertyBarAnchor(double Left, double Top, double Width);

// Centred above the selection's top and clamped inside the container. With no room above, the bar
// slides along the container's edge over the selection rather than flipping below it, so it never
// changes which side of the selection the user is reaching for.
internal static class PropertyBarPlacement
{
    public const double Gap = 8;
    public const double Margin = 8;

    // A 26px cell inside 3px of padding and a 1px border, mirrored by the bar's stylesheet.
    public const double Height = 34;

    public static (double Left, double Top) Place(
        PropertyBarAnchor anchor,
        double barWidth,
        double containerWidth,
        double containerHeight
    ) =>
        (
            Clamp(anchor.Left + anchor.Width / 2 - barWidth / 2, containerWidth - barWidth),
            Clamp(anchor.Top - Gap - Height, containerHeight - Height)
        );

    private static double Clamp(double position, double room) =>
        Math.Max(Margin, Math.Min(position, room - Margin));
}
