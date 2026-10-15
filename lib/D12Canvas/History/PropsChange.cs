namespace D12Canvas.History;

// One instance's props before and after an edit, as a batch commit takes them.
public readonly record struct PropsChange(Guid InstanceId, object Before, object After);

// One edge's style before and after an edit, as a batch commit takes them.
public readonly record struct EdgeStyleChange(Guid EdgeId, EdgeStyle Before, EdgeStyle After);
