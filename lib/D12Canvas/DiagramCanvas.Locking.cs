using D12Canvas.History;
using D12Canvas.Model;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class DiagramCanvas
{
    private bool IsLocked(Guid id) => Board?.IsLocked(id) ?? false;

    [JSInvokable]
    public void OnToggleLockPressed() => SetSelectionLocked(!SelectionIsLocked);

    public void OnLockPressed() => SetSelectionLocked(true);

    public void OnUnlockPressed() => SetSelectionLocked(false);

    public void OnUnlockAllPressed()
    {
        if (Board is null || PressOwnsBoard || PlacingPort)
        {
            return;
        }

        var commands = Board
            .Components.Where(instance => instance.Locked)
            .Select(instance => (ICommand)new ChangeLockedCommand(instance, after: false))
            .Concat(
                Board
                    .Edges.Where(edge => edge.Locked)
                    .Select(edge => new ChangeLockedCommand(edge, after: false))
            )
            .ToList();
        DoLockCommands(commands);
    }

    private void SetSelectionLocked(bool locked)
    {
        if (Board is null || PressOwnsBoard || PlacingPort)
        {
            return;
        }

        var commands = ResolvedSelection()
            .Where(instance => instance.Locked != locked)
            .Select(instance => (ICommand)new ChangeLockedCommand(instance, locked))
            .Concat(
                SelectedEdges
                    .Where(edge => edge.Locked != locked)
                    .Select(edge => new ChangeLockedCommand(edge, locked))
            )
            .ToList();
        DoLockCommands(commands);
    }

    private void DoLockCommands(List<ICommand> commands)
    {
        if (commands.Count == 0)
        {
            return;
        }

        _history.Do(new CompositeCommand(commands));
        NotifySelectionChanged();
        StateHasChanged();
    }

    private bool SelectionIsLocked
    {
        get
        {
            var topLevel = _selectedInstanceIds
                .Where(id => Board?.GetComponent(id) is not null || Board?.GetGroup(id) is not null)
                .Concat(SelectedEdges.Select(edge => edge.Id))
                .ToList();
            return topLevel.Count > 0 && topLevel.All(IsLocked);
        }
    }

    private bool HasAnythingLocked =>
        Board is not null
        && (
            Board.Components.Any(instance => instance.Locked)
            || Board.Edges.Any(edge => edge.Locked)
        );

    private List<ComponentInstance> UnlockedSelection() =>
        ResolvedSelection().Where(instance => !instance.Locked).ToList();

    private List<Edge> UnlockedSelectedEdges() =>
        SelectedEdges.Where(edge => !edge.Locked).ToList();

    private bool SelectionTakesPresses =>
        PendingCopies is not null || UnlockedSelection().Count > 0;

    private string SelectionBoxCssClass =>
        SelectionTakesPresses ? "selection-bounding-box" : "selection-bounding-box locked";

    private bool CanRemoveSelection =>
        UnlockedSelection().Count > 0 || UnlockedSelectedEdges().Count > 0;

    private bool IsPropsEntityLocked(Guid id) =>
        Board?.GetComponent(id) is { } instance
            ? instance.Locked
            : Board?.Edges.FirstOrDefault(edge => edge.Label?.Id == id) is { Locked: true };
}
