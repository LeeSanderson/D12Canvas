namespace D12Canvas.Persistence;

internal sealed record BoardEnvelope(
    int SchemaVersion,
    IReadOnlyList<ComponentInstanceEnvelope> Components
);

internal sealed record ComponentInstanceEnvelope(
    Guid Id,
    string ComponentTypeKey,
    object? Props,
    BoundsEnvelope Bounds,
    int ZIndex
);

internal readonly record struct BoundsEnvelope(double X, double Y, double Width, double Height);
