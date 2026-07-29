# 53 — Edge labels

**What to build:** An end user adds a label to an edge: rich content embedded on the edge itself — not a separate board entity — defaulting to text edited in place like other on-canvas text. The label rides the edge, staying positioned along it as endpoints move. (ADR 0005 — a label has no existence independent of the edge that owns it.)

**Blocked by:** 43 (Inline WYSIWYG text editing), 48 (Drag port-to-port creates an edge)

**Status:** resolved

- [x] A label can be added to an edge and edited in place (default: text)
- [x] The label stays positioned along the edge as either endpoint moves
- [x] The label is embedded on the edge — deleting the edge removes the label; no separate entity exists
- [x] Label edits are undoable gestures and the label round-trips through persistence
- [x] Screenshot case for a labelled edge

## Comments

`Edge` gained a `Label: ComponentInstance?` property (default `null`), per ADR 0005's "full
`ComponentInstance`, embedded, not a separate entity" shape. Double-clicking an edge's own line
(when it has no label yet) creates a default (empty) `"text"` instance centered on the edge's
current midpoint, via a new `ChangeEdgeLabelCommand` (History) - the `Edge` counterpart to
`ChangeEdgeStyleCommand`, swapping the whole `Label` reference (or `null`) rather than one of its
fields. Editing the label's own text afterwards is *not* a new mechanism - it reuses ticket 43's
existing inline WYSIWYG editor unchanged (`Text.razor`'s own dblclick-to-edit, committing via
`ParentCanvas.CommitPropsChange`), just resolved through a different lookup: `Board.FindEdgeLabel`
finds a label's live `ComponentInstance` by its own id across every edge (a label has no
`Board.Components` entry to `GetComponent` by), and `CommitPropsChange` now falls back to it before
giving up. Deleting the edge (`RemoveEdgeCommand`) removes the label for free - it was never
tracked anywhere except on the `Edge` object itself, so there's no separate cleanup path to get
wrong.

**Live positioning, never persisted:** the label's own `Bounds.X/Y` are ignored for rendering -
only `Width/Height` matter. Its rendered position is recomputed every render as the straight-line
midpoint between the edge's two *current* resolved endpoints (`EdgeLabelStyle`), the same
"derive from live geometry, never store it" trick ports and floating endpoints already rely on -
so it rides along automatically through both a committed move/resize and (caught by `/code-review`'s
Spec pass, see below) an in-progress endpoint-reposition drag. The anchor is the straight-line
midpoint regardless of `RoutingStyle` (Straight/Orthogonal/Curved) - a deliberate approximation,
not full on-path placement, since the ticket only asks for "stays positioned along the edge," not
pixel-exact placement on a bent path.

Persistence: `EdgeEnvelope` gained a nullable `Label: ComponentInstanceEnvelope?` (default `null`),
reusing the exact same `ToComponentEnvelope`/`FromComponentEnvelope` helpers `Components` already
uses - no new (de)serialization logic. `FromEdgeEnvelope` and `DeserializeEdgesPartial` became
instance methods (were `static`) since resolving a label's `Props` needs the registry-resolved
`FromComponentEnvelope`, mirroring how `FromComponentEnvelope` itself already needs `_registry`. An
edge whose label fails to parse (e.g. an unknown component type) is treated as a fully malformed
edge entity, same as any other nested parse failure in this file - no finer-grained "edge loads,
label warning recorded" path, since the ticket doesn't ask for one and `ParseEntries`'s existing
catch-all already behaves this way for a malformed `Group`/`Component`.

`/code-review` (Standards + Spec sub-agents) findings and how they were addressed:
- **Standards**: one hard violation - this file itself hadn't been updated to `resolved`/checked
  off/commented (fixed by this section). One judgement-call smell acted on: `AddEdgeLabel`
  duplicated `PlaceComponent`'s "resolve registration → fallback size → center a new
  `ComponentInstance` on a point" shape; extracted into a shared `NewCenteredInstance` helper both
  now call. A second, minor smell (`EdgeLabelStyle` re-resolving `EdgeLine` already computed once
  per edge in the SVG loop above it) was left as-is per the reviewer's own "low-priority" framing -
  the label loop is necessarily separate (labels are ordinary HTML `DynamicComponent`s, not SVG
  shapes), so sharing that one cheap computation isn't worth entangling the two loops. `CONTEXT.md`'s
  Command entry was updated to list `ChangeEdgeLabelCommand` in the closed set.
- **Spec**: one real gap caught, not cosmetic - the label was only positioned from
  `EdgeLine(edge)` (the edge's last-*committed* endpoints), so while an existing edge's endpoint was
  mid-drag (ticket 49's reposition gesture, which suppresses the normal line in favour of a live
  `ConnectPreviewLine`), the label stayed frozen at the stale pre-drag midpoint - visually detached
  for the drag's duration. Fixed: `EdgeLabelStyle` now reads `ConnectPreviewLine()` instead of
  `EdgeLine(edge)` while `IsBeingEdited(edge.Id)` is true, exactly mirroring how the edge's own
  line already switches source during a drag. Added a dedicated regression test
  (`TheLabelFollowsTheLiveDragPreviewWhileAnEndpointIsBeingRepositioned`) asserting the label's exact
  rendered position both before and mid-drag. Also flagged (accepted, out of scope): ADR 0005 says
  "any registered type can serve as a label," but only the "defaults to text" half is reachable -
  there's no UI to add a label of a different type. The ticket's own checklist only asks for the
  text default, so this is intentionally deferred, not a gap.

Test coverage:
- `D12Canvas.Tests/EdgeTests.cs` - `Label` defaults to `null`, keeps an explicitly-provided value,
  independently mutable per edge.
- `D12Canvas.Tests/ChangeEdgeLabelCommandTests.cs` - apply/undo/redo for both adding
  (`null` → instance) and removing (instance → `null`) a label, cross-edge isolation.
- `D12Canvas.Tests/BoardTests.cs` - `FindEdgeLabel` finds a label by its own id, across edges, on
  an empty board, and when no edge has a matching label.
- `D12Canvas.Tests/BoardJsonSerializerTests.cs` - serialize includes the label, round-trip rebuilds
  it (`Props`/`Bounds`/`ComponentTypeKey` intact), an edge with no label stays `null`, and an
  older envelope missing the `Label` property entirely still deserializes (defaults to `null`).
- `D12Canvas.Tests/DiagramCanvasEdgeLabelTests.cs` (new) - full-stack bUnit coverage: double-click
  creates a default Text label (no-op if one already exists), adding a label is its own undoable
  gesture, the label's position tracks both a committed endpoint move and (the bug above) a live
  reposition drag, editing the label's text commits/undoes/redoes exactly like any board Text
  instance, deleting the edge removes the label and undo restores both together, and a full
  `BoardJsonSerializer` round-trip of a labelled edge.
- `D12Canvas.VisualTests/EdgeLabelVisualTests.cs` (new) - reuses `/board-demo`'s existing
  Rectangle→Sticky Note port-drag (same pairing `PortDragVisualTests`/`EdgeSelectionVisualTests`
  already use) to create an edge, double-clicks it to add a label, types visible text into it, and
  screenshots the result.

Full `D12Canvas.Tests` suite (406 tests, 1 pre-existing unrelated skip) passes. The full
`D12Canvas.VisualTests` suite was run in the pinned Playwright Docker image
(`mcr.microsoft.com/playwright/dotnet:v1.61.0-noble`, per this repo's README): 36/40 pass,
including the new `EdgeLabelVisualTests` baseline. The remaining 4 (`FloatingEndpointVisualTests`,
`PortDragVisualTests.ConnectedEdge`, `EdgeSelectionVisualTests`,
`EdgeRoutingAndArrowheadsVisualTests`) were independently confirmed - via a clean-checkout control
run of the same tests with this ticket's changes stashed out - to already fail identically without
any of this ticket's changes (a whole-page layout/scoped-CSS-hash drift, missing the left nav
sidebar entirely; same family as tickets 78/81's already-tracked, unresolved environment-drift
issue). Their baselines were deliberately left untouched rather than papered over. 19 other
pre-existing baselines *were* regenerated (HTML only, one - `ResizeVisualTests.HandlesVisible` -
also a 5-pixel/1000000 anti-aliasing-level PNG diff, confirmed negligible) purely to absorb the new
`.edge-label` CSS text appended to `DiagramCanvas.razor`'s shared `<style>` block - present in every
rendered `DiagramCanvas` regardless of whether a given test's board has any edges/labels, same
"any shared markup/CSS edit re-triggers every Playwright HTML snapshot" situation tickets 48/52
already document. (Aside, not fixed here: several of those 19 pre-existing `.verified.html` files
were already stale from ticket 52 too, missing its arrowhead CSS entirely before this ticket's own
regeneration caught them up - pre-existing drift, not introduced here.)
