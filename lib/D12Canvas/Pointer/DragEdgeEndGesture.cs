using D12Canvas.Model;

namespace D12Canvas.Pointer;

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
            if (board.GetEdge(entityId) is { Locked: false } edge)
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
            && board.GetEdge(attached.EdgeId) is { Locked: false } anchored
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

    protected override void OnClick(PointerRelease release)
    {
        if (
            Press.EntityId is { } entityId
            && PortEndpoint(Press.Role, entityId, Press.Part) is { } port
        )
        {
            Context.QuickCreate(port);
        }
    }

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
            && Context.Board?.GetComponent(entityId) is { Locked: false }
        );

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
