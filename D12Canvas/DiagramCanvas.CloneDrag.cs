using D12Canvas.Model;

namespace D12Canvas;

// A clone drag's copies are real entities held in the gesture preview until release: they render
// as committed ones do, with the selected look the originals give up while the copies are in the
// user's hand, and the release adds those same objects, so nothing on screen is rebuilt.
public partial class DiagramCanvas
{
    private Board? PendingCopies => _preview.PendingFragment;

    private Board? CopyOfSelection()
    {
        if (Board is null || CopiedFragment() is not { } fragment)
        {
            return null;
        }

        var copies = BoardFragment.WithFreshIds(fragment);
        BoardFragment.StackAbove(Board, copies);
        return copies;
    }

    // The run's source is where the copies started, which is where the originals stand, so a
    // Ctrl+D straight after the drop repeats the drag's offset.
    private void CommitClone(Board copies)
    {
        if (BoardFragment.Extent(copies) is not { } source)
        {
            return;
        }

        foreach (var copy in copies.Components)
        {
            if (_preview.TryGetBounds(copy.Id, out var bounds))
            {
                copy.Bounds = bounds;
            }
        }

        foreach (var edge in copies.Edges)
        {
            edge.Source = Live.EndpointOf(edge, isSource: true);
            edge.Target = Live.EndpointOf(edge, isSource: false);
        }

        var placement = Place(copies);
        _duplicateRun.RecordDuplicate(
            placement.TopLevelIds,
            placement.EdgeIds,
            (source.X, source.Y)
        );
    }

    private IEnumerable<Edge> RenderedEdges() =>
        Board is null ? []
        : PendingCopies is { } copies ? Board.Edges.Concat(copies.Edges)
        : Board.Edges;

    // Every copy mounts whatever windowing says, since windowing reads committed bounds and the
    // copies have none yet. A copied top-level group renders its stop too, as the copied shapes
    // render their containers, so it can show selected; none of them takes focus until it is on
    // the board.
    private IEnumerable<TabStop> RenderedStops() =>
        PendingCopies is { } copies
            ? OrderedTabStops().Concat(CopyStops(copies))
            : OrderedTabStops();

    private IEnumerable<TabStop> CopyStops(Board copies)
    {
        var live = Live;
        var nestedIds = copies.Groups.SelectMany(group => group.MemberIds).ToHashSet();
        var groupStops = copies
            .Groups.Where(group => !nestedIds.Contains(group.Id))
            .Select(group =>
                copies.GetBounds(group, live.BoundsOf) is { } bounds
                    ? new TabStop(Instance: null, Group: group, Edge: null, Bounds: bounds)
                    : (TabStop?)null
            )
            .OfType<TabStop>();
        return copies
            .Components.Select(copy => new TabStop(
                Instance: copy,
                Group: null,
                Edge: null,
                Bounds: live.BoundsOf(copy)
            ))
            .Concat(groupStops);
    }

    private bool IsPendingCopy(Guid id) =>
        PendingCopies is { } copies
        && (copies.GetComponent(id) is not null || copies.GetGroup(id) is not null);
}
