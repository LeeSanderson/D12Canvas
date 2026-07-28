# 49 — Floating endpoints

**What to build:** An end user drags from a port and releases over empty canvas: the edge is still created, its far endpoint left floating at that board point. A floating endpoint can later be dragged onto a port to attach it. (ADR 0005 — endpoints can float.)

**Blocked by:** 48 (Drag port-to-port creates an edge)

**Status:** resolved

- [x] Releasing over empty canvas creates the edge with a floating endpoint at the release point
- [x] A floating endpoint renders visibly and stays at its board point through pan/zoom
- [x] Dragging a floating endpoint onto a port attaches it
- [x] Dragging an attached endpoint off its port can detach it back to floating
- [x] Screenshot case for an edge with a floating endpoint

## Comments

New model shape (`D12Canvas/Model/`): `IEdgeEndpoint`, an empty marker interface implemented by
both `PortEndpoint` (unchanged shape, ticket 48) and a new `FloatingEndpoint(double X, double Y)`
record struct - matching ADR 0005's discriminated `EdgeEndpoint` at the value level without
disturbing `PortEndpoint`'s existing call sites (it's still constructed and compared exactly as
before, just now satisfies the wider interface). `Edge.Source`/`Target` widen from `PortEndpoint` to
`IEdgeEndpoint`. `Board.ResolveEndpoint` widens to match, pattern-matching to resolve a
`PortEndpoint` through its instance's live `Bounds` (unchanged) or a `FloatingEndpoint` to its own
fixed point (tracks nothing, by design).

Gesture design: rather than treat "create a new edge" and "reposition an existing edge's endpoint"
as separate gestures, both now share the same connector-drag state machine ticket 48 built
(`_isConnectingPort` et al.), gated by a new `_connectEditingEdgeId`/`_connectEditingEdgeIsSource`
pair. Pressing a port that `Board.FindEdgeAttachedTo` already resolves to an edge starts an
edit-in-place gesture instead of a new edge (matches common diagramming-tool UX - grabbing a
connected point moves the connection, grabbing a bare port starts a new one); pressing a floating
endpoint's own marker (`StartFloatingEndpointDrag`) always starts one. `CompletePortDrag` resolves
the drop to a port within `PortHitRadius`, falling back to a `FloatingEndpoint` at the drop point
when nothing is near enough - so a connector drag always produces a valid endpoint. While editing,
the edge's own `.edge-line` is suppressed in favour of the drag preview (`IsBeingEdited`), whose
fixed end is now the edge's OTHER endpoint (`ResolveOtherEndpoint`) rather than always the original
source port. `ApplyEdgeEndpointEdit` guards against collapsing an edge onto a single point (both
ends resolving to the same value) via plain record equality - covers both a port-vs-port and a
floating-vs-floating coincidence with one comparison, not just the port case.

Floating markers render as small SVG circles (`.floating-endpoint`) inside the existing
`.edges-layer`, in board-space `cx`/`cy` - the same mechanism that already lets `.edge-line` track
pan/zoom for free, so no new coordinate handling was needed. Each marker's own mousedown starts its
drag directly (`@onmousedown:stopPropagation`, since unlike a port press it isn't nested inside a
`ComponentContainer` needing the bubble-then-gate trick) - suppressed per-*side* (`IsEndpointBeingEdited`)
rather than per-edge while being dragged, so an edge with one attached and one floating end doesn't
hide the untouched floating marker while the other end is being re-dragged.

`/code-review` (Standards + Spec sub-agents) findings and how they were addressed:
- **Standards**: flagged `FindEdgeAttachedTo` as Feature Envy - it lived on `DiagramCanvas` but only
  ever read `Board.Edges`, with no `DiagramCanvas` state involved, unlike its sibling `FindPortNear`
  which already lives on `Board`. Moved there (`Board.FindEdgeAttachedTo(PortEndpoint)`), matching
  the established "Board owns Board-content queries" convention. The `(Guid, bool IsSource)` pair
  travelling across several new signatures was flagged as a Data Clump judgement call but left as-is
  - it follows the same paired-primitive-fields style the file's pre-existing connector-drag state
    (`_connectSourceComponentId`/`_connectSourcePortId`) already uses.
- **Spec**: caught that checkbox 2 ("stays at its board point through pan/zoom") was implemented but
  untested - added `AFloatingEndpointsBoardPointIsUnaffectedByZoom`. Caught that the self-loop guard
  only handled a port-vs-port coincidence, not a floating-vs-floating one - generalized to plain
  record equality (see above). Caught that suppressing both of an edge's floating markers while
  editing either endpoint was a (self-correcting but real) visual glitch - fixed via
  `IsEndpointBeingEdited`, with a regression test (`RepositioningOneEndpointDoesNotHideTheOtherEndsFloatingMarker`).
  No scope creep found: the reattach/detach gestures are the literal checkbox wording, and a direct,
  narrow extension of ticket 48's own port-drag gesture, exactly as ticket 48's own comments
  anticipated ("the floating half is left for ticket 49 to introduce along with whatever shape it
  actually needs").

Test coverage:
- `D12Canvas.Tests/BoardTests.cs` - `ResolveEndpoint` given a `FloatingEndpoint` (ignores Board
  contents entirely), and `FindEdgeAttachedTo` (source match, target match, unattached port, empty
  board).
- `D12Canvas.Tests/DiagramCanvasPortDragTests.cs` - updated
  `DroppingOnEmptyCanvasCreatesNoEdgeAndCancelsTheGesture` (ticket 48's placeholder behaviour) into
  `DroppingOnEmptyCanvasCreatesAnEdgeWithAFloatingEndpoint`, since ticket 48 explicitly deferred this
  case to this ticket.
- `D12Canvas.Tests/DiagramCanvasFloatingEndpointTests.cs` (new) - marker rendering at the drop point;
  the marker's board-space coordinates surviving a zoom; reattaching a floating endpoint onto a port;
  detaching an attached endpoint back to floating (asserts the edge count stays at 1 - detaching
  moves the endpoint, never creates a new edge); Escape mid-reposition leaving the edge unchanged;
  the self-loop guard; and the per-side marker-suppression fix above.
- `D12Canvas.VisualTests/FloatingEndpointVisualTests.cs` (new) - screenshot baseline for an edge with
  a floating endpoint, dragged from `/board-demo`'s Rectangle to an empty region of the seeded board
  (board-space `x < 120`, where no seeded instance ever sits, chosen so the drop point's emptiness
  doesn't depend on reasoning about every instance's individual bounds).

Investigated an unexpected wave of Playwright baseline failures (21 of 34 tests) after adding this
ticket's own CSS rule (`.floating-endpoint`) to `DiagramCanvas.razor`'s inline `<style>` block: since
that block is *literally rendered as part of the component's own DOM* (not Blazor's separate
scoped-CSS-isolation mechanism - no `.razor.css` file exists here), adding any rule to it changes
the captured HTML snapshot of *every* test that captures `DiagramCanvas`'s markup, exactly as ticket
48's own notes describe for its own CSS/markup additions. Diffed every failing HTML baseline
line-by-line and confirmed each one traces to exactly this one appended CSS rule, nothing else.
A handful of PNG screenshots also failed on top of that, including a large, real-looking one
(`PortsVisibleOnHover`, already flagged as historically one-off flaky in ticket 48's own notes) -
bisected via `git stash` against the unmodified tree (0/34 failures, twice) versus this ticket's tree
(21/34 failures, 4/4 runs) to confirm the PNG failures were real and reproducible, not noise. Running
the suite with test parallelism disabled (`dotnet test -- --parallel none`) made the large corrupted-
layout diff disappear entirely, leaving only a handful of single-pixel, single-channel antialiasing
diffs at one fixed nav-chrome coordinate (unrelated to canvas content) - consistent with the shared
single dev-server (`DemoAppFixture`, one process for the whole assembly) getting measurably slower to
serve concurrent WASM cold-boots as the app's own DLL grows with each new ticket, tipping an
already-fragile timing-sensitive screenshot (ticket 77, "ClickToAdd cold-boot container-measurement
race," is the filed, not-yet-resolved ticket for this exact class of hazard) over the edge more
reliably than before. Not fixed here - out of scope for a floating-endpoints ticket - but worth
flagging for whoever picks up ticket 77: this suite's flakiness rate under parallel execution got
measurably worse this round, and running visual tests sequentially is a practical (if not yet
documented in `README.md`) workaround. Promoted all 21 affected baselines (20 pre-existing +
this ticket's own new one) from a clean sequential run.

Full `D12Canvas.Tests` suite (334 tests) and the full `D12Canvas.VisualTests` suite (34 tests, run in
the pinned Playwright Docker image per `README.md`) pass, both sequentially and with default
parallelism.
