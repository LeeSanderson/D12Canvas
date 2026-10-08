using D12Canvas.History;
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

    // Held from a creation until anything else changes history, so a retraction never leaves an
    // undone entry for the retracted instance on redo.
    private RetractableCreation? _retractableCreation;

    private readonly record struct RetractableCreation(
        Guid Id,
        ICommand Command,
        Guid? QuickCreateSourceId
    );

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
        var labelledEdge = EdgeLabelled(instanceId);
        var entity = ResolvePropsEntity(instanceId);
        var removed = entity is not null && EndsEmpty(entity, before, after);
        RetractableCreation? retracted = null;

        if (removed)
        {
            retracted = RetractOrRemoveEmptied(entity!, labelledEdge, before, after);
        }
        else if (!Equals(before, after))
        {
            CommitPropsChange(instanceId, before, after);
        }

        if (returnFocus)
        {
            ReturnFocusAfterEdit(instanceId, labelledEdge, removed, retracted?.QuickCreateSourceId);
        }

        StateHasChanged();
    }

    private bool EndsEmpty(ComponentInstance instance, object before, object after) =>
        Registry
            .Resolve(instance.ComponentTypeKey)
            .CountsAsEmpty(UnresolvedForCommit(instance, before, after).After);

    // Retracts the instance's creation while it is still retractable, and otherwise removes the
    // instance as its own entry. Returns the creation it retracted.
    private RetractableCreation? RetractOrRemoveEmptied(
        ComponentInstance instance,
        Edge? labelledEdge,
        object before,
        object after
    )
    {
        if (_retractableCreation is { } creation && creation.Id == instance.Id)
        {
            _retractableCreation = null;
            if (_history.Retract(creation.Command))
            {
                return creation;
            }
        }

        var commands = new List<ICommand>();
        if (!Equals(before, after))
        {
            var (committedBefore, unresolvedAfter) = UnresolvedForCommit(instance, before, after);
            commands.Add(new MutateEntityCommand(instance, committedBefore, unresolvedAfter));
        }

        if (labelledEdge is not null)
        {
            commands.Add(new ChangeEdgeLabelCommand(labelledEdge, instance, after: null));
        }
        else
        {
            commands.AddRange(InstanceRemoval.Compose(Board!, [instance.Id]));
        }

        _history.Do(new CompositeCommand(commands));
        return null;
    }

    // A removed instance has no stop left, so focus goes to the quick create's source it came
    // from, the stop of the edge it labelled, or the canvas with nothing selected.
    private void ReturnFocusAfterEdit(
        Guid instanceId,
        Edge? labelledEdge,
        bool removed,
        Guid? quickCreateSourceId
    )
    {
        _returningFocusAfterEdit = true;
        if (labelledEdge is not null)
        {
            _editFocusStopId = labelledEdge.Id;
        }
        else if (quickCreateSourceId is { } sourceId && Board?.GetComponent(sourceId) is not null)
        {
            var sourceStopId = EffectiveSelectionId(sourceId);
            _editFocusStopId = sourceStopId;
            SetSelection([sourceStopId], []);
        }
        else if (removed)
        {
            _editFocusStopId = null;
            SetSelection([], []);
        }
        else
        {
            _editFocusStopId = instanceId;
        }
    }

    private void RecordCreation(Guid id, ICommand command, Guid? quickCreateSourceId = null)
    {
        _history.Do(command);
        _retractableCreation = new RetractableCreation(id, command, quickCreateSourceId);
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
