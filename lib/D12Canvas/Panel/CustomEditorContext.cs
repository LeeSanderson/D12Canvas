namespace D12Canvas.Panel;

public sealed record CustomEditorContext(
    object? Value,
    Action<object?> Commit,
    Func<byte[], string, string>? AddAsset = null,
    bool Disabled = false,
    bool IsMixed = false
);
