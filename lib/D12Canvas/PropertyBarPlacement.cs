namespace D12Canvas;

public readonly record struct PropertyBarAnchor(double Left, double Top, double Width);

internal static class PropertyBarPlacement
{
    public const double Gap = 8;
    public const double Margin = 8;

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
