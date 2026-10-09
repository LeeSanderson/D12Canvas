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

    // A bar already showing stays where it is while its new place is less than this far away and
    // where it stands is still inside the margins, so a nudge or a small viewport change does not
    // make it twitch after the selection.
    public const double MinimumMove = 16;

    public static (double Left, double Top) Settle(
        (double Left, double Top) shown,
        (double Left, double Top) target,
        double barWidth,
        double containerWidth,
        double containerHeight
    )
    {
        var distance = Math.Sqrt(
            Math.Pow(target.Left - shown.Left, 2) + Math.Pow(target.Top - shown.Top, 2)
        );
        var insideMargins =
            shown.Left == Clamp(shown.Left, containerWidth - barWidth)
            && shown.Top == Clamp(shown.Top, containerHeight - Height);
        return distance < MinimumMove && insideMargins ? shown : target;
    }

    private static double Clamp(double position, double room) =>
        Math.Max(Margin, Math.Min(position, room - Margin));
}
