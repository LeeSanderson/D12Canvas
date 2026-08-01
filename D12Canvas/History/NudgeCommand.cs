using D12Canvas.Model;

namespace D12Canvas.History;

// Backs arrow-key nudge - moves one or more instances by a board-space delta,
// uniformly for a single instance, an ad-hoc multi-selection, or a Group's members (DiagramCanvas
// always resolves the nudge target through ExpandedSelection, the same flattening
// CommitGroupMove/RestackSelection/ApplyZIndexChange already rely on).
// Unlike ChangeBoundsCommand's fixed before/after, Extend grows this same command's accumulated
// delta in place - DiagramCanvas calls it for every repeat keydown of a held arrow key, so one
// press-to-release span becomes one history entry instead of one per repeat event.
public sealed class NudgeCommand : ICommand
{
    private readonly IReadOnlyList<ComponentInstance> _targets;
    private readonly Dictionary<Guid, Bounds> _before;
    private double _totalDeltaX;
    private double _totalDeltaY;

    public NudgeCommand(IReadOnlyList<ComponentInstance> targets, double deltaX, double deltaY)
    {
        _targets = targets;
        _before = targets.ToDictionary(t => t.Id, t => t.Bounds);
        _totalDeltaX = deltaX;
        _totalDeltaY = deltaY;
    }

    public bool Matches(IReadOnlyList<ComponentInstance> targets) =>
        targets.Count == _before.Count && targets.All(t => _before.ContainsKey(t.Id));

    public void Extend(double deltaX, double deltaY)
    {
        _totalDeltaX += deltaX;
        _totalDeltaY += deltaY;
        Apply();
    }

    public void Apply()
    {
        foreach (var target in _targets)
        {
            var before = _before[target.Id];
            target.Bounds = new Bounds(
                before.X + _totalDeltaX,
                before.Y + _totalDeltaY,
                before.Width,
                before.Height
            );
        }
    }

    public void Undo()
    {
        foreach (var target in _targets)
        {
            target.Bounds = _before[target.Id];
        }
    }
}
