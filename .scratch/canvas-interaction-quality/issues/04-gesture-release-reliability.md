# Gesture release reliability: enumerate the leak paths

Type: task
Status: open

## Question

Reported from real use: drag state frequently survives the mouse button being released — the press is registered, the release is missed, and the canvas keeps dragging or panning as the pointer moves with no button held.

Reproduce this and enumerate every path by which a gesture can leak, with a concrete trigger for each. The output is evidence, not a fix: ticket 01 designs the arbitration model, and a guaranteed release path is the single hardest property that model must provide. Designing it without knowing exactly how the current arrangement fails would be guessing.

Static inspection already establishes the shape of the problem. There is no `setPointerCapture`, no `mouseleave` fallback, and no document- or window-level `mouseup` listener anywhere in `D12Canvas` — every gesture terminates solely via an `@onmouseup` bound to one specific element. Suspected leak paths to confirm or rule out:

- Release outside the bound element — off the canvas, over the header or palette rail, or outside the browser window entirely.
- Release over a *different* `ComponentContainer`. Its `@onmouseup:stopPropagation` swallows the event before `DiagramCanvas` sees it. Connector drags forward across this boundary explicitly (`HandleMouseUp` checks `ParentCanvas.IsConnectingPort`); pan, marquee, group-move and group-resize do not.
- A right-press or context menu opening mid-drag.
- Focus loss — alt-tab, window switch, the browser stealing focus.
- Async ordering in Blazor's event dispatch, where a fast release may be processed against stale state.
- Whether an already-leaked gesture is recoverable — does the next `mousedown` clear it, or does state compound?

For each confirmed path record: the trigger, which flags are left set, on which component, and what the user then observes. Note especially any path where the leaked state is *invisible* until the user next clicks, since those are the ones that feel like the canvas is haunted rather than broken.

Also record whether existing Playwright coverage could ever have caught these — ticket 15 needs to know, and the answer is likely no, since a scripted drag always releases cleanly inside the element.
