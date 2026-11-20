using D12Canvas.Model;

namespace D12Canvas.History;

public static class InstanceRemoval
{
    public static IReadOnlyList<ICommand> Compose(Board board, IEnumerable<Guid> instanceIds)
    {
        var instances = instanceIds
            .Distinct()
            .Select(board.GetComponent)
            .OfType<ComponentInstance>()
            .ToList();

        if (instances.Count == 0)
        {
            return [];
        }

        var removedIds = instances.Select(instance => instance.Id).ToHashSet();
        var commands = instances
            .Select(instance => (ICommand)new RemoveEntityCommand(board, instance))
            .ToList();

        var plan = GroupRepair.Plan(
            TouchedGroups(board, removedIds),
            id =>
                !removedIds.Contains(id)
                && (board.GetComponent(id) is not null || board.GetGroup(id) is not null)
        );

        foreach (var group in plan.Removed)
        {
            commands.Add(new UngroupCommand(board, group));
        }

        foreach (var (before, after) in plan.Replaced)
        {
            commands.Add(new UngroupCommand(board, before));
            commands.Add(new GroupCommand(board, after));
        }

        return commands;
    }

    private static IReadOnlyCollection<Group> TouchedGroups(Board board, HashSet<Guid> removedIds)
    {
        var touched = new Dictionary<Guid, Group>();
        var frontier = new Queue<Guid>(removedIds);

        while (frontier.TryDequeue(out var memberId))
        {
            foreach (var group in board.Groups)
            {
                if (group.MemberIds.Contains(memberId) && touched.TryAdd(group.Id, group))
                {
                    frontier.Enqueue(group.Id);
                }
            }
        }

        return touched.Values;
    }
}
