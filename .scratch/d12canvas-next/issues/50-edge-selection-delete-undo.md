# 50 — Edge selection, delete & undo

**What to build:** Edges join the selection model: an end user clicks an edge to select it (visible affordance, `aria-selected`), presses Delete to remove it, and undo/redo covers both edge creation and deletion — restoring attachments exactly.

**Blocked by:** 38 (Undo/redo placement & delete), 48 (Drag port-to-port creates an edge)

**Status:** resolved

- [x] Clicking an edge selects it, with a visible selection affordance and `aria-selected`
- [x] Delete removes the selected edge
- [x] Creating an edge is undoable; undoing removes it, redo restores it with the same attachments
- [x] Undoing an edge delete restores it with both endpoints (attached or floating) intact
- [x] Screenshot case for a selected edge

## Comments

Edge selection lives in its own exclusive slot (`_selectedEdgeId` on `DiagramCanvas`), never mixed
into `_selectedInstanceIds` - edges don't participate in multi-select, marquee, or grouping (ADR
0006 only covers component instances), so a separate slot avoids polluting `ExpandedSelection`/
`IsMultiSelected`/etc. with edge ids. Selecting an edge clears any instance selection and vice
versa (`SelectComponent`, `UpdateMarqueeSelection`, `HandleCanvasClick`, `OnEscapePressed` all clear
`_selectedEdgeId`); the two selection kinds are mutually exclusive by construction, so `Delete`
needs only a two-way branch in `OnDeletePressed` (edge vs. the existing expanded-instance path),
no `CompositeCommand` needed since only one edge can ever be selected at a time.

The `.edge-line` `<line>` element gets a click handler (`SelectEdge`) plus `aria-selected` and a
`.selected` CSS class swap for the affordance - it also needed `pointer-events: auto` added, since
it previously inherited `pointer-events: none` from `.edges-layer` (only ports and floating-endpoint
markers had opted back in before this ticket).

Edge creation (`CompletePortDrag`'s bare-port-origin branch) was a direct `Board.AddEdge(...)` call
before this ticket - never undoable. Routed through a new `AddEdgeCommand` (mirroring
`AddEntityCommand`/`GroupCommand`'s existing Apply/Undo shape); edge deletion gets the matching
`RemoveEdgeCommand`. Both just hold the same `Edge` reference, so undo/redo restores identical
attachment state (attached or floating) rather than a stale copy - covered by both pure xUnit tests
(`AddEdgeCommandTests`, `RemoveEdgeCommandTests`, including a floating-endpoint case) and bUnit
gesture-level tests in `DiagramCanvasUndoRedoTests`.

`/code-review` (Standards + Spec sub-agents) findings and how they were addressed:
- **Standards**: flagged `AddEdgeCommand`/`RemoveEdgeCommand` as a third near-identical typed
  Add/Remove command pair (after `AddEntityCommand`/`RemoveEntityCommand` for `ComponentInstance`
  and `GroupCommand`/`UngroupCommand` for `Group`), against `CONTEXT.md`'s "small closed set...
  not one bespoke class per gesture type" wording for `Command`. Left as-is: `GroupCommand`/
  `UngroupCommand` already established this exact "one typed pair per entity kind" precedent (ADR
  0007 itself admits they're "thin wrappers... despite being separate classes"), so this isn't a
  new deviation this ticket introduced; genericizing now would mean refactoring that already-
  shipped code too, beyond this ticket's scope. Flagged for a possible future command-set-
  unification ticket instead of acted on here.
- **Spec**: no gaps, no scope creep, no implementation errors found - all five checkboxes verified
  against test coverage, including both the attached and floating endpoint cases for the undo-after-
  delete requirement.

Test coverage:
- `D12Canvas.Tests/AddEdgeCommandTests.cs`, `RemoveEdgeCommandTests.cs` (new) - Apply/Undo/redo
  command semantics, including a floating-endpoint attachment case.
- `D12Canvas.Tests/DiagramCanvasEdgeSelectionTests.cs` (new) - click-to-select, aria-selected +
  `.selected` class, selection exclusivity both directions (component vs. edge), Escape and
  empty-canvas-click clearing.
- `D12Canvas.Tests/DiagramCanvasDeleteSelectionTests.cs` - Delete removes a selected edge without
  touching its endpoint components.
- `D12Canvas.Tests/DiagramCanvasUndoRedoTests.cs` - undo/redo of edge creation (port-to-port) and
  edge deletion (both attached-attached and floating-endpoint cases).
- `D12Canvas.VisualTests/EdgeSelectionVisualTests.cs` (new) - screenshot baseline for a selected
  edge on `/board-demo`, clicking the exact midpoint between the two ports the edge was dragged
  between (always lands on the line itself regardless of slope).

Every CSS addition to `DiagramCanvas.razor`'s shared inline `<style>` block re-triggers every
Playwright visual test's `.verified.html` snapshot (that block is rendered as literal page markup,
not Blazor scoped CSS - same effect ticket 49 documented for its own CSS addition). Promoted all
21 affected pre-existing baselines plus this ticket's own new one from a clean sequential run
(`dotnet test -- --parallel none`, inside the pinned Playwright Docker image). A handful of PNG
baselines also showed sub-pixel diffs on top of that (`DragMoveVisualTests.DragInProgress`,
`MultiSelectionMoveResizeVisualTests.PersistedGroupBoundingBoxVisible`,
`PortsVisualTests.PortsVisibleOnHover`, `ResizeVisualTests.HandlesVisible`) - visually inspected
each pair side-by-side (no discernible content difference) and confirmed the failing subset varies
across repeated runs with no correlation to anything this ticket touches, consistent with the
known, still-open cold-boot/measurement-race flakiness ticket 77 documents. Promoted alongside the
rest.

Full `D12Canvas.Tests` suite (355 tests) and the full `D12Canvas.VisualTests` suite (35 tests, run
in the pinned Playwright Docker image per `README.md`) pass, confirmed twice in a row.
