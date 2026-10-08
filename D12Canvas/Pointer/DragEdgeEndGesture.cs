using D12Canvas.Model;

namespace D12Canvas.Pointer;

// A primary press on a port span or a floating endpoint carries one end of an edge. A port span
// pulls a new edge from its port unless an edge end is pinned to that port, which it carries; a
// floating endpoint carries its own end. Each tick publishes one pending line from the end that
// stays put to the pointer, naming the shape under the pointer as the drop target so its ports
// show. The release resolves what lies under the pointer, topmost first: a port pins the end to
// it, a shape's body attaches it as an auto endpoint, nothing at all leaves it floating at the
// release point, and chrome or another edge in between is looked through. A drop on the shape the
// other end is attached to changes nothing.
internal sealed class DragEdgeEndGesture : PointerGesture
{
    private IEdgeEndpoint? _anchor;
    private (Guid EdgeId, bool IsSource)? _carried;

    public DragEdgeEndGesture(PointerPress press, IGestureContext context)
        : base(press, context) { }

    protected override void OnPress()
    {
        if (Context.Board is not { } board || Press.EntityId is not { } entityId)
        {
            return;
        }

        if (Press.Role == HitRole.EdgeEndpoint)
        {
            if (board.GetEdge(entityId) is { } edge)
            {
                var isSource = Press.Part == "source";
                _carried = (edge.Id, isSource);
                _anchor = isSource ? edge.Target : edge.Source;
            }

            return;
        }

        if (PortEndpoint(Press.Role, entityId, Press.Part) is not { } port)
        {
            return;
        }

        if (
            board.FindEdgeAttachedTo(port) is { } attached
            && board.GetEdge(attached.EdgeId) is { } anchored
        )
        {
            _carried = attached;
            _anchor = attached.IsSource ? anchored.Target : anchored.Source;
        }
        else
        {
            _anchor = port;
        }
    }

    protected override void OnMove(PointerMove move)
    {
        if (_anchor is null)
        {
            return;
        }

        Context.PublishPendingEdge(
            new PendingEdge(
                _carried?.EdgeId,
                _carried?.IsSource ?? false,
                _anchor,
                Context.ToBoardPoint(move.X, move.Y),
                DropTargetOf(move.Hits) is { } target && target != _anchor.ComponentId
                    ? target
                    : null
            )
        );
    }

    protected override void OnRelease(PointerRelease release)
    {
        if (_anchor is null)
        {
            return;
        }

        var dropped = DroppedEndpoint(release);
        if (dropped.Equals(_anchor) || SameComponent(dropped, _anchor))
        {
            return;
        }

        if (_carried is not { } carried)
        {
            Context.AddEdge(_anchor, dropped);
            return;
        }

        if (Context.Board?.GetEdge(carried.EdgeId) is not { } edge)
        {
            return;
        }

        var current = carried.IsSource ? edge.Source : edge.Target;
        if (!dropped.Equals(current))
        {
            Context.ChangeEdgeEndpoint(carried.EdgeId, carried.IsSource, dropped);
        }
    }

    protected override void OnClick(PointerRelease release) { }

    private static bool SameComponent(IEdgeEndpoint dropped, IEdgeEndpoint anchor) =>
        dropped.ComponentId is { } componentId && componentId == anchor.ComponentId;

    private IEdgeEndpoint DroppedEndpoint(PointerRelease release)
    {
        if (TopmostComponentHit(release.Hits) is { EntityId: { } componentId } topmost)
        {
            var pinned =
                topmost.Role == HitRole.Port
                    ? PortEndpoint(topmost.Role, componentId, topmost.Part)
                    : null;
            return pinned ?? new AutoPortEndpoint(componentId);
        }

        var (x, y) = Context.ToBoardPoint(release.X, release.Y);
        return new FloatingEndpoint(x, y);
    }

    private Guid? DropTargetOf(IReadOnlyList<PointerHit>? hits) =>
        TopmostComponentHit(hits)?.EntityId;

    private PointerHit? TopmostComponentHit(IReadOnlyList<PointerHit>? hits) =>
        (hits ?? []).FirstOrDefault(hit =>
            (hit.Role is HitRole.Port or HitRole.Instance or HitRole.AuthorContent)
            && hit.EntityId is { } entityId
            && Context.Board?.GetComponent(entityId) is not null
        );

    // A port marker names a standard port by its PortId and a custom port by its id.
    private static IEdgeEndpoint? PortEndpoint(string role, Guid instanceId, string? part)
    {
        if (role != HitRole.Port)
        {
            return null;
        }

        if (Enum.TryParse<PortId>(part, out var standard))
        {
            return new PortEndpoint(instanceId, standard);
        }

        return Guid.TryParse(part, out var custom)
            ? new CustomPortEndpoint(instanceId, custom)
            : null;
    }
}
