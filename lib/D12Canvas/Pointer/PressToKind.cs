namespace D12Canvas.Pointer;

internal static class PressToKind
{
    public static GestureKind? Resolve(PointerPress press) =>
        press.Button switch
        {
            PointerPress.MiddleButton => GestureKind.Pan,
            PointerPress.SecondaryButton => press.MenuVerdict == PointerPress.BrowserMenuVerdict
            && !press.Locked
                ? GestureKind.Native
                : GestureKind.Pan,
            PointerPress.PrimaryButton when press.Locked => GestureKind.MarqueeSelect,
            PointerPress.PrimaryButton => press.Role switch
            {
                HitRole.Canvas => GestureKind.MarqueeSelect,
                HitRole.Instance => GestureKind.MoveSelection,
                HitRole.SelectionBounds => GestureKind.MoveSelection,
                HitRole.ResizeHandle => GestureKind.ResizeSelection,
                HitRole.SelectionHandle => GestureKind.ResizeSelection,
                HitRole.Port => GestureKind.DragEdgeEnd,
                HitRole.EdgeEndpoint => GestureKind.DragEdgeEnd,
                HitRole.Edge => GestureKind.SelectEdge,
                HitRole.EdgeLabel => GestureKind.SelectEdge,
                HitRole.AuthorContent => GestureKind.Native,
                _ => null,
            },
            _ => null,
        };
}
