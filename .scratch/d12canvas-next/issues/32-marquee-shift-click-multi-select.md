# 32 — Marquee + shift-click multi-select

**What to build:** An end user drags on empty canvas to draw a marquee: every component instance it *intersects* (not only fully contains) joins the selection. Shift-clicking an instance toggles its membership in the current selection. (ADR 0006.)

**Blocked by:** 29 (Click select, escape, and `aria-selected`)

**Status:** resolved

- [x] Dragging on empty canvas draws a visible marquee; screenshot case added
- [x] Marquee selection is intersection-based
- [x] Shift-click adds an unselected instance and removes a selected one
- [x] `aria-selected` is correct on every member of a multi-selection
- [x] bUnit coverage of intersection semantics and toggle behaviour

## Comments

Scope note: ADR 0006 also promises multi-select **move and resize** as a single bounding-box unit -
out of scope here (this ticket's own checklist only covers selection itself) and already tracked
separately as ticket 33 ("Multi-selection moves and resizes as one unit"), blocked on this ticket
resolving.

Selection changed from ticket 29's single `Guid? _selectedInstanceId` to a `HashSet<Guid>
_selectedInstanceIds` on `DiagramCanvas` - still transient view state (ADR 0006), never on
`Board`. `IsSelected` is a membership check; `SelectComponent(instanceId, addToSelection)` either
toggles the clicked instance in place (`addToSelection`, from a shift-click) or collapses the
selection down to just that one instance (a plain click, matching ticket 29's existing behaviour
extended to the multi-select case). `ComponentContainer.OnSelect` changed from a bare
`EventCallback` to `EventCallback<bool>` carrying `MouseEventArgs.ShiftKey` so `DiagramCanvas` knows
which mode to apply.

**A real interaction-model decision surfaced here, not pre-decided by ADR 0006 or ADR 0009, and
confirmed with the maintainer before implementing**: dragging on empty canvas was already a gesture
- it panned the canvas (mousedown/mousemove/mouseup on `.diagram-canvas`, present since before this
rethink). Marquee-select wanted the same gesture. Resolved as a three-way split on mousedown,
gated by where the press lands and whether Shift is held:

- **Shift+drag** on empty canvas draws the marquee - pairs with Shift-click's existing "I'm doing a
  multi-select gesture" meaning, rather than overloading a plain drag.
- **A plain drag** (no Shift) on empty canvas still pans, unchanged from before this ticket.
- **A plain drag starting inside the current selection's own combined bounding box** (but on empty
  space, not on top of either instance - the gap between two selected instances scattered apart) is
  deliberately inert: no pan underneath the selection, no new marquee. This is reserved for ticket
  33's actual group-move-as-a-unit mechanic, which is blocked on this ticket; ticket 32 only needed
  to make sure that gesture doesn't disturb the selection in the meantime. A *stationary* click at
  the same point (no movement) still clears the selection, per ADR 0006's "clicking empty canvas
  clears the selection" rule - only a real drag is specially reserved.

Arrow-key panning (`OnPanLeft/Right/Up/Down`, wired to `DiagramCanvas.razor.js`'s keydown listener)
and wheel zoom are both untouched throughout.

The marquee itself: `HandleMouseDown` (now `async Task`) fetches the container rect once via the
existing `getContainerDimensions` JS call - same reasoning `HandleDrop` already documents, the
container can move on the page between renders - and converts the mousedown point to board space
with a `ToBoardPoint` helper now shared with `HandleDrop` (previously duplicated inline math).
Whether that mousedown starts a marquee, a pan, or the reserved-inert mode is decided once, there,
from `e.ShiftKey` and `PointIsWithinSelectionBounds`. Each subsequent `HandleMouseMove` tick (while
marqueeing) re-converts the current point, builds a `Bounds` spanning the anchor and current point
(normalized via `Math.Min`/`Math.Abs` so drag direction doesn't matter), and replaces
`_selectedInstanceIds` outright with whichever `Board.Components` intersect it via the existing
`Bounds.Intersects` (ticket 24) - intersection, not full-containment, per ADR 0006, and not
additive, so a marquee that ends up over nothing empties the selection exactly like clicking empty
canvas does. This recomputation isn't throttled (unlike the pan branch, which keeps its pre-existing
`PanRenderInterval` throttle) since `ComponentContainer`'s own drag/resize gestures (tickets 30/31)
aren't throttled either, and it's the same cheap O(n) scan ticket 24 already established as the
current design point. A single `_dragMoved` flag (unifying what was ticket 29's `_panMoved` and
would otherwise have been a separate `_marqueeMoved`) is set by any of the three gestures on real
movement, and stops the native `click` that follows mouseup from clearing/disturbing whatever the
drag just did - regardless of which of the three modes it was.

The marquee rectangle itself renders as a `.marquee-select` div inside `.canvas-content`, positioned
with the same board-space `left/top/width/height` px convention every `ComponentContainer` already
uses - so it automatically tracks pan/zoom for free with no extra transform math, at the cost of its
1px border visually scaling with zoom (an accepted, minor cosmetic trade-off). The selection's
combined bounding box (`SelectedInstancesBounds`, used only to test the third gesture case) is
computed the same way ad-hoc, not persisted or rendered as its own affordance - that visual (and the
box's own drag-to-move behaviour) is ticket 33's to add.

bUnit coverage: `DiagramCanvasMarqueeSelectTests.cs` (Shift+drag renders/tracks/disappears,
intersection semantics including a partially-overlapped instance, drag-direction independence,
replacing rather than adding to an existing selection, zoom-aware board-space conversion, a plain
drag panning instead of marqueeing, a drag inside the selection bounds doing none of the three
disruptively while still suppressing the trailing click, a stationary click at that same point
still clearing the selection, shift-click add/remove toggle, and a plain click collapsing a
multi-selection). `ComponentContainerTests.cs` gained two cases asserting `OnSelect` carries the
click's shift state. `DiagramCanvasSelectionTests.PanningTheCanvasDoesNotClearAnExistingSelection`
needed no change in the end, since plain-drag-pans survived unchanged.

New Playwright visual coverage (`MarqueeVisualTests.cs`, reusing `/placement-demo`): a mid-drag
Shift+drag marquee (driven via `Page.Keyboard.Down("Shift")` around a real `Mouse.Down`/`Move`
sequence) spanning two click-to-added instances, and the resulting two-instance selection after
release. Baselines captured via the pinned `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble`
Docker image (via `podman machine start` + the `default` docker context already routed to it) per
the README process. As tickets 29-31 already noted, every *other* pre-existing visual-test baseline
in this repo currently fails in that same pinned image on an unmodified checkout too (a pre-existing
font/rendering-drift issue, not caused by or fixed in this ticket) - confirmed by inspecting the
`.received.png` output for each: none of the failures are attributable to this ticket's changes, only
this ticket's two new baselines were promoted.
