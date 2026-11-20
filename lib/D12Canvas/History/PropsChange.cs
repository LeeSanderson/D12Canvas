namespace D12Canvas.History;

public readonly record struct PropsChange(Guid InstanceId, object Before, object After);

public readonly record struct EdgeStyleChange(Guid EdgeId, EdgeStyle Before, EdgeStyle After);
