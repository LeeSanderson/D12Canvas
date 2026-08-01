# 68 — Keyboard connector attachment

**What to build:** A keyboard user connects two component instances entirely mouse-free: from a focused instance they enter port-focus, choose a source port, initiate a connection, move to the target instance/port via the same focus mechanics, and confirm — creating exactly the edge a pointer drag would have. Creation is undoable. (ADR 0010.)

**Blocked by:** 48 (Drag port-to-port creates an edge), 63 (Tab stops + focus-follows-selection)

**Status:** resolved

- [x] From a focused instance, ports can be focused and a source port chosen by keyboard
- [x] The target instance and port are reachable via focus navigation; confirming creates the `Edge`
- [x] The resulting edge is identical to one created by pointer drag (attachment, undo, persistence)
- [x] Escape cancels a half-built connection cleanly
- [x] bUnit coverage of the keyboard connection flow

## Comments

`Enter` drives a small state machine in `DiagramCanvas` (`_portFocusInstanceId`/
`_portFocusEndpoint`/`_pendingConnectorSource`), entirely separate from `_isConnectingPort` (the
mouse-drag gesture ticket 48 built): not currently picking a port, `Enter` enters port-focus mode on
whichever instance currently has real DOM focus, defaulting the pick to its Top port - a no-op for
anything that isn't a real `ComponentInstance` (nothing focused yet, or focus is on a Group's own
tab stop; groups have no ports). Already picking, arrow keys jump directly to one of the four
standard ports (`Top`/`Right`/`Bottom`/`Left` read naturally as `Up`/`Right`/`Down`/`Left`), and
`Space` instead steps to the next port in `Board.AllPorts`'s own order (every standard port, then
every custom one) - the only way to reach a custom port, since arrow keys only ever address the
four fixed sides. A first `Enter` while picking arms the currently-highlighted port as the
connection's source and exits port-focus mode, so native (never-intercepted) `Tab`/`Shift+Tab`
reaches the target instance exactly as any other focus-follows-selection navigation does; a second
`Enter` (reached once a source is armed) completes the connection via the same `AddEdgeCommand`
`CompletePortDrag` uses, including its "landing back on the exact port the drag started from
creates no edge" rule. `Escape` cancels both mid-pick and armed-source state in one press, matching
`OnEscapePressed`'s existing full-reset shape for every other piece of state it already touches.
`FocusEntity` clears `_portFocusInstanceId` unconditionally on every genuine focus-changing
navigation (Tab, click, Ctrl+Tab) - the one place all of them already pass through - so a pick
abandoned mid-flow (tabbed away without confirming) can never go stale.

The armed source's port stays visually highlighted (a new `.port-focused` CSS class, `#f39c12`
outline matching the existing floating-endpoint marker's color) across the Tab navigation to the
target, even though focus-follows-selection means that instance is no longer the selected/focused
one by then - without it there would be no way to see where a connection is coming from once you've
moved on to pick the target. `ComponentContainer` gained `FocusedPortId`/`FocusedCustomPortId`
parameters (mutually exclusive) feeding this, tracked in `ShouldRender`/`OnAfterRenderAsync` the
same way every other selection-driven bit of that component's state already is.

ADR 0010's own "Mouse-free connector attachment" paragraph describes a different mechanism (nudging
an *existing* edge's floating endpoint into port-snapping range via arrow keys + Enter) than this
ticket's own checklist, which describes picking a source port on one instance, navigating to a
target instance, and picking a target port - mirroring ticket 48's port-to-port drag rather than
ticket 49's floating-endpoint reattachment. Implemented the checklist literally rather than the
ADR's specific wording, the same narrower reading tickets 63 and 67 already took over their own
ADR/ticket text mismatches - reattaching a floating endpoint via nudge is a distinct gesture left
for whichever future ticket actually needs it.

`Board.AllPorts` (ticket 55) widened from `private static` to `public` (and from `static` to an
instance method, to avoid a `Board`-the-type/`Board`-the-property name collision at the call site)
so the keyboard gesture's `Space`-cycle reuses the exact same standard-then-custom port ordering
`FindPortNear` already relies on, rather than re-deriving it.

Two pre-existing, unrelated Playwright test failures were found and fixed along the way (not this
ticket's own regression - confirmed via `git stash` against a clean `main` checkout before touching
either): ticket 67's `ClickToAdd` now selects the instance it just places, which broke
`PortsVisualTests.PortsVisibleOnHover_MatchesBaseline`'s "not selected" precondition and
`SelectionContextMenuVisualTests.RightClickOnATwoInstanceSelectionOffersGroup_MatchesBaseline`'s
two-instance-selection setup (the second `ClickToAdd` silently replaced the first instance's
selection, so the subsequent shift-click on the already-selected top instance toggled it *off*
instead of adding it). Fixed by explicitly deselecting (`PortsVisualTests`) and by shift-clicking the
covered first instance's own exposed corner instead of the already-selected top one
(`SelectionContextMenuVisualTests`) - both were very likely already failing in CI on `main`. Left
`SelectionContextMenuVisualTests`'s own overlapping-instance setup un-extracted despite its
duplication with `ZIndexLayeringVisualTests`'s near-identical helper - a pre-existing smell, not
introduced here, and out of scope for this ticket to refactor.

Every existing Playwright HTML baseline needed re-promotion (`ComponentContainer.razor`'s shared
`<style>` block gained the new `.port-focused` rule - same "touches every rendered instance"
consequence tickets 47/48/55 already documented), verified diff-by-diff before promoting; a small
number also needed `tabindex`/`aria-selected` attribute-order and post-ticket-67
now-selected-by-default PNG re-renders folded in, all confirmed correct, not this ticket's own
regressions.

New coverage: `DiagramCanvasKeyboardConnectorAttachmentTests.cs` (bUnit) - pick+arm+confirm creates
the same edge a pointer drag would, undo/redo, the same-port no-op (mirroring ticket 48's own
case), Escape cancelling a half-built connection in one press, a stale pick clearing when focus
moves to another instance mid-pick, the armed source's highlight surviving that same navigation,
Enter on a Group's own tab stop being a no-op, Alt+Arrow not resizing mid-pick, and `Space` cycling
to a custom port (plus wrapping back to the first port) so a keyboard-built connection can use one
exactly like a pointer drag can.

`/code-review` (Standards + Spec sub-agents) findings and how they were addressed:
- **Standards**: hard violation - "(ticket 68)" cited verbatim in seven source comments across five
  files; stripped (kept the surrounding WHY content, which was genuine). Judgement calls addressed:
  `_pendingConnectorSource` was an ad-hoc `(Guid, PortId)` tuple duplicating `PortEndpoint`'s own
  shape (Primitive Obsession) - now a real `IEdgeEndpoint`, which is also what let `Space`-cycling
  and custom-port support drop in cleanly. `StandardPortCssClass` took a redundant `sideClass`
  parameter a call site could mismatch against its `PortId` (Data Clump) - now derived internally.
  Left `SelectionContextMenuVisualTests`/`ZIndexLayeringVisualTests`'s duplicated overlapping-
  instance setup as a documented, pre-existing smell (see above).
- **Spec**: caught a real gap - the keyboard flow only ever reached the four standard ports, so
  "identical to one created by pointer drag" didn't hold for a custom port (ticket 55), which a
  mouse drag can already attach to. Fixed via the `Space`-cycle mechanism described above, with new
  test coverage. Confirmed the ADR-mechanism-vs-checklist divergence is a reasonable, precedented
  interpretation rather than a spec gap.

Full `D12Canvas.Tests` suite (647 tests, 1 pre-existing skip) and the full `D12Canvas.VisualTests`
suite (52 tests, run in the pinned Playwright Docker image per README) pass.
