using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A middle-button press is dispatched straight at each marked element, so the walk starts from
// exactly that element whatever lies on top of it, and the middle button is used because the
// listener forwards it from every role and it changes nothing on release.
public sealed class PressClassificationProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string SourceId = "a0000000-0000-0000-0000-000000000001";
    private const string LooseEndId = "a0000000-0000-0000-0000-000000000003";
    private const string LabelledEdgeId = "e0000000-0000-0000-0000-000000000001";
    private const string FloatingEdgeId = "e0000000-0000-0000-0000-000000000002";

    public enum Selection
    {
        Nothing,
        OneInstance,
        TwoInstances,
    }

    public static TheoryData<string, Selection, string, string, string?, string?> MarkedElements =>
        new()
        {
            { "the bare canvas", Selection.Nothing, ".canvas-content", "canvas", null, null },
            {
                "an instance",
                Selection.Nothing,
                $".component-container[data-d12-entity='{SourceId}']",
                "instance",
                SourceId,
                null
            },
            {
                "unmarked content inside an instance",
                Selection.Nothing,
                $"[data-d12-entity='{SourceId}'] .d12-rectangle",
                "instance",
                SourceId,
                null
            },
            {
                "an edge",
                Selection.Nothing,
                $"[data-d12-role='edge'][data-d12-entity='{LabelledEdgeId}']",
                "edge",
                LabelledEdgeId,
                null
            },
            {
                "content inside an edge label",
                Selection.Nothing,
                ".edge-label > *",
                "edge-label",
                LabelledEdgeId,
                null
            },
            {
                "a floating endpoint",
                Selection.Nothing,
                ".floating-endpoint",
                "edge-endpoint",
                FloatingEdgeId,
                "target"
            },
            {
                "a port",
                Selection.OneInstance,
                $"[data-d12-entity='{LooseEndId}'] [data-d12-role='port'][data-d12-part='Right']",
                "port",
                LooseEndId,
                "Right"
            },
            {
                "a side's resize span",
                Selection.OneInstance,
                $"[data-d12-entity='{LooseEndId}'] .resize-span-top",
                "resize-handle",
                LooseEndId,
                "top"
            },
            {
                "a resize handle",
                Selection.OneInstance,
                $"[data-d12-entity='{LooseEndId}'] .resize-handle.bottom-right",
                "resize-handle",
                LooseEndId,
                "bottom-right"
            },
            {
                "the selection box",
                Selection.TwoInstances,
                ".selection-bounding-box",
                "selection-bounds",
                null,
                null
            },
            {
                "a selection box handle",
                Selection.TwoInstances,
                ".group-resize-handle.top-left",
                "selection-handle",
                null,
                "top-left"
            },
        };

    [Theory]
    [MemberData(nameof(MarkedElements))]
    public async Task APressOnAMarkedElement_ArrivesWithItsRoleEntityAndPart(
        string element,
        Selection selection,
        string selector,
        string role,
        string? entityId,
        string? part
    )
    {
        await SelectAsync(selection);
        await Expect(Page.Locator(selector).First).ToBeAttachedAsync();

        await DispatchMiddleClickAtAsync(selector, injectedChild: null);

        var press = await SinglePressAsync();
        var arrived = (
            Role: press.GetProperty("role").GetString(),
            EntityId: press.GetProperty("entityId").GetString(),
            Part: press.GetProperty("part").GetString()
        );
        Assert.True(
            arrived == (role, entityId, part),
            $"A press on {element} arrived as {arrived}, expected {(role, entityId, part)}"
        );
    }

    [Theory]
    [InlineData("<input type='text'>")]
    [InlineData("<div data-d12-author-content><span>marked</span></div>")]
    public async Task APressOnAuthorContentInsideAnInstance_ArrivesAsAuthorContentForThatInstance(
        string authorMarkup
    )
    {
        await DispatchMiddleClickAtAsync(
            $"[data-d12-entity='{SourceId}'] .container-content",
            injectedChild: authorMarkup
        );

        var press = await SinglePressAsync();
        Assert.Equal("author-content", press.GetProperty("role").GetString());
        Assert.Equal(SourceId, press.GetProperty("entityId").GetString());
        Assert.Null(press.GetProperty("part").GetString());
    }

    private async Task SelectAsync(Selection selection)
    {
        switch (selection)
        {
            case Selection.OneInstance:
                var looseEnd = Page.Locator(
                    $".component-container[data-d12-entity='{LooseEndId}']"
                );
                await looseEnd.ClickAsync();
                await Expect(looseEnd).ToHaveAttributeAsync("aria-selected", "true");
                break;
            case Selection.TwoInstances:
                var from = await PagePointOnCanvasAsync(20, 20);
                var to = await PagePointOnCanvasAsync(540, 160);
                await Page.Mouse.MoveAsync(from.X, from.Y);
                await Page.Mouse.DownAsync();
                await Page.Mouse.MoveAsync(to.X, to.Y, new() { Steps = 4 });
                await Page.Mouse.UpAsync();
                await Expect(Page.Locator(".selection-bounding-box")).ToBeVisibleAsync();
                break;
        }

        await SettleAsync();
        await ClearCallsAsync();
    }

    // The press goes to the element itself, or to markup injected as its last child when the
    // case needs content no registered component renders, and the release goes to the canvas,
    // which holds capture by then.
    private async Task DispatchMiddleClickAtAsync(string selector, string? injectedChild)
    {
        await Page.EvaluateAsync(
            """
            ([selector, injectedChild]) => {
                let target = document.querySelector(selector);
                if (injectedChild !== null) {
                    target.insertAdjacentHTML("beforeend", injectedChild);
                    target = target.lastElementChild;
                    while (target.firstElementChild) {
                        target = target.firstElementChild;
                    }
                }
                const canvas = document.querySelector(".diagram-canvas");
                const rect = target.getBoundingClientRect();
                const init = {
                    bubbles: true,
                    cancelable: true,
                    composed: true,
                    pointerId: 1,
                    pointerType: "mouse",
                    isPrimary: true,
                    button: 1,
                    buttons: 4,
                    clientX: rect.left + rect.width / 2,
                    clientY: rect.top + rect.height / 2
                };
                target.dispatchEvent(new PointerEvent("pointerdown", init));
                canvas.dispatchEvent(new PointerEvent("pointerup", { ...init, buttons: 0 }));
            }
            """,
            new object?[] { selector, injectedChild }
        );
        await SettleAsync();
    }
}
