namespace D12Canvas.Pointer;

// The press-to-kind table. The secondary and middle buttons ignore the role, so a later role can
// never change what the pan buttons do; the primary button is the only one that reads it. A null
// result means no gesture owns the press: the primary button on the port and edge roles
// is still served by the old per-element handlers, and the listener does not forward those
// presses at all, so those rows exist for the table's own completeness.
internal static class PressToKind
{
    public static GestureKind? Resolve(PointerPress press) =>
        press.Button switch
        {
            PointerPress.MiddleButton => GestureKind.Pan,
            PointerPress.SecondaryButton => GestureKind.Pan,
            PointerPress.PrimaryButton => press.Role switch
            {
                HitRole.Canvas => GestureKind.MarqueeSelect,
                HitRole.Instance => GestureKind.MoveSelection,
                HitRole.SelectionBounds => GestureKind.MoveSelection,
                HitRole.ResizeHandle => GestureKind.ResizeSelection,
                HitRole.SelectionHandle => GestureKind.ResizeSelection,
                HitRole.AuthorContent => GestureKind.Native,
                _ => null,
            },
            _ => null,
        };
}
