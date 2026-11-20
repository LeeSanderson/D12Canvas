namespace D12Canvas;

public sealed record WheelInput(
    double X,
    double Y,
    double DeltaX,
    double DeltaY,
    bool Coarse,
    bool ShiftKey,
    bool CtrlKey,
    bool AltKey,
    bool MetaKey
);
