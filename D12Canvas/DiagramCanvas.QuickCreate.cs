using D12Canvas.History;
using D12Canvas.Model;
using Microsoft.JSInterop;

namespace D12Canvas;

// A quick create duplicates through the same fragment path as Ctrl+D, so every id on the copy is
// fresh, but it never starts a duplicate run: its own chain is pressing the new node's side.
public partial class DiagramCanvas
{
    private const int QuickCreateGapCells = 2;

    [JSInvokable]
    public void OnQuickCreatePressed(string code)
    {
        if (
            PressOwnsBoard
            || _portFocusInstanceId is not null
            || _pendingConnectorSource is not null
            || _selectedInstanceIds.Count != 1
            || _selectedEdgeIds.Count != 0
            || PortIdForArrow(code) is not { } side
        )
        {
            return;
        }

        QuickCreate(new PortEndpoint(_selectedInstanceIds.Single(), side));
    }

    private void QuickCreate(IEdgeEndpoint sourcePort)
    {
        if (
            Board is null
            || sourcePort.ComponentId is not { } sourceId
            || Board.GetComponent(sourceId) is not { Locked: false } source
            || SideOf(source, sourcePort) is not { } side
        )
        {
            return;
        }

        var fragment = BoardFragment.WithFreshIds(
            BoardFragment.Of(Board, [source.Id], [], Registry)
        );
        var copy = fragment.Components.Single();
        var slot = QuickCreateSlot.For(
            source.Bounds,
            side,
            QuickCreateGapCells * DominantGridSpacing(),
            SnapSpacing,
            Board.Components.Select(instance => instance.Bounds).ToList()
        );
        BoardFragment.Translate(fragment, slot.X - source.Bounds.X, slot.Y - source.Bounds.Y);

        var placement = BoardFragment.PlaceOnto(Board, fragment);
        var edge = new Edge(sourcePort, new AutoPortEndpoint(copy.Id));
        RecordCreation(
            copy.Id,
            new CompositeCommand([placement.Command, new AddEdgeCommand(Board, edge)]),
            quickCreateSourceId: source.Id
        );

        _contextMenu = null;
        StepOutWhile(_ => true);
        SetSelection([copy.Id], []);
        _pendingFocusId = copy.Id;
        RequestInlineEdit(copy.Id);
        StateHasChanged();
    }

    private static PortId? SideOf(ComponentInstance instance, IEdgeEndpoint port) =>
        port switch
        {
            PortEndpoint standard => standard.PortId,
            CustomPortEndpoint custom => instance
                .CustomPorts.Where(def => def.Id == custom.PortId)
                .Select(BorderPartition.SideOf)
                .FirstOrDefault(),
            _ => null,
        };
}
