# Full keyboard-accessibility interaction

Type: grilling
Status: resolved
Blocked by: 14

## Question

Beyond the baseline screen-reader-friendly component contract (ADR 0001's auto-applied ARIA, ticket 05) and the baseline `aria-selected` (ticket 09), design full keyboard-driven interaction with the board: tab order among component instances and groups, arrow-key move (and resize, possibly with a modifier key), and a keyboard-triggered equivalent to marquee/multi-select. Decide how these interact with the baseline shortcuts and placement/connector gestures resolved in ticket 14 — e.g. can a new instance or connector be created and attached without a mouse at all, and does keyboard focus order need to reflect z-order, grouping, or something else?

## Discussion

1. **Tab order.** Every component instance individually, in reading order (spatial, top-left to bottom-right) — matching Miro's convention ([help.miro.com](https://help.miro.com/hc/en-us/articles/11997028019858-Keyboard-navigation-while-working-on-boards)), not creation order or `ZIndex`.
2. **Focus vs. selection.** Considered Figma's split model (arrow-key cursor movement, Enter commits selection — [figma.com/blog](https://www.figma.com/blog/introducing-keyboard-accessibility-features/)) against a fused focus-follows-selection model. Chose fused: landing focus selects outright, no separate commit step.
3. **Group tab stops.** Since focus-follows-selection reuses ticket 09's "clicking any member selects the whole group," a `Group` collapses to one tab stop by its computed bounds — not one stop per member. Entering it to focus an individual member is a new, separate gesture, left open the same way ticket 09 deferred "group-entry interaction."
4. **Arrow-key move distance.** Zoom-relative: `1 / zoomScale` board units per press, so it always reads as one screen pixel regardless of zoom. `Shift+Arrow` = `10 / zoomScale`.
5. **Arrow-key resize** (`Alt+Arrow`), single-instance only (multi-select/`Group` bounding-box resize stays mouse-only). Per axis: plain `Alt+Arrow` grows — the edge matching the arrow's direction moves outward, opposite edge anchored; `Alt+Shift+Arrow` shrinks — the edge matching the arrow's direction is the anchor, opposite edge moves toward it. Simultaneous arrows on different axes apply both independently. Step is always `1 / zoomScale`; no larger-step variant, since `Shift` is fully repurposed as the anchor-flip modifier here.
6. **Keyboard multi-select.** `Shift+Tab` rejected (reserved reverse-tab-order convention). `Alt+Tab` also rejected — it's an OS-level window-switcher shortcut, intercepted before any keydown reaches the browser. Settled on `Ctrl+Tab` (move focus in reading order without triggering focus-follows-selection's auto-replace) + `Space` (toggle the focused entity's membership in the ad-hoc selection) — mirrors ticket 09's shift-click toggle via different keys.
7. **Mouse-free placement.** Falls out of existing behavior: `CanvasPalette` entries are ordinary focusable controls, so `Tab`+`Enter`/`Space` triggers the same click-to-add path as a mouse click (ticket 14), for both component types and the built-in Connector entry.
8. **Mouse-free connector attachment.** Once a floating edge endpoint has focus, arrow keys nudge the endpoint itself (reusing the move mechanics above); `Enter` commits attachment when within snapping distance of a port — the keyboard equivalent of ticket 14's drag-release-onto-a-port.

## Answer

Tab order visits every `ComponentInstance` individually in **reading order** (spatial, top-left to bottom-right), not creation order or z-order; a `Group` collapses to a single tab stop by its computed bounds, with per-member entry deferred to implementation (same status ticket 09 left "group-entry interaction" in).

**Focus-follows-selection**: focus and selection are the same concept — landing focus on an entity selects it, no separate commit step (rejected Figma's split cursor/Enter-to-select model as an unnecessary second concept).

**Arrow-key move** nudges the focused/selected entity (single, ad-hoc multi-select, or `Group`, per ticket 09's shared bounding-box semantics) by `1 / zoomScale` board units — reads as one screen pixel at any zoom. `Shift+Arrow` = `10 / zoomScale`.

**Arrow-key resize** (`Alt+Arrow`) is single-instance only. Per axis: plain `Alt+Arrow` grows in the pressed direction (opposite edge anchored); `Alt+Shift+Arrow` shrinks toward the pressed direction (that edge becomes the anchor). Diagonal combinations apply both axes at once. Always `1 / zoomScale` per press — no `Shift`-driven larger step, since `Shift` means anchor-flip here, not magnitude.

**Keyboard multi-select**: `Ctrl+Tab` moves focus in reading order without auto-selecting; `Space` toggles the focused entity into/out of the ad-hoc selection.

**Mouse-free placement and connector attachment**: palette click-to-add works via `Tab`+`Enter`/`Space` with no new mechanism needed; a focused floating edge endpoint is nudged by arrow keys and attaches via `Enter` when within snapping distance of a port.

Seeded ADR [0010](../../../docs/adr/0010-keyboard-accessible-interaction-model.md). No new `CONTEXT.md` terms — composes existing vocabulary (Selection, Group, Port, Edge, Gesture). This was the map's last remaining fog item — `Not yet specified` is now empty.
