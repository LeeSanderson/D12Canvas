using D12Canvas.Model;

namespace D12Canvas.Pointer;

// A plain primary drag on empty canvas draws the selection band. Every tick replaces the
// selection with what the band intersects inside the entered group, resolved outward to the
// entered group's direct members, or to top-level groups when none is entered; with Shift held at
// press the band's contents are unioned into the press-time selection instead, so a selection can
// be collected across several sweeps. With no group entered an edge comes along by closure rather
// than by geometry: it is taken when each of its ends is attached to a component the band swept,
// a swept group's members included, or floats inside the band. Inside an entered group the
// selection holds members only, so no edge is taken. A release below the threshold is the click
// on empty canvas: it clears the selection and nothing else.
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
            .Where(instance => instance.Bounds.Intersects(band) && Context.IsInScope(instance.Id))
            .Select(instance => Context.EffectiveSelectionId(instance.Id))
            .ToHashSet();
        var closed = (Context.HasEnteredGroup ? [] : board?.Edges ?? [])
            .Where(edge =>
                IsClosedOver(board!, edge.Source, swept, band)
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
            _ => EndpointAttachment.ComponentIdOf(endpoint) is { } componentId
                && board.GetComponent(componentId) is not null
                && sweptIds.Contains(Context.EffectiveSelectionId(componentId)),
        };

    private static Bounds BandBetween((double X, double Y) a, (double X, double Y) b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}
