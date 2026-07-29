# 52 — Per-edge routing styles & arrowheads

**What to build:** An end user chooses each edge's look individually — routing style (straight/orthogonal/curved) and arrowheads (none/start/end/both) are per-edge settings, never board-wide. Changing them is an undoable gesture and the settings persist with the edge. (ADR 0005.)

**Blocked by:** 48 (Drag port-to-port creates an edge)

**Status:** resolved

- [x] Each edge carries its own routing style; all supported styles render correctly
- [x] Each edge carries its own arrowhead settings; all combinations render correctly
- [x] Changing routing or arrowheads on one edge never affects another
- [x] Style changes are undoable gestures and round-trip through persistence
- [x] Screenshot cases per routing style and arrowhead variant

## Comments

`Edge` gained three settable properties per ADR 0005: `RoutingStyle: EdgeRouting {Straight,
Orthogonal, Curved}` (default `Straight`), `SourceArrow`/`TargetArrow: ArrowStyle {None, Arrow}`
(default `None`/`Arrow` - the front-loaded single-directed-arrow case). The `Edge` constructor keeps
its existing `(source, target, id)` positional shape with the three new params appended as optional,
so every pre-52 call site (production and test) still compiles unchanged.

Rendering (`DiagramCanvas.razor`/`.razor.cs`): Straight stays a plain `<line>` (preserving the
`x1`/`y1`/`x2`/`y2` contract `DiagramCanvasPortDragTests` already asserts on); Orthogonal/Curved
render as a `<path>` with a computed `d` (a 3-segment right-angle bend for Orthogonal, a
horizontal-tangent cubic Bezier for Curved) - `EdgePathD` builds the string with
`CultureInfo.InvariantCulture` throughout, since manual C# string interpolation doesn't get Blazor's
own attribute-binding invariant-formatting for free. Arrowheads are two SVG `<marker>` defs (default
and selected color, `orient="auto-start-reverse"` so one marker id serves both `marker-start` and
`marker-end`), gated on `Board.Edges.Count > 0` so a board with no edges renders byte-identical
markup to before this ticket - the shared `<defs>` block was originally unconditional and that
diff-blast-radius gap was caught by code review before it shipped (see below).

Undo/redo: `ChangeEdgeStyleCommand` (History) bundles the three fields as a before/after `EdgeStyle`
value (a new small record, mirroring how `ChangeBoundsCommand` already bundles X/Y/Width/Height into
`Bounds` rather than passing loose params) - `CONTEXT.md`'s Command entry is updated to list it in
the closed set. `DiagramCanvas.CommitEdgeStyleChange` is the wiring point, mirroring
`CommitPropsChange`'s exact shape - no UI calls it yet (ticket 56, the property panel, doesn't cover
edges and isn't built), so it's exercised directly by tests, same as `AddEdgeCommand`/
`RemoveEdgeCommand` were before ticket 50 gave edges a selection UI.

Persistence: `EdgeEnvelope` gained the same three fields with matching C# default values, so a board
saved before this ticket (missing them entirely) still deserializes to `Edge`'s own ADR 0005
defaults - same "field didn't exist yet" tolerance `BoardEnvelope.Groups`/`Edges` themselves already
rely on. No `JsonStringEnumConverter` added (ints), matching the existing untouched convention for
`PortId`.

`/code-review` (Standards + Spec sub-agents) findings and how they were addressed:
- **Standards**: no hard violations (domain vocabulary, ADR 0005/0007 shapes, and default values all
  matched exactly). Judgement-call smells raised and acted on: (1) *Data Clump* - the three style
  fields were originally passed as a raw `ValueTuple` through the command/commit-method signatures;
  replaced with the `EdgeStyle` record noted above. (2) *Undocumented command-taxonomy growth* -
  `CONTEXT.md`'s Command entry didn't list the new `ChangeEdgeStyleCommand`; added. (3) *Repeated
  Switches* - `EdgeRouting` is branched on both in the razor markup (line-vs-path element choice)
  and in `EdgePathD`'s switch; left as-is, since the markup branch is a structural element-type
  decision Razor can't express inside a C# switch, not a duplicate of the same logic, and collapsing
  it would mean giving up the `<line>` backward-compatibility this ticket deliberately kept.
- **Spec**: no missing requirements. One real gap caught: the shared `<defs>` markers block was
  originally unconditional, changing the rendered HTML for *every* `DiagramCanvas` instance
  regardless of whether its board had any edges at all - fixed by gating on `Board.Edges.Count > 0`.
  Flagged as partial-not-full: `CommitEdgeStyleChange` has no end-user-facing trigger yet (no panel/
  shortcut/context-menu entry) - noted above as expected, given ticket 56 doesn't cover edges.

Test coverage:
- `D12Canvas.Tests/EdgeTests.cs` - default styles, explicit constructor values, per-edge mutability.
- `D12Canvas.Tests/ChangeEdgeStyleCommandTests.cs` - apply/undo/redo, cross-edge isolation.
- `D12Canvas.Tests/DiagramCanvasEdgeRoutingAndArrowheadsTests.cs` - line-vs-path element choice per
  routing style, marker-start/marker-end presence per arrow combination, selected-color marker swap,
  two edges with different styles rendering independently, and `CommitEdgeStyleChange` end-to-end
  through undo/redo.
- `D12Canvas.Tests/BoardJsonSerializerTests.cs` - non-default round-trip, and an older-envelope
  (fields absent) defaulting test alongside the pre-existing missing-endpoint-instance test.
- `D12Canvas.VisualTests/EdgeRoutingAndArrowheadsVisualTests.cs` - a new dedicated demo page
  (`EdgeStylesDemo.razor`, since no interactive UI exists yet to set these on `/board-demo`, and
  `/board-demo` itself must stay edge-free for other tickets' own visual tests) pre-seeding every
  routing style and several arrow combinations; curated DOM assertions plus one full-page baseline
  screenshot, per spec.md's "small, curated" Playwright-layer guidance (the exhaustive combinatorial
  coverage lives in the bUnit tests above instead). Three pre-existing baselines
  (`PortDragVisualTests.ConnectedEdge`, `FloatingEndpointVisualTests.EdgeWithAFloatingEndpoint`,
  `EdgeSelectionVisualTests.SelectedEdge`) were regenerated (HTML+PNG) since the new
  `TargetArrow = Arrow` default changes their edges' rendered appearance - each was visually
  inspected before promoting.

Known unrelated issue found during this ticket, not fixed here: on this dev machine, most/all
Playwright visual-test baselines currently fail even on a clean, unmodified checkout of this
ticket's starting commit (confirmed via `git stash`) - the rendered page is missing its left nav
sidebar entirely versus the checked-in baselines. This is environment-specific baseline drift in the
same family as tickets 78-80, not caused by this ticket's changes (verified: `PaletteVisualTests`,
whose page never renders a `DiagramCanvas` at all, fails identically). Out of scope to fix here;
worth its own investigation ticket.
