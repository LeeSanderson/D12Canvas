# Gesture release reliability: enumerate the leak paths

Type: task
Status: resolved

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

## Answer

Reproduced in a real browser against the running Demo app. The probe harness and its raw output are on branch `research/gesture-leak-probe` (`D12Canvas.VisualTests/GestureLeakProbe.cs`, output at `gesture-leak-report.md`, commit `15dedcf`). Thirteen probes; each drives one gesture, ends it the way a user plausibly would, then **moves the pointer with no button held** and records what still responds. A response to a buttonless pointer is a leaked gesture. Measurements below are from a 1280×900 viewport on `/placement-demo`.

**Every gesture on the canvas leaks. All six of them.**

| # | Gesture | Flags left set | Concrete trigger |
|---|---|---|---|
| 1 | Pan | `_isPanning`, `_panStart` | release anywhere outside `.diagram-canvas`'s subtree |
| 2 | Instance move | `ComponentContainer._isMoving`, `_moveStart`, `_moveStartX/Y` | drag an instance past the container's clip edge |
| 3 | Marquee | `_isMarqueeSelecting`, `_marqueeAnchor`, `_marqueeCurrent` | release over any `ComponentContainer` |
| 4 | Group move | `_isGroupMoving`, `_groupMoveAnchor`, `_groupMoveDeltaX/Y` | press inside the bbox on empty canvas, release outside the canvas |
| 5 | Group resize | `_isGroupResizing`, `_groupResizeAnchor`, the member-bounds snapshot | press a bbox handle, release outside the canvas |
| 6 | Connector drag | `_isConnectingPort`, `_connectSource*`, `_connectEditingEdgeId` | press a port, release outside the canvas |

### The headline: the leak needs no unusual release location at all

`HandleMouseDown` is `async` and **awaits a `getContainerDimensions` JS round-trip before setting `_isPanning`**. That await yields, so a fast enough press-release has its `mouseup` processed *first* — `HandleMouseUp` clears the flags, and `HandleMouseDown` then resumes and sets `_isPanning = true` on a gesture that is already over.

Probe 9 dispatches `mousedown` and `mouseup` synchronously in one JS turn on empty canvas, then moves the pointer with no button held: `translate(0px, 0px)` → `translate(104px, 40px)`. **An ordinary click on empty canvas leaves the canvas panning.** This is the path that matches the original report most exactly — "the press is registered, the release is missed" — and it is the one that is *invisible* until the user next moves the mouse, because nothing about the click looks wrong. It is also the only path with no geometric precondition whatsoever: it does not care where the release lands.

Static inspection could not have found this. It is not a missing listener; it is an ordering hazard created by the `await` on line 1917 of `DiagramCanvas.razor.cs`.

### The remaining paths, with what the user sees

- **Release outside `.diagram-canvas`** (probes 1, 6, 7, 8). Confirmed against the palette rail. The canvas keeps panning / the selection keeps moving / the group keeps resizing as the naked pointer moves.
- **Release over a different `ComponentContainer`** (probe 3). Confirmed, and worth stressing: this is **entirely inside the canvas — no host-page chrome is involved**. A shift+drag marquee released over an instance is swallowed by that container's `@onmouseup:stopPropagation`; the ancestor chain of the release target was `div.demo-note < div.container-content < div.component-container < div.canvas-content < div.diagram-canvas`. The dashed marquee rectangle stays on screen and keeps resizing *and rewriting the selection* on every buttonless move. This is the "haunted" case in its purest form. The connector drag is the one gesture immune to it, because `ComponentContainer.HandleMouseUp` forwards explicitly when `ParentCanvas.IsConnectingPort`.
- **Instance drag past the clip edge** (probe 2). `.diagram-container` is `overflow: hidden`, so a dragged instance stops being hit-testable the moment it is clipped, and the release lands on whatever host-page chrome is underneath. The instance then follows the naked cursor: `left: -78px` at release → `left: 351px` after a buttonless move. **Worse, `OnMoved` never fires**, so the move is never written to `Board` and never enters history — Ctrl+Z afterwards removed the instance entirely, undoing the *placement* it skipped straight past. The rendered position and the persisted `Bounds` are silently divergent for as long as the leak lasts.
- **Connector drag consumes the next click** (probe 8). After leaking, the green dashed preview trails the naked cursor, and the next ordinary click on empty canvas is taken as the drop point: 0 edges before, **1 edge and 1 floating endpoint after a single click the user meant as a deselect**. This is the only leak that silently *writes to the board*.

### Ruled out

- **The dead band below `.diagram-canvas`.** `.diagram-container`'s `grid-template-rows` leaves a 197px strip inside the bordered frame that is not `.diagram-canvas`. It is **not** a leak zone: `.canvas-content` is 3000×3000 and `.diagram-canvas` has no clip of its own, so the content overflows into that strip and still hit-tests as a descendant. `elementFromPoint` there returns `div.canvas-content`, and the release reaches the handler normally (probe 2 verdict: does not leak). Releasing anywhere in the visible frame is safe.
- **A right-press mid-drag as a *leak*.** It is the inverse bug. `HandleMouseDown` filters on `e.Button != 0`, but `HandleMouseUp` does not filter at all, so a right-button release **terminates an in-flight left-button pan early** — verified: the transform froze at `translate(80px, 40px)` and stopped tracking while the left button was still physically held (probe 10). The gesture dies rather than leaking, but the asymmetry between the two handlers is the same defect from the other side.

### Not reproduced in-harness (mechanism confirmed by absence)

Release outside the browser window, and focus loss / alt-tab. Headless CDP cannot faithfully model an OS-level window switch, so these are stated as mechanism, not measurement — but the mechanism is not in doubt. Grep-verified across `D12Canvas/`: **zero** uses of `setPointerCapture`; no `mouseleave`, `pointercancel`, `visibilitychange` or window-level `blur` handler anywhere (the only two `@onblur` bindings are the inline text editors in `StickyNote` and `Text`); and the only JS listeners registered are `keydown`/`keyup` on `window` and a `mousedown` capture listener for click-outside. Every gesture terminates solely via an `@onmouseup` on one element, so any release the page never sees leaves the flags set.

### Recoverability

- **Escape does not clear a leaked gesture** (probe 11). `OnEscapePressed` handles `_isConnectingPort` only. A leaked marquee survives it unchanged (1 before, 1 after); the connector preview clears (1 → 0). The one gesture with a working cancel key is the one gesture that also self-recovers on the next click.
- **A clean click inside the canvas does clear the `DiagramCanvas`-owned flags** (probe 12), because `HandleMouseUp` clears unconditionally. But recovery requires the release to land inside the canvas, and for `ComponentContainer._isMoving` it must land on *that specific container*.
- **State compounds before it recovers** (probe 5). A leaked marquee hijacks the *next* gesture: a plain unmodified drag on empty canvas — which should pan — instead redraws the marquee from the stale anchor and replaces the selection. Verified: the transform did not move, `.marquee-select` was visible throughout, and the selection was rewritten. `HandleMouseMove` tests `_isMarqueeSelecting` before `_isPanning`, and `HandleMouseDown` never clears the flags it is not about to set. The user presses on empty canvas expecting a pan and gets a selection box anchored where they were several gestures ago.

### Could existing Playwright coverage have caught these? No — structurally

Every drag test in the suite (`DragMoveVisualTests`, `MarqueeVisualTests`, `ResizeVisualTests`, `PortDragVisualTests`, `MultiSelectionMoveResizeVisualTests`) calls `Mouse.UpAsync()` at a point inside the element that started the gesture, and **none moves the pointer after release**. The two things that expose a leak — releasing somewhere else, and moving with no button held afterwards — appear nowhere in the suite. Ticket 15 needs "release elsewhere, then move with no button held, then assert nothing changed" as an explicit, named test shape; it is not a variation on the existing drag tests, it is a different assertion about a different moment.

### Side finding for tickets 18 and 06

The four standard ports and the four edge resize handles are **exactly concentric** (`.port-right` at `right: -10px` width 20; `.resize-handle.right` at `right: -5px` width 10 — same centre), and the handles render later in the markup, so they win the paint-order tie-break. `elementFromPoint` at a port's exact centre returns `div.resize-handle.right`; the port is only pressable off-centre (7px outboard worked). Ticket 18 owns precedence between overlapping participants and ticket 06 owns port/handle sizing — this is a live instance of exactly the "inherited from paint order rather than written down" problem ticket 18 describes.
