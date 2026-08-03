# 72 — Snap-to-grid toggle

**What to build:** An end user who wants alignment help turns on snap-to-grid — a `SnapToGrid` parameter, toggled at runtime with Ctrl+' — and placement and move operations snap to the spacing of whichever grid layer is currently dominant. Off by default; ephemeral view state like selection, never persisted regardless of the layer it tracks. (ADR 0011, amending ADR 0009's shortcut table.)

**Blocked by:** 28 (Click-to-add placement), 30 (Drag-move an instance), 71 (Adaptive multi-layer grid)

**Status:** resolved

- [x] Snap is off by default; the `SnapToGrid` parameter and Ctrl+' both toggle it
- [x] With snap on, placement and drag-move land on the dominant grid layer's spacing
- [x] The snap spacing follows the dominant layer as zoom changes
- [x] Snap state is ephemeral — never serialized with the board
- [x] bUnit/xUnit coverage of snapping maths and the toggle

## Comments

`SnapToGrid` is a component-owned bindable bool parameter (`SnapToGridChanged` EventCallback, same
shape as an `<InputBase>`-style control) plus a host-settable `EnableSnapToGridShortcut` flag that
disables just the built-in Ctrl+' chord without touching the parameter itself. The chord reuses the
exact modifier/`isEditableTarget`-guarded shape every other shortcut in
`DiagramCanvas.razor.js`'s `addKeyboardListener` already has (`KeyZ`/`KeyG`/`BracketLeft`/
`BracketRight`).

"Dominant grid layer" reuses ticket 71's `VisibleGridLayers` math rather than introducing a second
notion of grid spacing: `GridLevelSplit()` (extracted from `VisibleGridLayers` so both share one
source of the `level`/`lowerLevel`/`upperWeight` computation) feeds a new `DominantGridSpacing()`,
which picks whichever of the two blended layers currently has the greater opacity — the same
"more opaque of the two" reading a user's eye would make. `SnapBounds` rounds a `Bounds`' `X`/`Y` to
the nearest multiple of that spacing; a no-op (returns the bounds unchanged) whenever `SnapToGrid`
is off, so every call site applies it unconditionally rather than branching on the parameter itself.

Wired into the two places the acceptance criteria name: `NewCenteredInstance` (shared by both
click-to-add and drag-and-drop placement, which have used one code path since tickets 27/28) and
`MoveComponent`'s single-instance branch. Deliberately **not** wired into multi-selection/group move
or resize — the ticket's own checklist says "placement and drag-move" (singular), its only
move-related blocker is ticket 30 ("Drag-move an instance", also singular), and snapping a
multi-selection's shared delta independently per member would break the "preserve relative offset"
guarantee `CommitGroupMove` exists for.

One shared-code side effect, evaluated and left as-is: `AddEdgeLabel` also calls
`NewCenteredInstance` to build a new edge label's placeholder instance, so with snap on its `Bounds`
gets snapped too - harmless, since `EdgeLabelStyle`'s own comment already establishes that an edge
label's `Bounds.X`/`Y` are never read again after creation (only `Width`/`Height` matter; the
label's actual on-screen position is always live-derived from the edge's current geometry). Adding
a bypass for this path would be speculative complexity for a value nothing ever reads.

A real (non-zero) drag can snap right back to an instance's own current position - `MoveComponent`
now skips pushing a `ChangeBoundsCommand` in that case, the same "skip a computed-but-unchanged
value" pattern `RestackSelection`/`ApplyZIndexChange` already use, so a no-op snap doesn't clutter
the undo stack. Covered by its own regression test
(`ASnapThatRevertsToTheCurrentPositionDoesNotAddAnUndoEntry`).

`/code-review` (Standards + Spec sub-agents) findings and how they were addressed:
- **Standards**: one hard violation - a new parameter's comment cited "(ADR 0011)", violating the
  repo's "never cite tickets/ADRs in comments" rule. Fixed by dropping the citation. One judgement
  call on an awkwardly-worded tie-break comment, reworded for clarity; a second on `MoveComponent`
  still calling `StateHasChanged()` unconditionally even on the new no-op skip path - left as-is,
  since that shape predates this change and the extra render is harmless.
- **Spec**: flagged that the new tests exercised `ClickToAdd` and drag-move snapping but never
  `HandleDrop` (drag-and-drop placement) directly, even though it shares `PlaceComponent`/
  `NewCenteredInstance` with `ClickToAdd`. Added
  `WithSnapOnDroppingAPendingPaletteDragSnapsToTheDominantGridLayerSpacing` to close that gap. Also
  raised the `AddEdgeLabel` shared-path side effect above as unexamined - addressed by documenting
  the reasoning here rather than changing the code. No missing or wrongly-implemented checklist
  items found otherwise.

Full `D12Canvas.Tests` suite passes (696 tests, 1 pre-existing skip). No Playwright visual-test run
needed - this ticket only touches `DiagramCanvas.razor.cs`/`DiagramCanvas.razor.js` interaction
logic; no `.razor` markup or shared `<style>` block changed, so no rendered visual state is new or
different at `SnapToGrid`'s default (off) value, per the layered-testing doc's own carve-out for
pure logic changes.
