using D12Canvas.Pointer;

namespace D12Canvas.Tests;

// Builds the three interop records the browser-side listener would send, with one pointer and a
// mouse, so a test states only the role, button, point and modifier it is about.
internal static class PointerEvents
{
    public const int PointerId = 1;

    public static PointerPress Press(
        string role,
        int button,
        double x,
        double y,
        Guid? entityId = null,
        bool shift = false,
        int pressCount = 1,
        string? part = null
    ) =>
        new(
            PointerId,
            button,
            ButtonsFor(button),
            "mouse",
            role,
            entityId,
            Part: part,
            PressCount: pressCount,
            x,
            y,
            shift,
            CtrlKey: false,
            AltKey: false,
            MetaKey: false
        );

    public static PointerMove Move(double x, double y, bool shift = false) =>
        new(PointerId, x, y, Buttons: 1, shift, CtrlKey: false, AltKey: false, MetaKey: false);

    public static PointerRelease Release(int button, double x, double y) =>
        new(
            PointerId,
            button,
            x,
            y,
            ShiftKey: false,
            CtrlKey: false,
            AltKey: false,
            MetaKey: false
        );

    private static int ButtonsFor(int button) =>
        button switch
        {
            PointerPress.PrimaryButton => 1,
            PointerPress.SecondaryButton => 2,
            _ => 4,
        };
}
