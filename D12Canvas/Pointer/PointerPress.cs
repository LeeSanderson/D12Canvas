namespace D12Canvas.Pointer;

// What the browser-side listener hands the canvas for one press, after it has classified the
// hit target and taken the synchronous decisions. X and Y are container-relative screen pixels,
// converted once by the listener so no interop round trip stands between a press and its owner.
// Public only because Blazor requires the interop entry points and their parameters to be.
public sealed record PointerPress(
    int PointerId,
    int Button,
    int Buttons,
    string PointerType,
    string Role,
    Guid? EntityId,
    string? Part,
    int PressCount,
    double X,
    double Y,
    bool ShiftKey,
    bool CtrlKey,
    bool AltKey,
    bool MetaKey
)
{
    public const int PrimaryButton = 0;
    public const int MiddleButton = 1;
    public const int SecondaryButton = 2;
}

public sealed record PointerMove(
    int PointerId,
    double X,
    double Y,
    int Buttons,
    bool ShiftKey,
    bool CtrlKey,
    bool AltKey,
    bool MetaKey
);

public sealed record PointerRelease(
    int PointerId,
    int Button,
    double X,
    double Y,
    bool ShiftKey,
    bool CtrlKey,
    bool AltKey,
    bool MetaKey
);
