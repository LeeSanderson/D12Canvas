using AngleSharp.Dom;
using Bunit;
using D12Canvas.Pointer;

namespace D12Canvas.Tests;

internal static class CanvasPointer
{
    public static Task Press(
        this IRenderedComponent<DiagramCanvas> canvas,
        double x,
        double y,
        int button = PointerPress.PrimaryButton,
        bool shift = false,
        string role = HitRole.Canvas,
        Guid? entityId = null,
        string? part = null,
        string? menuVerdict = null
    ) =>
        canvas.InvokeAsync(
            () =>
                canvas.Instance.OnPointerPressed(
                    PointerEvents.Press(
                        role,
                        button,
                        x,
                        y,
                        entityId,
                        shift,
                        part: part,
                        menuVerdict: menuVerdict
                    )
                )
        );

    public static Task Move(
        this IRenderedComponent<DiagramCanvas> canvas,
        double x,
        double y,
        bool shift = false,
        bool alt = false
    ) =>
        canvas.InvokeAsync(
            () => canvas.Instance.OnPointerMoved(PointerEvents.Move(x, y, shift, alt: alt))
        );

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
        (double X, double Y) to,
        bool alt = false
    ) => canvas.Move(to.X, to.Y, alt: alt).GetAwaiter().GetResult();

    public static void ReleaseAt(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) at
    ) => canvas.Release(at.X, at.Y).GetAwaiter().GetResult();

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

    public static void PressHandle(
        this IRenderedComponent<DiagramCanvas> canvas,
        IElement handle,
        (double X, double Y) at
    )
    {
        var press = PointerEvents.Press(
            handle.GetAttribute("data-d12-role")!,
            PointerPress.PrimaryButton,
            at.X,
            at.Y,
            handle.Closest("[data-d12-entity]") is { } entity ? EntityOf(entity) : null,
            part: handle.GetAttribute("data-d12-part")
        );
        canvas.InvokeAsync(() => canvas.Instance.OnPointerPressed(press)).GetAwaiter().GetResult();
    }

    public static void DragHandle(
        this IRenderedComponent<DiagramCanvas> canvas,
        IElement handle,
        (double X, double Y) from,
        (double X, double Y) to
    )
    {
        canvas.PressHandle(handle, from);
        canvas.MoveTo(to);
        canvas.ReleaseAt(to);
    }

    public static void PressElement(
        this IRenderedComponent<DiagramCanvas> canvas,
        IElement element,
        (double X, double Y) at,
        int pressCount = 1,
        bool shift = false
    )
    {
        var press = PointerEvents.Press(
            element.GetAttribute("data-d12-role")!,
            PointerPress.PrimaryButton,
            at.X,
            at.Y,
            element.Closest("[data-d12-entity]") is { } entity ? EntityOf(entity) : null,
            shift,
            pressCount: pressCount,
            part: element.GetAttribute("data-d12-part")
        );
        canvas.InvokeAsync(() => canvas.Instance.OnPointerPressed(press)).GetAwaiter().GetResult();
    }

    public static void ReleaseOver(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) at,
        IElement? over = null
    )
    {
        var release = PointerEvents.Release(PointerPress.PrimaryButton, at.X, at.Y) with
        {
            Hits = HitsUnder(over),
        };
        canvas
            .InvokeAsync(() => canvas.Instance.OnPointerReleased(release))
            .GetAwaiter()
            .GetResult();
    }

    public static void ClickElement(
        this IRenderedComponent<DiagramCanvas> canvas,
        IElement element,
        int pressCount = 1,
        (double X, double Y) at = default,
        bool shift = false
    )
    {
        canvas.PressElement(element, at, pressCount, shift);
        canvas.ReleaseAt(at);
    }

    public static void DoubleClickElement(
        this IRenderedComponent<DiagramCanvas> canvas,
        IElement element,
        (double X, double Y) at = default
    )
    {
        canvas.ClickElement(element, at: at);
        canvas.ClickElement(element, pressCount: 2, at: at);
    }

    public static void MoveOver(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) to,
        IElement? over
    ) =>
        canvas
            .InvokeAsync(
                () =>
                    canvas.Instance.OnPointerMoved(
                        PointerEvents.Move(to.X, to.Y) with
                        {
                            Hits = HitsUnder(over),
                        }
                    )
            )
            .GetAwaiter()
            .GetResult();

    public static void DragConnector(
        this IRenderedComponent<DiagramCanvas> canvas,
        IElement from,
        (double X, double Y) start,
        (double X, double Y) end,
        IElement? over = null
    )
    {
        canvas.PressElement(from, start);
        canvas.MoveOver(end, over);
        canvas.ReleaseOver(end, over);
    }

    public static void DragConnectorToPort(
        this IRenderedComponent<DiagramCanvas> canvas,
        IElement from,
        (double X, double Y) start,
        (double X, double Y) end,
        Guid targetId,
        string part
    )
    {
        canvas.PressElement(from, start);
        canvas.MoveOver(end, canvas.ContainerOf(targetId));
        canvas.ReleaseOver(end, canvas.PortSpanOf(targetId, part));
    }

    public static void PressPort(
        this IRenderedComponent<DiagramCanvas> canvas,
        Guid instanceId,
        string part,
        (double X, double Y) at
    ) =>
        canvas
            .Press(at.X, at.Y, role: HitRole.Port, entityId: instanceId, part: part)
            .GetAwaiter()
            .GetResult();

    public static void ReleaseOverPort(
        this IRenderedComponent<DiagramCanvas> canvas,
        (double X, double Y) at,
        Guid instanceId,
        string part
    )
    {
        var release = PointerEvents.Release(PointerPress.PrimaryButton, at.X, at.Y) with
        {
            Hits =
            [
                new PointerHit(HitRole.Port, instanceId, part),
                new PointerHit(HitRole.Instance, instanceId, null),
            ],
        };
        canvas
            .InvokeAsync(() => canvas.Instance.OnPointerReleased(release))
            .GetAwaiter()
            .GetResult();
    }

    public static IElement ContainerOf(this IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.Find($".component-container[data-d12-entity='{id}']");

    public static IElement PortSpanOf(
        this IRenderedComponent<DiagramCanvas> canvas,
        Guid instanceId,
        string part
    ) =>
        canvas.Find(
            $".component-container[data-d12-entity='{instanceId}'] .port-span[data-d12-part='{part}']"
        );

    public static IElement SelectedPortSpan(
        this IRenderedComponent<DiagramCanvas> canvas,
        Guid instanceId,
        string part
    )
    {
        canvas.ClickOn(canvas.ContainerOf(instanceId));
        return canvas.PortSpanOf(instanceId, part);
    }

    private static IReadOnlyList<PointerHit> HitsUnder(IElement? element)
    {
        var hits = new List<PointerHit>();
        for (var current = element; current is not null; current = current.ParentElement)
        {
            if (current.GetAttribute("data-d12-role") is { } role)
            {
                hits.Add(
                    new PointerHit(
                        role,
                        current.Closest("[data-d12-entity]") is { } entity
                            ? EntityOf(entity)
                            : null,
                        current.GetAttribute("data-d12-part")
                    )
                );
            }
        }

        return hits;
    }

    private static Guid EntityOf(IElement element) =>
        Guid.Parse(
            element.GetAttribute("data-d12-entity")
                ?? throw new ArgumentException("The element carries no entity.", nameof(element))
        );
}
