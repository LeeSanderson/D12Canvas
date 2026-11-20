using D12Canvas.Model;

namespace D12Canvas.Pointer;

internal sealed class MarqueeSelectGesture : PointerGesture
{
    private readonly (double X, double Y) _anchor;

    public MarqueeSelectGesture(PointerPress press, IGestureContext context)
        : base(press, context)
    {
        _anchor = context.ToBoardPoint(press.X, press.Y);
    }

    protected override void OnMove(PointerMove move)
    {
        var current = Context.ToBoardPoint(move.X, move.Y);
        var band = BandBetween(_anchor, current);
        Context.ShowMarquee(band);

        var board = Context.Board;
        var swept = (board?.Components ?? [])
            .Where(instance =>
                instance.Bounds.Intersects(band)
                && Context.HasHitRegion(instance.Id)
                && Context.IsInScope(instance.Id)
            )
            .Select(instance => Context.EffectiveSelectionId(instance.Id))
            .ToHashSet();
        var closed = (Context.HasEnteredGroup ? [] : board?.Edges ?? [])
            .Where(edge =>
                Context.HasHitRegion(edge.Id)
                && IsClosedOver(board!, edge.Source, swept, band)
                && IsClosedOver(board!, edge.Target, swept, band)
            )
            .Select(edge => edge.Id);

        Context.ReplaceSelection(
            Press.ShiftKey ? Context.SelectionSnapshot.InstanceIds.Union(swept) : swept,
            Press.ShiftKey ? Context.SelectionSnapshot.EdgeIds.Union(closed) : closed
        );
    }

    protected override void OnRelease(PointerRelease release) => Context.ShowMarquee(null);

    protected override void OnClick(PointerRelease release) => Context.ClearSelection();

    private bool IsClosedOver(
        Board board,
        IEdgeEndpoint endpoint,
        IReadOnlySet<Guid> sweptIds,
        Bounds band
    ) =>
        endpoint switch
        {
            FloatingEndpoint floating => band.Contains(floating.X, floating.Y),
            _ => endpoint.ComponentId is { } componentId
                && board.GetComponent(componentId) is not null
                && sweptIds.Contains(Context.EffectiveSelectionId(componentId)),
        };

    private static Bounds BandBetween((double X, double Y) a, (double X, double Y) b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
