using Bunit;
using D12Canvas.Pointer;

namespace D12Canvas.Tests;

// Drives a rendered Minimap through the entry points its pointer listener calls, with points
// relative to the minimap as that listener delivers them. The listener only ever reports the
// primary button for the minimap.
internal static class MinimapPointer
{
    public static Task Press(this IRenderedComponent<Minimap> minimap, double x, double y) =>
        minimap.InvokeAsync(
            () =>
                minimap.Instance.OnPointerPressed(
                    PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, x, y)
                )
        );

    public static Task Move(this IRenderedComponent<Minimap> minimap, double x, double y) =>
        minimap.InvokeAsync(() => minimap.Instance.OnPointerMoved(PointerEvents.Move(x, y)));

    public static Task Release(this IRenderedComponent<Minimap> minimap, double x, double y) =>
        minimap.InvokeAsync(
            () =>
                minimap.Instance.OnPointerReleased(
                    PointerEvents.Release(PointerPress.PrimaryButton, x, y)
                )
        );

    public static Task Cancel(this IRenderedComponent<Minimap> minimap, string reason) =>
        minimap.InvokeAsync(() => minimap.Instance.OnPointerCancelled(reason));

    public static Task Resize(
        this IRenderedComponent<Minimap> minimap,
        double width,
        double height
    ) => minimap.InvokeAsync(() => minimap.Instance.OnContainerResized(width, height));
}
