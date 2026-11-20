namespace D12Canvas.Pointer;

internal readonly record struct HitStackEntry(Guid Id, bool IsEdge);

internal static class HitStack
{
    public static HitStackEntry? NextBelow(
        IReadOnlyList<HitStackEntry> stack,
        SelectionSnapshot before,
        Func<Guid, Guid> resolve
    )
    {
        if (stack.Count == 0)
        {
            return null;
        }

        var position = SingleEntryOf(before, resolve) is { } selected
            ? IndexOf(stack, selected)
            : -1;
        return stack[position < 0 ? 1 % stack.Count : (position + 1) % stack.Count];
    }

    private static HitStackEntry? SingleEntryOf(
        SelectionSnapshot snapshot,
        Func<Guid, Guid> resolve
    ) =>
        (snapshot.InstanceIds.Count, snapshot.EdgeIds.Count) switch
        {
            (1, 0) => new HitStackEntry(resolve(snapshot.InstanceIds.Single()), IsEdge: false),
            (0, 1) => new HitStackEntry(snapshot.EdgeIds.Single(), IsEdge: true),
            _ => null,
        };

    private static int IndexOf(IReadOnlyList<HitStackEntry> stack, HitStackEntry entry)
    {
        for (var index = 0; index < stack.Count; index++)
        {
            if (stack[index] == entry)
            {
                return index;
            }
        }

        return -1;
    }
}
