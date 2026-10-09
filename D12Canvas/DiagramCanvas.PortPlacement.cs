using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Pointer;

namespace D12Canvas;

// Two routes add a custom port. The pointer's Add port here row adds one where the menu's press
// landed on a border span. The keyboard's Add port… row starts placement: a provisional port that
// only the arrows, Enter and Escape act on, held here and never written to the board until Enter.
// Placement ends when focus leaves the instance's stop, so it is only ever live on the focused one.
public partial class DiagramCanvas
{
    private sealed record PortPlacementState(Guid InstanceId, ProvisionalPort Port);

    private sealed record PortAtPress(Guid InstanceId, PortDef Port);

    private PortPlacementState? _portPlacement;

    private bool PlacingPort => _portPlacement is not null;

    private ComponentInstance? SinglePortTarget() =>
        _selectedInstanceIds.Count == 1
        && _selectedEdgeIds.Count == 0
        && Board?.GetComponent(_selectedInstanceIds.Single()) is { Locked: false } instance
            ? instance
            : null;

    private (double FractionX, double FractionY)? ProvisionalPortFor(Guid instanceId) =>
        _portPlacement is { } placement && placement.InstanceId == instanceId
            ? placement.Port.Fractions
            : null;

    private PortAtPress? PortAtBorderPress(PointerPress press)
    {
        if (
            press.EntityId is not { } instanceId
            || SinglePortTarget() is not { } instance
            || instance.Id != instanceId
            || PressedSide(press, instance) is not { } side
        )
        {
            return null;
        }

        var (x, y) = ToBoardPoint((press.X, press.Y), (0, 0));
        var bounds = instance.Bounds;
        var along = side is PortId.Top or PortId.Bottom
            ? PortPlacement.AlongAt(x, bounds.X, bounds.Width)
            : PortPlacement.AlongAt(y, bounds.Y, bounds.Height);
        var (fractionX, fractionY) = new ProvisionalPort(side, along).Fractions;
        return new PortAtPress(instance.Id, new PortDef(fractionX, fractionY));
    }

    private static PortId? PressedSide(PointerPress press, ComponentInstance instance) =>
        press.Role switch
        {
            HitRole.ResizeHandle => press.Part switch
            {
                "top" => PortId.Top,
                "right" => PortId.Right,
                "bottom" => PortId.Bottom,
                "left" => PortId.Left,
                _ => null,
            },
            HitRole.Port when Enum.TryParse<PortId>(press.Part, out var standard) => standard,
            HitRole.Port when Guid.TryParse(press.Part, out var customId) => instance
                .CustomPorts.Where(port => port.Id == customId)
                .Select(BorderPartition.SideOf)
                .FirstOrDefault(),
            _ => null,
        };

    private void AddPortAtPress(PortAtPress portAtPress)
    {
        if (
            Board?.GetComponent(portAtPress.InstanceId) is { Locked: false } instance
            && !PressOwnsBoard
        )
        {
            _history.Do(new AddCustomPortCommand(instance, portAtPress.Port));
        }

        StateHasChanged();
    }

    // Focus goes to the instance's stop, since a menu opened with focus on the canvas would
    // otherwise leave the keys that place the port somewhere Enter does not reach.
    private void BeginPortPlacement()
    {
        if (Board is null || PressOwnsBoard || SinglePortTarget() is not { } instance)
        {
            return;
        }

        _portFocusInstanceId = null;
        _portPlacement = new PortPlacementState(
            instance.Id,
            PortPlacement.Start(instance.Bounds, SnapSpacing)
        );
        _pendingFocusId = instance.Id;
        StateHasChanged();
    }

    private void MovePortPlacement(PortPlacementState placement, string code, bool coarse)
    {
        var (directionX, directionY) = ArrowDirection(code);
        if (Board?.GetComponent(placement.InstanceId) is not { } instance)
        {
            EndPortPlacement();
            return;
        }

        var moved = PortPlacement.Move(
            placement.Port,
            instance.Bounds,
            (int)directionX,
            (int)directionY,
            (coordinate, direction) => NudgeAlong(coordinate, direction, coarse)
        );
        _portPlacement = placement with { Port = moved };
        StateHasChanged();
    }

    private void CommitPortPlacement(PortPlacementState placement)
    {
        _portPlacement = null;
        if (Board?.GetComponent(placement.InstanceId) is { Locked: false } instance)
        {
            var (fractionX, fractionY) = placement.Port.Fractions;
            _history.Do(new AddCustomPortCommand(instance, new PortDef(fractionX, fractionY)));
        }

        StateHasChanged();
    }

    private void EndPortPlacement()
    {
        _portPlacement = null;
        StateHasChanged();
    }

    // Focus landing anywhere but the instance's stop ends placement. A landing on the canvas ends
    // it only once the stop has held focus, because the menu that started placement hands focus
    // back to wherever it was at the keydown, the canvas included, before focus reaches the stop.
    private void EndPortPlacementOnFocus(Guid? landedOn)
    {
        if (_portPlacement is not { } placement || landedOn == placement.InstanceId)
        {
            return;
        }

        if (landedOn is null && _focusedTabStopId != placement.InstanceId)
        {
            return;
        }

        _portPlacement = null;
    }
}
