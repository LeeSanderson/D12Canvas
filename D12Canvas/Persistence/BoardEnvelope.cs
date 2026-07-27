namespace D12Canvas.Persistence;

internal sealed record BoardEnvelope(
    int SchemaVersion,
    IReadOnlyList<ComponentInstanceEnvelope> Components,
    IReadOnlyList<GroupEnvelope>? Groups = null
);

internal sealed record ComponentInstanceEnvelope(
    Guid Id,
    string ComponentTypeKey,
    object? Props,
    BoundsEnvelope Bounds,
    int ZIndex
);

internal sealed record GroupEnvelope(Guid Id, IReadOnlyList<Guid> MemberIds);

internal readonly record struct BoundsEnvelope(double X, double Y, double Width, double Height);
