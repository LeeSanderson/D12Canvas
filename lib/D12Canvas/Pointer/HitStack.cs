namespace D12Canvas.Pointer;

// One entity under a point, resolved as a press on it would select it.
internal readonly record struct HitStackEntry(Guid Id, bool IsEdge);

// Every entity under the press point, topmost first in the paint order the browser reports. Read
// for two click outcomes only and never for the role the press classified to.
internal static class HitStack
{
    // The entry below the one selected before the press, wrapping from the bottom to the top, or
    // the entry below the top when the press did not start from a single entry of this stack.
    // The selection from before the press is resolved to the level the press stepped out to, so
    // a member selected inside a group the press left is found as that group.
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
