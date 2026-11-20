namespace D12Canvas.Panel;

internal readonly record struct LockTally(int Locked, int Total)
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
