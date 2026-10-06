namespace D12Canvas;

// One wheel event, or the events of one animation frame summed, as the browser-side listener
// hands it over. X and Y are the container-relative pointer, the deltas are in pixels, and Coarse
// is the granularity the listener classified the wheel gesture with at its start. Public only
// because Blazor requires the interop entry point and its parameter to be.
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
