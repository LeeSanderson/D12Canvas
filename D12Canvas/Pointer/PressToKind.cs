namespace D12Canvas.Pointer;

// The press-to-kind table. The middle button ignores the role, so a later role can never change
// what it does. The secondary button ignores it too and reads only the Menu verdict, which the
// listener took from the role and the content under the press: a press the browser kept for its
// own menu is the browser's, as a primary press on author content is, and any other pans.
internal static class PressToKind
{
    public static GestureKind? Resolve(PointerPress press) =>
        press.Button switch
        {
            PointerPress.MiddleButton => GestureKind.Pan,
            PointerPress.SecondaryButton => press.MenuVerdict == PointerPress.BrowserMenuVerdict
                ? GestureKind.Native
                : GestureKind.Pan,
            PointerPress.PrimaryButton => press.Role switch
            {
                HitRole.Canvas => GestureKind.MarqueeSelect,
                HitRole.Instance => GestureKind.MoveSelection,
                HitRole.SelectionBounds => GestureKind.MoveSelection,
                HitRole.ResizeHandle => GestureKind.ResizeSelection,
                HitRole.SelectionHandle => GestureKind.ResizeSelection,
                HitRole.Port => GestureKind.DragEdgeEnd,
                HitRole.PortStrip => GestureKind.DragEdgeEnd,
                HitRole.EdgeEndpoint => GestureKind.DragEdgeEnd,
                HitRole.Edge => GestureKind.SelectEdge,
                HitRole.EdgeLabel => GestureKind.SelectEdge,
                HitRole.AuthorContent => GestureKind.Native,
                _ => null,
            },
            _ => null,
        };
}
