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

**Considered and rejected:**
- **Split focus/selection with an explicit commit key** (the model some design-canvas tools use: arrow keys move a cursor, Enter selects) — rejected in favor of focus-follows-selection; it avoids introducing a second UI concept (focus ring vs. selection) with no existing counterpart in this design to hang it on.
- **Creation-order or `ZIndex`-order tab stops** — rejected in favor of reading order; spatial order is what a keyboard/screen-reader user actually perceives moving through the board, independent of when something was created or how it's layered.
- **Per-member tab stops inside a `Group`** — rejected; it would make several consecutive `Tab` presses visibly do nothing (each re-selecting the same already-selected group), which reads as broken rather than intentional.
- **`Shift+Tab` or `Alt+Tab` for multi-select** — rejected; both are captured by browser/OS convention before a page-level handler would ever see them.
- **A larger-step (`Shift`) variant for resize** — rejected; `Shift` is already fully committed as the resize anchor-flip modifier, and overloading it with a second, unrelated meaning (bigger step) would make resize presses ambiguous.
- **Keyboard resize for multi-select/`Group` bounding boxes** — deferred; proportional keyboard scaling of multiple members' bounds inside a resized bounding box adds real complexity (rounding, minimum sizes per member) nothing currently requires, and mouse-driven bounding-box resize already covers the case.
