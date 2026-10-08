namespace D12Canvas;

// What the browser reports once, when the canvas first renders: the container's size, and whether
// this is an Apple platform, which decides how a shortcut hint is drawn.
internal sealed record InitialFacts(double Width, double Height, bool ApplePlatform);
