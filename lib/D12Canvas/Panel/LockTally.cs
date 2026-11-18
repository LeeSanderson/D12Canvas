namespace D12Canvas.Panel;

// How many of a selection's targets are locked. The property bar and the property panel both read
// it, so "some locked, some not" means the same thing in each.
public readonly record struct LockTally(int Locked, int Total)
{
    public static LockTally Of(IEnumerable<bool> lockedFlags)
    {
        var flags = lockedFlags.ToList();
        return new LockTally(flags.Count(locked => locked), flags.Count);
    }

    public bool IsPartial => Locked > 0 && Locked < Total;

    public bool IsFull => Total > 0 && Locked == Total;

    public string Fraction => $"{Locked}/{Total}";

    public string Summary => $"{Locked} of {Total} locked; changes skip them";
}
