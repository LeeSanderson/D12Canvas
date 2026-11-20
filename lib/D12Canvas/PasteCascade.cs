namespace D12Canvas;

internal sealed class PasteCascade
{
    public const double Step = 20;

    private (double X, double Y)? _lastSpot;
    private int _repeats;

    public double OffsetFor((double X, double Y) spot)
    {
        if (_lastSpot == spot)
        {
            _repeats++;
        }
        else
        {
            _lastSpot = spot;
            _repeats = 0;
        }

        return _repeats * Step;
    }
}
