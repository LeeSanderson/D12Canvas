namespace D12Canvas;

internal abstract record WheelAction;

internal sealed record WheelZoom(double X, double Y, double Factor) : WheelAction;

internal sealed record WheelPan(double DeltaX, double DeltaY) : WheelAction;

internal static class WheelMapping
{
    private const double ZoomDamping = 600;

    public static WheelDeviceProfile DeviceFor(WheelInput input, WheelDeviceProfile profile) =>
        profile switch
        {
            WheelDeviceProfile.Auto => input.Coarse
                ? WheelDeviceProfile.Mouse
                : WheelDeviceProfile.Trackpad,
            _ => profile,
        };

    public static WheelAction Map(WheelInput input, WheelDeviceProfile device)
    {
        if (input.CtrlKey || input.MetaKey)
        {
            return ZoomAboutPointer(input);
        }

        if (device == WheelDeviceProfile.Trackpad || input.AltKey)
        {
            return new WheelPan(-input.DeltaX, -input.DeltaY);
        }

        if (input.ShiftKey)
        {
            return new WheelPan(-(input.DeltaX != 0 ? input.DeltaX : input.DeltaY), 0);
        }

        return ZoomAboutPointer(input);
    }

    private static WheelZoom ZoomAboutPointer(WheelInput input) =>
        new(input.X, input.Y, Math.Exp(-input.DeltaY / ZoomDamping));

    public static TimeSpan AmbientTransitionFor(WheelDeviceProfile device) =>
        device == WheelDeviceProfile.Mouse ? TimeSpan.FromMilliseconds(100) : TimeSpan.Zero;
}
