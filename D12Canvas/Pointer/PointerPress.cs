namespace D12Canvas.Pointer;

// What the browser-side listener hands the canvas for one press, after it has classified the
// hit target and taken the synchronous decisions. X and Y are container-relative screen pixels,
// converted once by the listener so no interop round trip stands between a press and its owner.
// Public only because Blazor requires the interop entry points and their parameters to be. Hits is
// what lies under the press point, topmost first, read only for a press on the selection box or
// a primary press with Alt held, whose click selects from it. MenuVerdict is the Menu verdict the
// listener took for a secondary press, browser or canvas, and null for any other button.
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
    string? MenuVerdict = null
)
{
    public const string BrowserMenuVerdict = "browser";
    public const string CanvasMenuVerdict = "canvas";

    public const int PrimaryButton = 0;
    public const int MiddleButton = 1;
    public const int SecondaryButton = 2;
}

// Velocity is how fast the pointer was travelling when it reached this point, in screen pixels per
// millisecond. A move re-sent for a modifier change, or re-run for a viewport change, carries
// zero, since the pointer itself is still. Hits is what lies under the point, topmost first, sent
// only for a press that carries an edge end, whose drop target is the shape under the pointer.
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

// Hits is what lies under the release point, topmost first, read only for a press that can drop
// an edge end, where the captured release's own target says nothing about what is beneath it.
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

// One marked element under a point, classified the way a press on it would be.
public sealed record PointerHit(string Role, Guid? EntityId, string? Part);
