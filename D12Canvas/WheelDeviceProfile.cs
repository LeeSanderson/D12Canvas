namespace D12Canvas;

// Which device a canvas treats the wheel as coming from. Mouse and Trackpad pin it; Auto takes it
// from the delta granularity the listener measured at the start of each wheel gesture.
public enum WheelDeviceProfile
{
    Auto,
    Mouse,
    Trackpad,
}
