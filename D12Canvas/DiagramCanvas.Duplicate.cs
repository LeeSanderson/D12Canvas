using Microsoft.JSInterop;

namespace D12Canvas;

// Duplicate places a copy straight onto the board, never through the clipboard, so whatever the
// user copied elsewhere survives. The run's offset is replayed as it stands and never snapped:
// snapping each step against a zoom-dependent grid would drift, while a verbatim replay puts the
// nth copy exactly n offsets along.
public partial class DiagramCanvas
{
    private readonly DuplicateRun _duplicateRun = new();

    [JSInvokable]
    public void OnDuplicatePressed()
    {
        if (
            Board is null
            || PressOwnsBoard
            || CopiedFragment() is not { } fragment
            || BoardFragment.Extent(fragment) is not { } source
        )
        {
            return;
        }

        var sourceTopLeft = (source.X, source.Y);
        var (dx, dy) = _duplicateRun.OffsetFor(
            _selectedInstanceIds,
            _selectedEdgeIds,
            sourceTopLeft
        );
        var copy = BoardFragment.WithFreshIds(fragment);
        BoardFragment.Translate(copy, dx, dy);

        var placement = Place(copy);
        _duplicateRun.RecordDuplicate(placement.TopLevelIds, placement.EdgeIds, sourceTopLeft);
        StateHasChanged();
    }
}
