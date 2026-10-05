using D12Canvas.Model;

namespace D12Canvas.Pointer;

// A primary press on a port, a port strip or a floating endpoint carries one end of an edge. A
// bare port, or the side a strip runs along, pulls a new edge from that port; a port that already
// anchors an edge, or a floating endpoint, carries that edge's end. Each tick publishes one
// pending line from the end that stays put to the pointer. The release resolves what lies under
// the pointer, topmost first: a port pins the end to it, a shape's body or nothing at all leaves
// it floating at the release point, and chrome or another edge in between is looked through.
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
                Context.ToBoardPoint(move.X, move.Y)
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
        if (dropped.Equals(_anchor))
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
            Press.Role == HitRole.PortStrip
            && Press.PressCount > 1
            && Press.EntityId is { } entityId
            && Context.Board?.GetComponent(entityId) is { } instance
            && SideOf(Press.Part) is { } side
        )
        {
            Context.AddCustomPort(
                entityId,
                PortAlong(instance.Bounds, side, Context.ToBoardPoint(Press.X, Press.Y))
            );
        }
    }

    private IEdgeEndpoint DroppedEndpoint(PointerRelease release)
    {
        var topmost = (release.Hits ?? []).FirstOrDefault(hit =>
            hit.Role is HitRole.Port or HitRole.Instance or HitRole.AuthorContent
        );
        if (
            topmost is { Role: HitRole.Port, EntityId: { } entityId }
            && PortEndpoint(topmost.Role, entityId, topmost.Part) is { } port
        )
        {
            return port;
        }

        var (x, y) = Context.ToBoardPoint(release.X, release.Y);
        return new FloatingEndpoint(x, y);
    }

    // A port marker names a standard port by its PortId and a custom port by its id; a port strip
    // names the side it runs along, which pulls from that side's standard port.
    private static IEdgeEndpoint? PortEndpoint(string role, Guid instanceId, string? part)
    {
        if (role == HitRole.PortStrip)
        {
            return SideOf(part) is { } side ? new PortEndpoint(instanceId, side) : null;
        }

        if (Enum.TryParse<PortId>(part, out var standard))
        {
            return new PortEndpoint(instanceId, standard);
        }

        return Guid.TryParse(part, out var custom)
            ? new CustomPortEndpoint(instanceId, custom)
            : null;
    }

    private static PortId? SideOf(string? part) =>
        Enum.TryParse<PortId>(part, ignoreCase: true, out var side) ? side : null;

    private static PortDef PortAlong(Bounds bounds, PortId side, (double X, double Y) point)
    {
        var alongX = Math.Clamp((point.X - bounds.X) / bounds.Width, 0, 1);
        var alongY = Math.Clamp((point.Y - bounds.Y) / bounds.Height, 0, 1);
        return side switch
        {
            PortId.Top => new PortDef(alongX, 0),
            PortId.Right => new PortDef(1, alongY),
            PortId.Bottom => new PortDef(alongX, 1),
            _ => new PortDef(0, alongY),
        };
    }
}
