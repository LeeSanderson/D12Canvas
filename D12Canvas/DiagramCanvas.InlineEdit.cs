using D12Canvas.Model;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class DiagramCanvas
{
    private readonly record struct PendingEdit(Guid Id, bool IsEdgeLabel);

    // Lasts one render: whatever that render has not mounted as a full component is never
    // edited, so an edit cannot open later when the user happens to scroll it into view.
    private PendingEdit? _pendingEdit;

    // After an edit ends with Escape, focus goes to the edited instance's stop, or the stop of the
    // edge whose label was edited, and to the canvas when there is no such stop on screen.
    private bool _returningFocusAfterEdit;
    private Guid? _editFocusStopId;

    [JSInvokable]
    public void OnBeginEditPressed()
    {
        if (
            _focusedTabStopId is not { } focusedId
            || Board?.GetComponent(focusedId) is null
            || !IsAddressable(focusedId)
        )
        {
            return;
        }

        _additiveTraversal = false;
        RequestInlineEdit(focusedId);
    }

    // The component calls this once at every edit end, whether or not anything changed.
    public void CommitInlineEdit(Guid instanceId, object before, object after, bool returnFocus)
    {
        if (!Equals(before, after))
        {
            CommitPropsChange(instanceId, before, after);
        }

        if (returnFocus)
        {
            _returningFocusAfterEdit = true;
            _editFocusStopId = Board?.GetComponent(instanceId) is not null
                ? instanceId
                : EdgeLabelled(instanceId)?.Id;
        }

        StateHasChanged();
    }

    private Edge? EdgeLabelled(Guid labelId) =>
        Board?.Edges.FirstOrDefault(edge => edge.Label?.Id == labelId);

    private void BeginInlineEdit(Guid instanceId)
    {
        if (IsAddressable(instanceId))
        {
            RequestInlineEdit(instanceId);
        }
    }

    private void BeginLabelEdit(Guid edgeId)
    {
        if (
            Board?.GetEdge(edgeId) is not { Locked: false, Label: { } label } edge
            || RouteOf(edge) is not { } route
        )
        {
            return;
        }

        var labelBox = LabelBox(label, route);
        if (IsBelowLodThreshold(labelBox))
        {
            return;
        }

        Reveal(labelBox);
        _pendingEdit = new PendingEdit(edgeId, IsEdgeLabel: true);
        StateHasChanged();
    }

    private void RequestInlineEdit(Guid instanceId)
    {
        if (
            Board?.GetComponent(instanceId) is not { Locked: false } instance
            || !Registry.Resolve(instance.ComponentTypeKey).IsInlineEditable
            || IsPlaceholder(instance)
        )
        {
            return;
        }

        Reveal(instance.Bounds);
        _pendingEdit = new PendingEdit(instanceId, IsEdgeLabel: false);
        StateHasChanged();
    }

    private void Reveal(Bounds bounds)
    {
        if (!_zoomPanTracker.HasKnownContainerSize)
        {
            return;
        }

        var (shiftX, shiftY) = ViewportReveal.ShiftToReveal(_zoomPanTracker.Viewport, bounds);
        var scale = _zoomPanTracker.Scale;
        _zoomPanTracker.SetPanPosition(
            _zoomPanTracker.PanX - shiftX * scale,
            _zoomPanTracker.PanY - shiftY * scale
        );
    }

    private void BeginPendingEdit()
    {
        if (_pendingEdit is not { } edit)
        {
            return;
        }

        _pendingEdit = null;
        var mounted = edit.IsEdgeLabel ? _mountedLabels : _mountedComponents;
        if (
            mounted.TryGetValue(edit.Id, out var component)
            && component.Instance is IInlineEditable editable
        )
        {
            editable.BeginEdit();
        }
    }

    private async Task ReturnFocusAfterEditAsync()
    {
        if (!_returningFocusAfterEdit)
        {
            return;
        }

        _returningFocusAfterEdit = false;
        _additiveTraversal = false;
        var index = _editFocusStopId is { } stopId ? FocusableTabStopIds().IndexOf(stopId) : -1;
        if (index >= 0)
        {
            await _jsModule!.InvokeVoidAsync("focusTabStopAt", ContainerElement, index);
        }
        else
        {
            await _jsModule!.InvokeVoidAsync("focusCanvas", CanvasElement);
        }
    }
}
