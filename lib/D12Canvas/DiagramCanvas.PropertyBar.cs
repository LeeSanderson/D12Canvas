using D12Canvas.History;
using D12Canvas.Panel;

namespace D12Canvas;

public partial class DiagramCanvas
{
    private sealed record PropertyBarState(
        IReadOnlyList<PropertyBarRow> Rows,
        PropertyBarAnchor Anchor,
        LockTally Locks
    );

    private PropertyBarState? CurrentPropertyBar()
    {
        if (!BoardShown || _activeGesture is not null || _contextMenu is not null)
        {
            return null;
        }

        var instances = ResolvedSelection();
        var edges = SelectedEdges;
        if (Board!.ExtentOf(instances, edges) is not { } extent)
        {
            return null;
        }

        var rows = PropertyBarRows.For(
            instances,
            edges,
            Registry,
            changes =>
            {
                CommitPropsChangeBatch(changes);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            },
            changes =>
            {
                CommitEdgeStyleChangeBatch(changes);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        );
        if (rows.Count == 0)
        {
            return null;
        }

        var scale = _zoomPanTracker.Scale;
        return new PropertyBarState(
            rows,
            new PropertyBarAnchor(
                extent.X * scale + _zoomPanTracker.PanX,
                extent.Y * scale + _zoomPanTracker.PanY,
                extent.Width * scale
            ),
            LockTally.Of(
                instances
                    .Select(instance => instance.Locked)
                    .Concat(edges.Select(edge => edge.Locked))
            )
        );
    }

    public void CommitEdgeStyleChangeBatch(IReadOnlyList<EdgeStyleChange> changes)
    {
        var commands = new List<ICommand>();
        foreach (var (edgeId, before, after) in changes)
        {
            if (Board?.GetEdge(edgeId) is { Locked: false } edge)
            {
                commands.Add(new ChangeEdgeStyleCommand(edge, before, after));
            }
        }

        if (commands.Count == 0)
        {
            return;
        }

        _history.Do(new CompositeCommand(commands));
        StateHasChanged();
    }
}
