using Microsoft.JSInterop;

namespace D12Canvas;

public partial class DiagramCanvas
{
    private readonly DuplicateRun _duplicateRun = new();

    [JSInvokable]
    public void OnDuplicatePressed()
    {
        if (
            Board is null
            || PressOwnsBoard
            || PlacingPort
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
