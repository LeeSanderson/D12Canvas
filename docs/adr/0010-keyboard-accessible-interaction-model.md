# Full keyboard-driven interaction — reading-order tab stops, focus-follows-selection, zoom-relative nudge/resize, and Ctrl+Tab/Space multi-select

Beyond the baseline ARIA contract (ADR 0001's auto-applied `AccessibleName`, ADR 0006's `aria-selected`), the board is now fully operable without a mouse: every gesture ADR 0009 defined a drag/click equivalent for has a keyboard equivalent here.

**Tab order** visits every individual `ComponentInstance` in **reading order** — spatial, top-left to bottom-right by current on-screen position — not creation order and not `ZIndex`. A `Group` (ADR 0006) collapses to a **single tab stop**, positioned by its computed bounds; entering it to focus/select one member individually is a distinct gesture, left open the same way ADR 0006 already deferred "group-entry interaction" to implementation.

**Focus-follows-selection**: landing focus on an entity selects it outright — there is no separate commit step. This was chosen over the split model some canvas tools use (arrow-key cursor movement separate from an explicit Enter-to-select), because it keeps one concept ("what's focused" = "what's selected") instead of two that must stay in sync, and because D12Canvas has no existing notion of a focus ring distinct from selection to build on.

**Arrow-key move** nudges the focused/selected entity (single instance, ad-hoc multi-select, or `Group`, all per ADR 0006's shared bounding-box move semantics) by `1 / zoomScale` board units per press — this renders as exactly one screen pixel regardless of current zoom, rather than a fixed board-space amount that would look bigger or smaller as the user zooms. `Shift+Arrow` multiplies the step to `10 / zoomScale` (reads as ~10 screen px), for coarser movement.

**Arrow-key resize** (`Alt+Arrow`) is single-instance only — an ad-hoc multi-select or `Group`'s bounding-box has no keyboard resize; that stays mouse-only. Per axis, independently:
- Plain `Alt+Arrow`: the edge matching the arrow's direction moves outward by `1 / zoomScale`, growing that dimension; the opposite edge is the anchor and stays fixed.
- `Alt+Shift+Arrow`: the edge matching the arrow's direction is the anchor and stays fixed; the opposite edge moves toward it by `1 / zoomScale`, shrinking that dimension.
- Simultaneous arrows on different axes (e.g. `Alt+↑` + `Alt+→`) apply both axes' rules at once, each with its own independent anchor.

Resize has no larger-step variant — `Shift` is fully repurposed here as the anchor-flip modifier, not a magnitude multiplier, so every resize press is `1 / zoomScale` regardless of `Shift`.

**Keyboard multi-select** does not reuse `Shift+Tab` or `Alt+Tab` — both are reserved before they'd ever reach the page (`Shift+Tab` is the universal reverse-tab-order convention; `Alt+Tab` is an OS-level window-switcher intercepted before any keydown event reaches the browser). Instead, building a multi-select is two separate keys, mirroring how native OS file managers (Explorer, Finder) decouple "move" from "select" for exactly this reason:
- `Ctrl+Tab` moves focus to the next entity in reading order **without** triggering focus-follows-selection's auto-replace — a one-off suspension of that rule for this chord only.
- `Space` toggles the currently-focused entity's membership in the ad-hoc selection (add if absent, remove if present) — mirroring ADR 0006's shift-click toggle semantics via a different key, since `Shift` was unavailable.

**Mouse-free placement** falls out of existing behavior rather than needing new decisions: `CanvasPalette` entries (ADR 0001) are ordinary focusable/activatable controls, so `Tab` then `Enter`/`Space` triggers the same click-to-add path a mouse click would (ADR 0009's viewport-center-plus-cascade placement) — for both registered component types and the built-in `Connector` entry that creates a floating `Edge`.

**Mouse-free connector attachment** reuses the arrow-key move mechanics above, applied to a focused edge endpoint instead of a whole entity: once an `Edge`'s floating endpoint has focus, arrow keys nudge that endpoint; when it's within snapping distance of a port (ADR 0005), `Enter` commits the attachment — the keyboard equivalent of ADR 0009's drag-release-onto-a-port gesture.

**Addendum (surfaced while resolving the edge-attachment-without-a-named-port ticket):** ADR 0027 amends the port pick in one place and adds one cycle member. Entering the pick currently seeds `PortEndpoint(id, PortId.Top)` — an arbitrary side, guessed and then pinned, which is the defect 0027 fixes on the pointer side. The seed becomes `AutoPortEndpoint(id)`, so committing without naming a side gives an attachment that keeps choosing its own, and the tentative choice moves with the geometry instead of always pointing up. Arrow keys and `Space` still reach every pinned option, so nothing is lost, and no binding is added.

`Space` gains auto at the front of its cycle — auto, the four standard ports, then any custom ones, wrapping back to auto — because a state you can leave but not re-enter is a trap. The cycle list is built at the call site rather than inside `Board.AllPorts`, which `FindPortNear` shares and which must never see an entry with no fixed point to measure against.

This is what keeps the two input paths in parity: the pointer distinguishes pinned from auto by where the drag is released, the keyboard by how far the pick is drilled, and without the seed change the pointer could express an endpoint kind the keyboard could not.

**Considered and rejected:**
- **Split focus/selection with an explicit commit key** (the model some design-canvas tools use: arrow keys move a cursor, Enter selects) — rejected in favor of focus-follows-selection; it avoids introducing a second UI concept (focus ring vs. selection) with no existing counterpart in this design to hang it on.
- **Creation-order or `ZIndex`-order tab stops** — rejected in favor of reading order; spatial order is what a keyboard/screen-reader user actually perceives moving through the board, independent of when something was created or how it's layered.
- **Per-member tab stops inside a `Group`** — rejected; it would make several consecutive `Tab` presses visibly do nothing (each re-selecting the same already-selected group), which reads as broken rather than intentional.
- **`Shift+Tab` or `Alt+Tab` for multi-select** — rejected; both are captured by browser/OS convention before a page-level handler would ever see them.
- **A larger-step (`Shift`) variant for resize** — rejected; `Shift` is already fully committed as the resize anchor-flip modifier, and overloading it with a second, unrelated meaning (bigger step) would make resize presses ambiguous.
- **Keyboard resize for multi-select/`Group` bounding boxes** — deferred; proportional keyboard scaling of multiple members' bounds inside a resized bounding box adds real complexity (rounding, minimum sizes per member) nothing currently requires, and mouse-driven bounding-box resize already covers the case.

**Addendum (surfaced while resolving the keyboard parity ticket):** ADR 0026 amends this ADR in four places and supersedes ADR 0009's shortcut table.

**The arrow-key nudge step follows the grid when snap-to-grid is on** — one press moves to the next dominant grid line and `Shift` moves ten, which is one cell of ADR 0011's next coarser rendered layer. This costs the zoom-relative reasoning above nothing, which is the part worth reading twice: `SnapBounds` targets `DominantGridSpacing()` rather than a fixed 20 units, so the dominant cell is always between 6.3 and 63 screen pixels and the step stays screen-relative within a bounded range. A fixed board-space step would have reversed the reasoning; this does not. With snap off, the `1 / zoomScale` step above is untouched. Object snapping on a nudge was rejected outright, on ADR 0020's preview rule and because ADR 0014's align commands are the keyboard's exact answer to the same need.

**Arrow keys pan the viewport when the selection is empty, and `PanStep` is corrected.** That binding was live in code and in no ADR; ADR 0026 documents it, and in doing so found it moves 50 board units *undivided by scale*, so a press travels 200 screen pixels at 4x and five at 0.1x. It becomes `PanStep / scale`, on this ADR's own stated reasoning. `Shift` is ignored while panning, which is recorded rather than changed.

**The tab-stop model's silence about chrome is filled by two rules.** A chrome surface enters keyboard navigation only if it offers a capability no other keyboard route provides; where it does, canvas-rendered chrome takes a deliberate chord and host-placed chrome takes its natural tab order. So the minimap gets nothing and is `aria-hidden`, the property bar gets `Ctrl+Enter`, the context menu gets `Shift+F10`, and the palette's existing behaviour is now explained rather than incidental.

**`Ctrl+Tab` is recorded as suspect against this ADR's own reasoning.** The rejection above of `Shift+Tab` and `Alt+Tab` — captured by browser or OS convention before a page-level handler sees them — appears to apply to `Ctrl+Tab` as well: it is the tab-switching chord in Chrome, Edge and Firefox, and because every binding accepts `metaKey`, the Mac reading is `Cmd+Tab`, the OS application switcher. The existing tests invoke `OnCtrlTabPressed` directly and therefore cannot see it. The row is carried with the doubt attached rather than rebound, because every candidate replacement is reserved somewhere and, if none is free, the fix is reopening focus-follows-selection rather than changing a key.
