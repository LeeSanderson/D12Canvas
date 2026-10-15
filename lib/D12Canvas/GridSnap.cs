namespace D12Canvas;

internal static class GridSnap
{
    // Math.Round(-0.5) is -0, which would render as "-0px" and serialize as -0, so adding zero
    // normalises it to 0.
    public static double NearestLine(double coordinate, double spacing) =>
        Math.Round(coordinate / spacing) * spacing + 0.0;
}
