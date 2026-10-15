namespace D12Canvas.Model;

public sealed record MissingMember(Guid GroupId, Guid MemberId);

public sealed record DissolvedGroup(Group Group, Guid SurvivorId);

public sealed record GroupReplacement(Group Before, Group After);

// The repaired shape of a set of groups once some of their member ids stopped resolving: which
// groups are replaced by a copy with fewer members, which leave the board and why. The planner
// never touches a Board; the delete composition turns a plan into commands and the load paths
// write it straight onto the board being built.
public sealed class GroupRepairPlan
{
    public GroupRepairPlan(
        IReadOnlyList<GroupReplacement> replaced,
        IReadOnlyList<MissingMember> missingMembers,
        IReadOnlyList<Group> emptied,
        IReadOnlyList<DissolvedGroup> dissolved
    )
    {
        Replaced = replaced;
        MissingMembers = missingMembers;
        Emptied = emptied;
        Dissolved = dissolved;
    }

    public IReadOnlyList<GroupReplacement> Replaced { get; }
    public IReadOnlyList<MissingMember> MissingMembers { get; }
    public IReadOnlyList<Group> Emptied { get; }
    public IReadOnlyList<DissolvedGroup> Dissolved { get; }

    public IEnumerable<Group> Removed => Emptied.Concat(Dissolved.Select(entry => entry.Group));

    public void ApplyTo(Board board)
    {
        foreach (var group in Removed)
        {
            board.RemoveGroup(group.Id);
        }

        foreach (var (before, after) in Replaced)
        {
            board.RemoveGroup(before.Id);
            board.AddGroup(after);
        }
    }
}

public static class GroupRepair
{
    public static GroupRepairPlan Plan(IEnumerable<Group> groups, Func<Guid, bool> instanceResolves)
    {
        var groupsById = groups.ToDictionary(group => group.Id);
        var membersByGroupId = groupsById.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.MemberIds.ToList()
        );
        var missingMembers = new List<MissingMember>();
        var emptied = new List<Group>();
        var dissolved = new List<DissolvedGroup>();

        foreach (var (groupId, members) in membersByGroupId)
        {
            foreach (var memberId in members.ToList())
            {
                if (!instanceResolves(memberId) && !membersByGroupId.ContainsKey(memberId))
                {
                    members.Remove(memberId);
                    missingMembers.Add(new MissingMember(groupId, memberId));
                }
            }
        }

        var undersized = new Queue<Guid>(
            membersByGroupId.Where(entry => entry.Value.Count < 2).Select(entry => entry.Key)
        );
        var deferrals = new Dictionary<Guid, int>();

        while (undersized.TryDequeue(out var groupId))
        {
            if (!membersByGroupId.TryGetValue(groupId, out var members))
            {
                continue;
            }

            if (HasUndersizedChild(membersByGroupId, groupId, members) && Defer(deferrals, groupId))
            {
                undersized.Enqueue(groupId);
                continue;
            }

            membersByGroupId.Remove(groupId);
            var parentId = ParentOf(membersByGroupId, groupId);

            if (members.Count == 0)
            {
                emptied.Add(groupsById[groupId]);
                if (parentId is { } emptiedParentId)
                {
                    var parentMembers = membersByGroupId[emptiedParentId];
                    parentMembers.Remove(groupId);
                    if (parentMembers.Count < 2)
                    {
                        undersized.Enqueue(emptiedParentId);
                    }
                }
            }
            else
            {
                var survivor = members[0];
                dissolved.Add(new DissolvedGroup(groupsById[groupId], survivor));
                if (parentId is { } dissolvedParentId)
                {
                    var parentMembers = membersByGroupId[dissolvedParentId];
                    parentMembers[parentMembers.IndexOf(groupId)] = survivor;
                }
            }
        }

        var replaced = membersByGroupId
            .Where(entry => !entry.Value.SequenceEqual(groupsById[entry.Key].MemberIds))
            .Select(entry => new GroupReplacement(
                groupsById[entry.Key],
                new Group(entry.Value, entry.Key)
            ))
            .ToList();

        return new GroupRepairPlan(replaced, missingMembers, emptied, dissolved);
    }

    // A parent settles after its children, so the survivor it reports is the one that is still on
    // the board once the whole plan has run.
    private static bool HasUndersizedChild(
        Dictionary<Guid, List<Guid>> membersByGroupId,
        Guid groupId,
        List<Guid> members
    ) =>
        members.Any(memberId =>
            memberId != groupId
            && membersByGroupId.TryGetValue(memberId, out var childMembers)
            && childMembers.Count < 2
        );

    // A membership cycle in a hand-written file would otherwise defer forever; after one pass
    // over every group the deferral gives up and the group is processed as it stands.
    private static bool Defer(Dictionary<Guid, int> deferrals, Guid groupId)
    {
        var count = deferrals.GetValueOrDefault(groupId) + 1;
        deferrals[groupId] = count;
        return count <= deferrals.Count;
    }

    private static Guid? ParentOf(Dictionary<Guid, List<Guid>> membersByGroupId, Guid groupId)
    {
        foreach (var (candidateId, members) in membersByGroupId)
        {
            if (members.Contains(groupId))
            {
                return candidateId;
            }
        }

        return null;
    }
}
