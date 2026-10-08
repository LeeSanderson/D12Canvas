namespace D12Canvas;

// Repeated pastes landing on one spot would stack exactly on top of each other, so each repeat
// steps one cascade step further down and right; any other spot starts again from no offset.
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
