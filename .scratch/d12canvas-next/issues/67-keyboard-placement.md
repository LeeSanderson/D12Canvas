# 67 — Keyboard placement

**What to build:** A keyboard user places a component entirely mouse-free: palette entries are reachable and activatable by keyboard, activation places the instance at the viewport centre (the click-to-add path), focus moves to the new instance, and arrow-key nudging positions it precisely. (ADR 0010 — placement via existing focus/nudge mechanics.)

**Blocked by:** 28 (Click-to-add placement), 64 (Arrow-key move)

**Status:** resolved

- [x] Palette entries are tabbable and activate on Enter/Space
- [x] Activation places the instance at the viewport centre with cascading offset
- [x] Focus lands on the newly placed instance, which is selected
- [x] Arrow keys then position it; the whole flow needs no pointer
- [x] bUnit coverage of the keyboard activation and focus hand-off

## Comments

The first two checklist items needed no new code: `Palette`'s entries are plain `<button
type="button">` elements (native tab stops, and Enter/Space already synthesize the same `click`
event a pointer would), and `HandleClick`/`ClickToAdd` (ticket 28) already place the instance at
the viewport centre with the cascading offset. A keyboard user's Enter/Space reaches that exact
same code path - there is no separate keyboard branch to wire.

What was missing is what a keyboard user needs that a mouse click-to-add never did: somewhere to
land. `ClickToAdd` now selects the newly placed instance directly (`SelectComponent(placed.Id,
addToSelection: false)`, the same hard-select a plain click already performs) and moves real DOM
focus to it once its tab stop exists in the DOM - mirroring `OnGroupPressed`'s own
`_pendingGroupFocus` pattern exactly, just keyed by the new instance's id
(`_pendingPlacementFocusId`) rather than a bare flag, since a freshly-placed instance's reading-
order slot among the current tab stops isn't knowable until the render that adds it actually
commits. `OnAfterRenderAsync` resolves it by looking the id up in `FocusableTabStopIds()` and
reusing the exact `focusTabStopAt(container, index)` JS call ticket 66 already established for
Ctrl+Tab - no new JS was needed.

`PlaceComponent` (shared by `HandleDrop` and `ClickToAdd` since ticket 28) now returns the placed
`ComponentInstance?` (`null` for the Connector sentinel, which produces an Edge instead) so
`ClickToAdd` has something to select/focus. `HandleDrop` discards the return value - mouse
drag-and-drop placement deliberately keeps its existing (no selection) behaviour, since the ticket
scopes the new select-and-focus behaviour to "the click-to-add path" reached by keyboard
activation, not drag-and-drop generally.

The Connector palette entry produces an Edge, which has no tab stop of its own (edges live in
`_selectedEdgeId`, never the instance-focused selection/tab-stop machinery) - `ClickToAdd`'s new
select/focus branch is skipped entirely for it (`placed is null`), same as before this ticket.

New coverage: `DiagramCanvasKeyboardPlacementTests.cs` (bUnit) - selection and the `focusTabStopAt`
call after a single `ClickToAdd`, that a second `ClickToAdd` moves both away from the first
instance, an arrow-key nudge moving the just-placed instance with no prior Tab/click needed, and
that click-to-add's new selection replaces whatever was previously selected.
`DiagramCanvasConnectorPaletteTests.cs` gained one regression case confirming a click-to-added
Connector still selects/focuses nothing. `PaletteTests.cs` gained one case driving the actual
`Palette` button's `Click()` (rather than calling `DiagramCanvas.ClickToAdd` directly) through to
the resulting `aria-selected`, so the activation entry point itself - not just the shared method
underneath it - is proven to select what it just placed.
No rendered markup or `<style>` block changed (only selection state and JS interop call
sequencing), so no Playwright visual-test coverage was added - the same call ticket 64/65/66 each
made for the same reason.
