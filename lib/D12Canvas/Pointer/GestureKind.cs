namespace D12Canvas.Pointer;

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
