using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Pointer;

namespace D12Canvas;

public partial class DiagramCanvas
{
    private CustomPortEndpoint? RemovablePortAtPress(PointerPress press) =>
        press.Role == HitRole.Port
        && press.EntityId is { } instanceId
        && SinglePortTarget() is { } instance
        && instance.Id == instanceId
        && Guid.TryParse(press.Part, out var portId)
        && Board is not null
        && CustomPortRemoval.Compose(Board, instance, portId) is not null
            ? new CustomPortEndpoint(instanceId, portId)
            : null;

    private void RemoveCustomPort(CustomPortEndpoint removed)
    {
        if (
            Board?.GetComponent(removed.ComponentId) is not { } instance
            || PressOwnsBoard
            || CustomPortRemoval.Compose(Board, instance, removed.PortId) is not { } removal
        )
        {
            return;
        }

        _history.Do(removal);
        if (_portFocusEndpoint.Equals(removed))
        {
            _portFocusEndpoint = new AutoPortEndpoint(removed.ComponentId);
        }

        if (_pendingConnectorSource is { } source && source.Equals(removed))
        {
            _pendingConnectorSource = null;
        }
    }

    private void DeleteDuringPortPicking()
    {
        if (_portFocusEndpoint is CustomPortEndpoint custom)
        {
            RemoveCustomPort(custom);
        }

        StateHasChanged();
    }
}
