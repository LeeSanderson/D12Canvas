using Bunit;
using D12Canvas.Pointer;

namespace D12Canvas.Tests;

// Drives a rendered DiagramCanvas through the four public pointer entry points the browser-side
// listener calls, with the coordinates already container-relative as that listener delivers them.
// A press on bare canvas is the `canvas` role; a press on an entity names its role and id. The
// drag threshold lives in the listener, so a move here is always a real drag.
internal static class CanvasPointer
{
    public static Task Press(
        this IRenderedComponent<DiagramCanvas> canvas,
        double x,
        double y,
        int button = PointerPress.PrimaryButton,
        bool shift = false,
        string role = HitRole.Canvas,
        Guid? entityId = null
    ) =>
        canvas.InvokeAsync(
            () =>
                canvas.Instance.OnPointerPressed(
                    PointerEvents.Press(role, button, x, y, entityId, shift)
                )
        );

    public static Task Move(
        this IRenderedComponent<DiagramCanvas> canvas,
        double x,
        double y,
        bool shift = false
    ) => canvas.InvokeAsync(() => canvas.Instance.OnPointerMoved(PointerEvents.Move(x, y, shift)));

    public static Task Release(
        this IRenderedComponent<DiagramCanvas> canvas,
        double x,
        double y,
        int button = PointerPress.PrimaryButton
    ) =>
        canvas.InvokeAsync(
            () => canvas.Instance.OnPointerReleased(PointerEvents.Release(button, x, y))
        );

    public static Task Cancel(this IRenderedComponent<DiagramCanvas> canvas, string reason) =>
        canvas.InvokeAsync(() => canvas.Instance.OnPointerCancelled(reason));

    public static async Task ClickCanvas(
        this IRenderedComponent<DiagramCanvas> canvas,
        double x = 0,
        double y = 0,
        int button = PointerPress.PrimaryButton
    )
    {
        await canvas.Press(x, y, button);
        await canvas.Release(x, y, button);
    }

    public static async Task Drag(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) from,
        (double X, double Y) to,
        int button = PointerPress.PrimaryButton,
        bool shift = false
    )
    {
        await canvas.Press(from.X, from.Y, button, shift);
        await canvas.Move(to.X, to.Y, shift);
        await canvas.Release(to.X, to.Y, button);
    }

    // The middle button pans whatever it lands on, so it is the pan the tests reach for.
    public static Task Pan(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) from,
        (double X, double Y) to
    ) => canvas.Drag(from, to, PointerPress.MiddleButton);

    public static Task Marquee(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) from,
        (double X, double Y) to,
        bool shift = false
    ) => canvas.Drag(from, to, PointerPress.PrimaryButton, shift);
}
