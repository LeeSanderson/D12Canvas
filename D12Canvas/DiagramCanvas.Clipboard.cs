using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Persistence;
using Microsoft.JSInterop;

namespace D12Canvas;

// The system clipboard is the only clipboard. Ctrl+C, Ctrl+X and Ctrl+V arrive from the
// browser's copy, cut and paste events, which need no permission; the menu's rows go through the
// async clipboard instead, which exists only in a secure context.
public partial class DiagramCanvas
{
    // Raised when a paste could not bring everything across, such as an instance whose component
    // type this canvas does not know. What did paste is on the board already.
    public event EventHandler<PasteWarningsEventArgs>? PasteWarnings;

    private BoardJsonSerializer? _serializer;
    private BoardJsonSerializer Serializer => _serializer ??= new BoardJsonSerializer(Registry);

    private readonly PasteCascade _pasteCascade = new();

    private bool _asyncClipboard;

    private bool CanCopySelection =>
        SelectedEdges.Count > 0
        || ExpandedSelection().Any(id => Board?.GetComponent(id) is not null);

    // The board envelope of what a copy carries, or null when the selection holds nothing, so the
    // browser's own copy of selected page text runs instead.
    [JSInvokable]
    public string? OnCopyRequested() =>
        PressOwnsBoard || CopiedFragment() is not { } fragment
            ? null
            : Serializer.Serialize(fragment);

    // A copy and then the removal of exactly what it carried, in one history entry: the selection
    // as a delete would remove it, plus the edges carried between the removed instances. Undo does
    // not take the payload back off the clipboard.
    [JSInvokable]
    public string? OnCutRequested()
    {
        if (PressOwnsBoard || CopiedFragment() is not { } fragment)
        {
            return null;
        }

        var payload = Serializer.Serialize(fragment);
        Remove(fragment);
        return payload;
    }

    // The fragment keeps the ids of what it copied, so the removal names exactly those entities
    // and skips any that have gone since.
    private void Remove(Board fragment)
    {
        if (Board is null || PressOwnsBoard)
        {
            return;
        }

        var commands = InstanceRemoval
            .Compose(Board, fragment.Components.Select(instance => instance.Id))
            .Concat(
                fragment
                    .Edges.Select(carried => Board.GetEdge(carried.Id))
                    .OfType<Edge>()
                    .Select(edge => new RemoveEdgeCommand(Board, edge))
            )
            .ToList();
        if (commands.Count > 0)
        {
            _history.Do(new CompositeCommand(commands));
        }

        _contextMenu = null;
        SetSelection([], []);
        StateHasChanged();
    }

    // The pointer is in container pixels while it is over the canvas and absent otherwise, in which
    // case the paste lands at the viewport centre.
    [JSInvokable]
    public void OnPasteReceived(string text, double? pointerX, double? pointerY) =>
        Paste(
            text,
            pointerX is { } x && pointerY is { } y ? ToBoardPoint((x, y), (0, 0)) : ViewportCentre()
        );

    private Board? CopiedFragment()
    {
        if (Board is null)
        {
            return null;
        }

        var fragment = BoardFragment.Of(Board, _selectedInstanceIds, _selectedEdgeIds, Registry);
        return fragment.Components.Count == 0 && fragment.Edges.Count == 0 ? null : fragment;
    }

    // The fragment is centred on the anchor and moved as one body, its top-left on the grid when
    // snapping is on. A repeat that would land on the same spot lands one cascade step further
    // along, which is measured after snapping so a pointer nudged within one grid cell still
    // cascades. What was pasted becomes the selection, at the board's top level.
    private void Paste(string text, (double X, double Y) anchor)
    {
        if (Board is null || PressOwnsBoard)
        {
            return;
        }

        if (ClipboardPayload.From(text, Serializer, Registry) is not { } read)
        {
            return;
        }

        var warnings = read.Warnings.ToList();
        if (BoardFragment.Extent(read.Fragment) is { } extent)
        {
            var (left, top) = SnapPoint(anchor.X - extent.Width / 2, anchor.Y - extent.Height / 2);
            var step = _pasteCascade.OffsetFor((left, top));
            BoardFragment.Translate(read.Fragment, left + step - extent.X, top + step - extent.Y);

            var placement = Place(read.Fragment);
            warnings.AddRange(
                placement.RejectedAssetIds.Select(id => new BoardDeserializeWarning(
                    id,
                    "The asset's bytes do not match its id; it was not added."
                ))
            );
        }

        if (warnings.Count > 0)
        {
            PasteWarnings?.Invoke(this, new PasteWarningsEventArgs(warnings));
        }

        StateHasChanged();
    }

    // What was placed becomes the selection at the board's top level, since the fragment's
    // groups and instances are added there.
    private BoardFragment.Placement Place(Board fragment)
    {
        var placement = BoardFragment.PlaceOnto(Board!, fragment);
        _history.Do(placement.Command);
        _contextMenu = null;
        StepOutWhile(_ => true);
        SetSelection(placement.TopLevelIds, placement.EdgeIds);
        return placement;
    }

    private (double X, double Y) ViewportCentre()
    {
        var viewport = _zoomPanTracker.Viewport;
        return (viewport.X + viewport.Width / 2, viewport.Y + viewport.Height / 2);
    }

    // A menu paste lands where the press that opened the menu was, and a menu the keyboard opened
    // has no press, so its paste takes the viewport centre.
    private (double X, double Y) MenuPasteAnchor(ContextMenuState menu) =>
        menu.OpenedFromKeyboard ? ViewportCentre() : ToBoardPoint((menu.X, menu.Y), (0, 0));

    // Cut removes only once the write has succeeded, and removes what was written even if the
    // selection moved while the write was in flight.
    private async Task CopyFromMenu(bool cut)
    {
        if (_jsModule is null || PressOwnsBoard || CopiedFragment() is not { } fragment)
        {
            return;
        }

        var payload = Serializer.Serialize(fragment);
        var written = await _jsModule.InvokeAsync<bool>("writeClipboardText", payload);
        if (written && cut)
        {
            Remove(fragment);
        }
    }

    private async Task PasteFromMenu(ContextMenuState menu)
    {
        if (_jsModule is null)
        {
            return;
        }

        var anchor = MenuPasteAnchor(menu);
        if (await _jsModule.InvokeAsync<string?>("readClipboardText") is { } text)
        {
            Paste(text, anchor);
        }
    }
}
