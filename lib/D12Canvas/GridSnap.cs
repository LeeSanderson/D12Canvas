namespace D12Canvas;

internal static class GridSnap
{
    public static double NearestLine(double coordinate, double spacing) =>
        Math.Round(coordinate / spacing) * spacing + 0.0;
}
