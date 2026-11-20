using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Persistence;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class DiagramCanvas
{
    public event EventHandler<PasteWarningsEventArgs>? PasteWarnings;

    private BoardJsonSerializer? _serializer;
    private BoardJsonSerializer Serializer => _serializer ??= new BoardJsonSerializer(Registry);

    private readonly PasteCascade _pasteCascade = new();

    private bool _asyncClipboard;

    private bool CanCopySelection =>
        SelectedEdges.Count > 0
        || ExpandedSelection().Any(id => Board?.GetComponent(id) is not null);

    [JSInvokable]
    public string? OnCopyRequested() =>
        PressOwnsBoard || CopiedFragment() is not { } fragment
            ? null
            : Serializer.Serialize(fragment);

    [JSInvokable]
    public string? OnCutRequested()
    {
        if (PressOwnsBoard || PlacingPort || CopiedFragment(forCut: true) is not { } fragment)
        {
            return null;
        }

        var payload = Serializer.Serialize(fragment);
        Remove(fragment);
        return payload;
    }

    private void Remove(Board fragment)
    {
        if (Board is null || PressOwnsBoard)
        {
            return;
        }

        var commands = InstanceRemoval
            .Compose(
                Board,
                fragment
                    .Components.Where(copied => Board.GetComponent(copied.Id) is { Locked: false })
                    .Select(copied => copied.Id)
            )
            .Concat(
                fragment
                    .Edges.Select(carried => Board.GetEdge(carried.Id))
                    .OfType<Edge>()
                    .Where(edge => !edge.Locked)
                    .Select(edge => new RemoveEdgeCommand(Board, edge))
            )
            .ToList();
        if (commands.Count > 0)
        {
            _history.Do(new CompositeCommand(commands));
        }

        _contextMenu = null;
        KeepSurvivingSelection();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnPasteReceived(string text, double? pointerX, double? pointerY) =>
        Paste(
            text,
            pointerX is { } x && pointerY is { } y ? ToBoardPoint((x, y), (0, 0)) : ViewportCentre()
        );

    private Board? CopiedFragment(bool forCut = false)
    {
        if (Board is null)
        {
            return null;
        }

        var fragment = BoardFragment.Of(
            Board,
            _selectedInstanceIds,
            _selectedEdgeIds,
            Registry,
            withoutLocked: forCut
        );
        return fragment.Components.Count == 0 && fragment.Edges.Count == 0 ? null : fragment;
    }

    private void Paste(string text, (double X, double Y) anchor)
    {
        if (Board is null || PressOwnsBoard || PlacingPort)
        {
            return;
        }

        if (ClipboardPayload.From(text, Serializer, Registry) is { } read)
        {
            PasteFragment(read.Fragment, anchor, read.Warnings);
        }
    }

    private void PasteFragment(
        Board fragment,
        (double X, double Y) anchor,
        IEnumerable<BoardDeserializeWarning> readWarnings
    )
    {
        var warnings = readWarnings.ToList();
        if (BoardFragment.Extent(fragment) is { } extent)
        {
            var (left, top) = SnapPoint(anchor.X - extent.Width / 2, anchor.Y - extent.Height / 2);
            var step = _pasteCascade.OffsetFor((left, top));
            BoardFragment.Translate(fragment, left + step - extent.X, top + step - extent.Y);

            var placement = Place(fragment);
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

    private (double X, double Y) MenuPasteAnchor(ContextMenuState menu) =>
        menu.OpenedFromKeyboard ? ViewportCentre() : ToBoardPoint((menu.X, menu.Y), (0, 0));

    private async Task CopyFromMenu(bool cut)
    {
        if (_jsModule is null || PressOwnsBoard || CopiedFragment(forCut: cut) is not { } fragment)
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
        var images = await _jsModule.InvokeAsync<HeldImage[]>("readClipboardImages");
        if (images.Length > 0)
        {
            await PasteImages(images, anchor);
            return;
        }

        if (await _jsModule.InvokeAsync<string?>("readClipboardText") is { } text)
        {
            Paste(text, anchor);
        }
    }
}
