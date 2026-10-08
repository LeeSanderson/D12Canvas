namespace D12Canvas;

// What the browser reports once, when the canvas first renders: the container's size, whether
// this is an Apple platform, which decides how a shortcut hint is drawn, and whether the async
// clipboard exists, which it does only in a secure context and which the menu's clipboard rows
// need.
internal sealed record InitialFacts(
    double Width,
    double Height,
    bool ApplePlatform,
    bool AsyncClipboard = false
);
