namespace D12Canvas.Model;

public readonly record struct Bounds(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;

    public Bounds ExpandedBy(double margin) =>
        new(X - margin, Y - margin, Width + margin * 2, Height + margin * 2);

    public bool Intersects(Bounds other) =>
        X <= other.Right && Right >= other.X && Y <= other.Bottom && Bottom >= other.Y;

    // Ticket 44: the smallest Bounds enclosing every given Bounds, or null for an empty sequence -
    // shared by Board.GetBounds(Group) and DiagramCanvas's own selection bounding box, so both
    // "union a set of Bounds into one" computations share a single implementation.
    public static Bounds? Union(IEnumerable<Bounds> bounds)
    {
        double minX = 0,
            minY = 0,
            maxX = 0,
            maxY = 0;
        var any = false;

        foreach (var b in bounds)
        {
            minX = any ? Math.Min(minX, b.X) : b.X;
            minY = any ? Math.Min(minY, b.Y) : b.Y;
            maxX = any ? Math.Max(maxX, b.Right) : b.Right;
            maxY = any ? Math.Max(maxY, b.Bottom) : b.Bottom;
            any = true;
        }

        return any ? new Bounds(minX, minY, maxX - minX, maxY - minY) : null;
    }
}
