namespace D12Canvas.Pointer;

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
    bool MetaKey,
    IReadOnlyList<PointerHit>? Hits = null,
    string? MenuVerdict = null,
    bool Locked = false
)
{
    public const string BrowserMenuVerdict = "browser";
    public const string CanvasMenuVerdict = "canvas";

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
    bool MetaKey,
    double Velocity = 0,
    IReadOnlyList<PointerHit>? Hits = null
);

public sealed record PointerRelease(
    int PointerId,
    int Button,
    double X,
    double Y,
    bool ShiftKey,
    bool CtrlKey,
    bool AltKey,
    bool MetaKey,
    IReadOnlyList<PointerHit>? Hits = null
);

public sealed record PointerHit(string Role, Guid? EntityId, string? Part);
