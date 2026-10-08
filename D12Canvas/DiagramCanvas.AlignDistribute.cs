using D12Canvas.History;
using D12Canvas.Model;

namespace D12Canvas;

// The selection is read unexpanded, so a selected group is one box with one delta that every
// member below it shares, and its own arrangement survives. Edges are not part of it.
public partial class DiagramCanvas
{
    public void OnAlignLeftPressed() => AlignSelection(AlignEdge.Left);

    public void OnAlignCentrePressed() => AlignSelection(AlignEdge.Centre);

    public void OnAlignRightPressed() => AlignSelection(AlignEdge.Right);

    public void OnAlignTopPressed() => AlignSelection(AlignEdge.Top);

    public void OnAlignMiddlePressed() => AlignSelection(AlignEdge.Middle);

    public void OnAlignBottomPressed() => AlignSelection(AlignEdge.Bottom);

    public void OnDistributeHorizontallyPressed() => DistributeSelection(DistributeAxis.Horizontal);

    public void OnDistributeVerticallyPressed() => DistributeSelection(DistributeAxis.Vertical);

    private void AlignSelection(AlignEdge edge) =>
        ArrangeSelection(entities => AlignDistribute.Align(entities, edge, SnapSpacing));

    private void DistributeSelection(DistributeAxis axis) =>
        ArrangeSelection(entities => AlignDistribute.Distribute(entities, axis, SnapSpacing));

    private void ArrangeSelection(
        Func<IReadOnlyList<ArrangedEntity>, IReadOnlyList<ArrangeDelta>> arrange
    )
    {
        if (Board is null || PressOwnsBoard)
        {
            return;
        }

        var commands = new List<ICommand>();
        foreach (var delta in arrange(ArrangeableSelection()))
        {
            var leaves = new HashSet<Guid>();
            ExpandInto(delta.Id, leaves);
            foreach (var leaf in leaves.Select(Board.GetComponent).OfType<ComponentInstance>())
            {
                var before = leaf.Bounds;
                var after = before with { X = before.X + delta.Dx, Y = before.Y + delta.Dy };
                commands.Add(new ChangeBoundsCommand(leaf, before, after));
            }
        }

        if (commands.Count == 0)
        {
            return;
        }

        _history.Do(new CompositeCommand(commands));
        StateHasChanged();
    }

    private List<ArrangedEntity> ArrangeableSelection()
    {
        if (Board is null)
        {
            return [];
        }

        var entities = new List<ArrangedEntity>();
        foreach (var id in _selectedInstanceIds)
        {
            var bounds =
                Board.GetComponent(id) is { } instance ? instance.Bounds
                : Board.GetGroup(id) is { } group ? Board.GetBounds(group)
                : null;
            if (bounds is { } box)
            {
                entities.Add(new ArrangedEntity(id, box));
            }
        }

        return entities;
    }

    private double? SnapSpacing => SnapToGrid ? DominantGridSpacing() : null;
}
