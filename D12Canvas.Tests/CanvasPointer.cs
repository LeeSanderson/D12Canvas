using AngleSharp.Dom;
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

    // A primary click on a rendered instance or LOD placeholder, as the listener reports it: the
    // `instance` role and the entity the element carries. Synchronous, like bUnit's own Click().
    public static void ClickOn(
        this IRenderedComponent<DiagramCanvas> canvas,
        IElement instanceElement,
        bool shift = false,
        int pressCount = 1
    ) =>
        canvas
            .InvokeAsync(() =>
            {
                canvas.Instance.OnPointerPressed(
                    PointerEvents.Press(
                        HitRole.Instance,
                        PointerPress.PrimaryButton,
                        0,
                        0,
                        EntityOf(instanceElement),
                        shift,
                        pressCount
                    )
                );
                canvas.Instance.OnPointerReleased(
                    PointerEvents.Release(PointerPress.PrimaryButton, 0, 0)
                );
            })
            .GetAwaiter()
            .GetResult();

    // A primary drag that starts on a rendered instance, from one container-relative point to
    // another, released where it ends.
    public static void DragOn(
        this IRenderedComponent<DiagramCanvas> canvas,
        IElement instanceElement,
        (double X, double Y) from,
        (double X, double Y) to,
        bool shift = false
    )
    {
        canvas.PressOn(instanceElement, from, shift);
        canvas.MoveTo(to);
        canvas.ReleaseAt(to);
    }

    public static void PressOn(
        this IRenderedComponent<DiagramCanvas> canvas,
        IElement element,
        (double X, double Y) at,
        bool shift = false,
        string role = HitRole.Instance
    ) =>
        canvas
            .InvokeAsync(
                () =>
                    canvas.Instance.OnPointerPressed(
                        PointerEvents.Press(
                            role,
                            PointerPress.PrimaryButton,
                            at.X,
                            at.Y,
                            role == HitRole.SelectionBounds ? null : EntityOf(element),
                            shift
                        )
                    )
            )
            .GetAwaiter()
            .GetResult();

    public static void MoveTo(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) to
    ) => canvas.Move(to.X, to.Y).GetAwaiter().GetResult();

    public static void ReleaseAt(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) at
    ) => canvas.Release(at.X, at.Y).GetAwaiter().GetResult();

    // The multi-selection's box, pressed and dragged as the listener reports it: the
    // `selection-bounds` role with no entity.
    public static void DragSelectionBox(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) from,
        (double X, double Y) to
    )
    {
        canvas.PressOn(canvas.Find(".selection-bounding-box"), from, role: HitRole.SelectionBounds);
        canvas.MoveTo(to);
        canvas.ReleaseAt(to);
    }

    private static Guid EntityOf(IElement element) =>
        Guid.Parse(
            element.GetAttribute("data-d12-entity")
                ?? throw new ArgumentException("The element carries no entity.", nameof(element))
        );
}
