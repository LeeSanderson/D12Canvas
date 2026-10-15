using D12Canvas.Model;

namespace D12Canvas.History;

// Backs arrow-key nudge - moves one or more instances, and the floating ends of selected edges, by
// a board-space delta. Unlike ChangeBoundsCommand's fixed before/after, Extend grows this same
// command's accumulated delta in place - DiagramCanvas calls it for every repeat keydown of a held
// arrow key, so one press-to-release span becomes one history entry instead of one per repeat event.
public sealed class NudgeCommand : ICommand
{
    private readonly IReadOnlyList<ComponentInstance> _targets;
    private readonly Dictionary<Guid, Bounds> _before;
    private readonly IReadOnlyList<FloatingEnd> _floatingEnds;
    private double _totalDeltaX;
    private double _totalDeltaY;

    public NudgeCommand(
        IReadOnlyList<ComponentInstance> targets,
        double deltaX,
        double deltaY,
        IReadOnlyList<Edge>? edges = null
    )
    {
        _targets = targets;
        _before = targets.ToDictionary(t => t.Id, t => t.Bounds);
        _floatingEnds = FloatingEndsOf(edges ?? []);
        _totalDeltaX = deltaX;
        _totalDeltaY = deltaY;
    }

    public bool Matches(IReadOnlyList<ComponentInstance> targets, IReadOnlyList<Edge>? edges = null)
    {
        var floatingEnds = FloatingEndsOf(edges ?? []);
        return targets.Count == _before.Count
            && targets.All(t => _before.ContainsKey(t.Id))
            && floatingEnds.Count == _floatingEnds.Count
            && floatingEnds.All(end =>
                _floatingEnds.Any(held =>
                    ReferenceEquals(held.Edge, end.Edge) && held.IsSource == end.IsSource
                )
            );
    }

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

        foreach (var end in _floatingEnds)
        {
            end.Set(new FloatingEndpoint(end.Before.X + _totalDeltaX, end.Before.Y + _totalDeltaY));
        }
    }

    public void Undo()
    {
        foreach (var target in _targets)
        {
            target.Bounds = _before[target.Id];
        }

        foreach (var end in _floatingEnds)
        {
            end.Set(end.Before);
        }
    }

    private static List<FloatingEnd> FloatingEndsOf(IReadOnlyList<Edge> edges) =>
        edges
            .SelectMany(edge =>
                new[]
                {
                    (edge, IsSource: true, End: edge.Source),
                    (edge, IsSource: false, End: edge.Target),
                }
            )
            .Where(candidate => candidate.End is FloatingEndpoint)
            .Select(candidate => new FloatingEnd(
                candidate.edge,
                candidate.IsSource,
                (FloatingEndpoint)candidate.End
            ))
            .ToList();

    private sealed record FloatingEnd(Edge Edge, bool IsSource, FloatingEndpoint Before)
    {
        public void Set(FloatingEndpoint endpoint)
        {
            if (IsSource)
            {
                Edge.Source = endpoint;
            }
            else
            {
                Edge.Target = endpoint;
            }
        }
    }
}
