namespace D12Canvas.Pointer;

// The closed set of pointer gestures. Every press resolves to exactly one member, which owns the
// pointer until its claiming button comes up. The release-reliability and cancel theories
// enumerate this set, so a new member fails the suite until its cases exist.
internal enum GestureKind
{
    Pan,
    MarqueeSelect,
    MoveSelection,
    ResizeSelection,
    DragEdgeEnd,
    SelectEdge,
    Native,
    MinimapPan,
}

internal enum GesturePhase
{
    Pointing,
    Active,
    Cancelled,
}
