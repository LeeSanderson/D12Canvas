namespace D12Canvas;

internal sealed record InitialFacts(
    double Width,
    double Height,
    bool ApplePlatform,
    bool AsyncClipboard = false,
    Dictionary<string, string>? KeyLabels = null
);
