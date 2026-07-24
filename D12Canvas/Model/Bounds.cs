namespace D12Canvas.Model;

public readonly record struct Bounds(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;

    public Bounds ExpandedBy(double margin) =>
        new(X - margin, Y - margin, Width + margin * 2, Height + margin * 2);

    public bool Intersects(Bounds other) =>
        X <= other.Right && Right >= other.X && Y <= other.Bottom && Bottom >= other.Y;
}
