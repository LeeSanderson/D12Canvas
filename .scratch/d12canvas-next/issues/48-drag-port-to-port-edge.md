# 48 — Drag port-to-port creates an edge

**What to build:** An end user presses on a port and drags to a port on another instance: a live drag-preview follows the pointer, and dropping on the target port creates an `Edge` entity connecting the two. The edge renders (straight-line routing for now) and stays attached through moves and resizes of either endpoint. Port-drag is a distinct gesture — it doesn't conflict with drag-move or click-select. (ADR 0005.)

**Blocked by:** 30 (Drag-move an instance), 47 (Standard ports visible)

**Status:** resolved

- [x] Dragging from a port shows a connector drag-preview; dropping on another instance's port creates an `Edge` on the `Board`
- [x] The edge renders and tracks both endpoint instances through move and resize
- [x] Starting a drag on a port never initiates an instance move
- [x] Screenshot cases: mid-drag preview and a connected edge
- [x] bUnit/xUnit coverage of edge creation and endpoint tracking

## Comments

New model types (`D12Canvas/Model/`): `PortId` (Top/Right/Bottom/Left), `StandardPorts` (the
fractional lookup ticket 47 deliberately deferred introducing), `PortEndpoint {ComponentId,
PortId}`, and `Edge {Id, Source, Target}`. Per ADR 0005's own scope note, `Edge.Source`/`Target`
are `PortEndpoint` directly rather than the eventual discriminated `EdgeEndpoint` (PortEndpoint |
FloatingEndpoint) — ticket 48 only ever creates port-to-port edges, so the floating half is left
for ticket 49 to introduce along with whatever shape it actually needs. Likewise no
`RoutingStyle`/arrows/`Label` fields yet (tickets 52/53). `Board` gained `Edges`/`AddEdge`/
`RemoveEdge`/`GetEdge` (mirroring the existing `Groups` triplet), `ResolveEndpoint(PortEndpoint)`
(resolves a port's live board point from its instance's *current* `Bounds` — never stored, so an
attached edge tracks move/resize for free, same trick as the ports themselves), and
`FindPortNear(point, tolerance)` (the connector-drag drop hit-test — ADR 0005 settled on discrete
named ports over nearest-point-on-perimeter, so a drop only resolves within tolerance of one of
an instance's actual four port points).

Gesture wiring: each port div gets its own `@onmousedown` (`ComponentContainer.razor`), calling
`StartPortDrag` which sets a local `_isPortDragging` flag *before* the same mousedown bubbles to
the container's own `HandleMouseDown` — same ordering trick the resize handles already rely on,
now also gating the move-arming branch so a port press can never start an instance move. Because
a completed connection spans two different instances, the rest of the gesture (live preview,
drop hit-test) is owned centrally by `DiagramCanvas`, not the originating `ComponentContainer`:
`OnPortDragStart` notifies it of the press, and for the remainder of the drag every
`ComponentContainer`'s own `HandleMouseMove`/`HandleMouseUp` checks `ParentCanvas.IsConnectingPort`
and forwards raw client coordinates instead of running its own drag logic (needed even when the
cursor never leaves the source instance's own bounding box, since `@onmousemove:stopPropagation`
would otherwise swallow the event before `DiagramCanvas` ever saw it). Edges and the drag-preview
render as plain SVG `<line>`s in a new `.edges-layer`, sitting before the component loop in markup
so they tuck behind shapes at the shared default z-index. `Escape` cancels an in-progress
connector drag — closing out a "later, it will also cancel..." comment `OnEscapePressed` already
carried since selection escape was first wired up.

Two real bugs surfaced and were fixed before this landed:
- **Race condition (real browser only, invisible to bUnit):** the first version of `StartPortDrag`
  awaited a JS interop call (fetching the container's page position) before setting
  `_isConnectingPort = true`. bUnit's mocked JS interop resolves synchronously so this looked
  fine there, but in a real Playwright/Chromium run the very next `mousemove`/`mouseup` could
  arrive before that await resolved, silently dropping the whole gesture (confirmed via a
  throwaway diagnostic test dumping live page state — `.connector-drag-preview` never appeared).
  Fixed by setting the connecting state synchronously and firing the origin refresh in the
  background instead (a stale origin only risks one barely-perceptible frame of preview
  misalignment, self-correcting on the next mousemove).
- **Inverted zoom-scale hit-tolerance**, caught by `/code-review`'s Spec pass: `CompletePortDrag`
  divided `PortHitRadius` by the current zoom scale before hit-testing, but `FindPortNear`'s
  comparison already happens entirely in board space (both the drop point and the ports'
  positions), where the port's own CSS-authored radius is already zoom-independent — the ancestor
  `.canvas-content` scale transform only changes its *painted* footprint, not this board-space
  size. Fixed by passing the constant directly; added a zoomed-in bUnit regression case.

Also spot-checked (real Playwright/Chromium only, not caught by bUnit): the drop point must land
strictly *inside* a port's rendered circle, not exactly on the target `Bounds`'s mathematical
edge — landing precisely on that boundary intermittently hit-tested to the element behind it
instead (`elementFromPoint` resolving to `.canvas-content`, not the port), a sub-pixel rendering
edge case pure model math can't see. `PortDragVisualTests` nudges its interaction points 1px
inward from the edge to stay reliably inside the port's own 10px hit radius.

Test coverage:
- `D12Canvas.Tests/EdgeTests.cs` — `Edge`/`PortEndpoint`/`StandardPorts` construction, identity,
  and fraction-lookup correctness.
- `D12Canvas.Tests/BoardTests.cs` — `Edges`/`AddEdge`/`RemoveEdge`/`GetEdge`, `ResolveEndpoint`
  tracking an instance through move and resize (and returning null for a missing instance), and
  `FindPortNear`'s tolerance/nearest-match/empty-board behavior.
- `D12Canvas.Tests/BoundsTests.cs` — `PointAtFraction` correctness.
- `D12Canvas.Tests/DiagramCanvasPortDragTests.cs` — full-gesture bUnit coverage: edge creation,
  the mid-drag preview line's coordinates, the created edge's rendered line, dropping on empty
  canvas or back on the start port creating nothing, a port press never arming a move (plain and
  Escape-mid-drag variants), an attached edge's rendered line tracking through both a real
  drag-move and a real resize gesture, and the zoomed-in regression case above.
- `D12Canvas.VisualTests/PortDragVisualTests.cs` — screenshot baselines against `/board-demo`'s
  Rectangle→Sticky Note pair for the mid-drag preview and the completed edge.
- Regenerated 17 other visual-test baselines across the suite: the new (empty when no edges
  exist) `<svg class="edges-layer">` and the ports' new `pointer-events`/`cursor` CSS appear in
  every rendered instance regardless of whether this ticket's own gesture is exercised, so any
  test capturing `.canvas-content` or `.port` changed its HTML snapshot. Diffed a representative
  sample line-by-line before promoting — every change traces to exactly this ticket's markup,
  nothing else moved. Two additional tests (`PortsVisibleOnHover`, `GroupResizeInProgress`, and
  later `StickyNoteInEditingState`) each threw a one-off PNG diff on a single run that didn't
  reproduce on retry — pre-existing rig flakiness (per ticket 47's own note on this rig), not
  something this diff introduced; none of their `.verified.png` files were touched.

`/code-review` (Standards + Spec sub-agents, run before commit) findings and how they were
addressed:
- **Standards**: no hard violations. Confirmed `AddEdge`/`RemoveEdge`/`GetEdge` correctly mirror
  the existing `Groups` triplet, the port-mousedown-bubble-then-gate idiom is a deliberate, valid
  reuse of the resize-handle pattern, and the fire-and-forget origin-refresh (a new idiom, not
  used elsewhere) is justified by an inline comment explaining the race it avoids. Flagged two
  purely-judgement-call smells (tuple proliferation consistent with the pre-existing
  `ToBoardPoint` convention; mild shape overlap between `EdgeLine`/`ConnectPreviewLine`) — left
  as-is, not worth a shared abstraction for two call sites.
- **Spec**: caught the inverted zoom-scale hit-tolerance bug above, and flagged that
  "tracks through resize" was only proven at the `Board`/model level, not integration-tested via
  an actual canvas resize gesture — addressed by both fixes described above. No scope creep
  found; confirmed the Escape-cancel addition is in-spec (closing a pre-existing forward-reference
  comment) rather than gold-plating.

Full `D12Canvas.Tests` suite (322 tests) and the full `D12Canvas.VisualTests` suite (23 tests, run
in the pinned Playwright Docker image per README) pass.
