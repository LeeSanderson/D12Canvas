namespace D12Canvas.Pointer;

// The roles a press on the board can classify to, as the marker values the markup renders and
// the listener reads. `canvas` means the walk from the event target found no marked element.
internal static class HitRole
{
    public const string Instance = "instance";
    public const string ResizeHandle = "resize-handle";
    public const string Port = "port";
    public const string Edge = "edge";
    public const string EdgeEndpoint = "edge-endpoint";
    public const string EdgeLabel = "edge-label";
    public const string SelectionBounds = "selection-bounds";
    public const string SelectionHandle = "selection-handle";
    public const string AuthorContent = "author-content";
    public const string Canvas = "canvas";
}
