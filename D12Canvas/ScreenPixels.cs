namespace D12Canvas;

// Distances stated in screen pixels because each says how precisely a hand aims a pointer, not
// anything about the board, so a gesture divides them by scale before comparing board units. Their
// order matters more than their values: a press must count as a drag before it can snap, and a
// snap must let go before an edge's hit band would claim the pointer.
internal static class ScreenPixels
{
    public const double DragThreshold = 4;
    public const double ObjectSnapTolerance = 8;
    public const double EdgeHitBand = 20;
}
