using System.Text.Json.Serialization;
using D12Canvas.Model;

namespace D12Canvas.Persistence;

internal sealed record BoardEnvelope(
    int SchemaVersion,
    IReadOnlyList<ComponentInstanceEnvelope> Components,
    IReadOnlyList<GroupEnvelope>? Groups = null,
    IReadOnlyList<EdgeEnvelope>? Edges = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<AssetEnvelope>? Assets = null
);

internal sealed record AssetEnvelope(string Id, string MimeType, byte[] Data);

internal sealed record ComponentInstanceEnvelope(
    Guid Id,
    string ComponentTypeKey,
    object? Props,
    BoundsEnvelope Bounds,
    int ZIndex,
    IReadOnlyList<PortDefEnvelope>? CustomPorts = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Locked = false
);

internal readonly record struct PortDefEnvelope(Guid Id, double FractionX, double FractionY);

internal sealed record GroupEnvelope(Guid Id, IReadOnlyList<Guid> MemberIds);

internal sealed record EdgeEnvelope(
    Guid Id,
    EdgeEndpointEnvelope Source,
    EdgeEndpointEnvelope Target,
    EdgeRouting RoutingStyle = EdgeRouting.Straight,
    ArrowStyle SourceArrow = ArrowStyle.None,
    ArrowStyle TargetArrow = ArrowStyle.Arrow,
    ComponentInstanceEnvelope? Label = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Color = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Locked = false
);

internal sealed record EdgeEndpointEnvelope(
    Guid? ComponentId,
    PortId? PortId,
    double? X,
    double? Y,
    Guid? CustomPortId = null
);

internal readonly record struct BoundsEnvelope(double X, double Y, double Width, double Height);
