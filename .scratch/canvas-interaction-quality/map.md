# Canvas Interaction Quality

Wayfinder map.

## Destination

A written spec (PRD) for a full interaction-quality pass on the D12Canvas canvas, backed by amended or superseded interaction-layer ADRs, and carried through to implementation tickets in this same effort. The seed was a list of ten felt defects and gaps from real use of `D12Canvas.App` — each now restated in the ticket that owns it — but the destination is deliberately wider than that list: the canvas should feel like a first-class direct-manipulation surface, not a component library that technically supports the gestures. Mouse and trackpad are first-class; touch and pen are not built, but must not be foreclosed.

## Notes

- Domain: a Blazor DOM/CSS diagramming canvas. Consult `domain-modeling` when naming what this effort introduces — there is currently no term for "the gesture that owns a pointer press", and `CONTEXT.md` will need one. Consult `codebase-design` when shaping the arbitration layer; it is a deep-module candidate replacing logic currently smeared across `stopPropagation` directives and nine `_isXxx` flags.
- **Reopenable:** ADRs 0005 (connector/edge model), 0006 (selection model), 0008 (property panel and layering), 0009 (tool modes and interaction model), 0010 (keyboard-accessible interaction). Supersede rather than accumulate contradictory amendments where a decision genuinely changes.
- **Settled, not open for re-litigation:** ADRs 0001 (registration contract), 0002 (chrome vs board content), 0003 (board state model), 0004 (persistence format), 0011 (unbounded zoom/LOD/grid), 0012 (chrome theming contract). Work within them.
- **Non-foreclosure constraint** — binds on the arbitration model ticket: the model must be expressible over pointer/touch input later without redesign. Touch is not being built; it is being kept cheap to add.
- Deliverable spans three projects: `D12Canvas` is the deliverable itself, `D12Canvas.Demo` gains pages so the Playwright suite can cover new visual states, and `D12Canvas.App` gets wired as the acceptance surface (its `BoardEditor` currently mounts neither `PropertyPanel` nor any of this effort's new chrome).
- Standing rule from `AGENTS.md` applies throughout and bites hard here: any change touching rendered markup or a shared `<style>` block requires a full Playwright visual-test run in the pinned Docker image with `-parallel none` before commit. Nearly every ticket in this map touches markup.
- Interaction quality resists paper specification. Prefer `prototype` tickets over `grilling` tickets wherever the real question is "how does this feel" rather than "what are the rules".

## Decisions so far

<!-- one line per closed ticket: enough to judge relevance, then open the link for the detail -->

- [What the browser actually delivers for trackpad input](issues/02-trackpad-input-research.md) — the pinch → `wheel` + synthesised `ctrlKey` convention is real in all three engines, but nothing distinguishes it from a genuine Ctrl+scroll and nothing needs to: both mean "zoom about the cursor", and deriving zoom multiplicatively from `deltaY` (`exp(-deltaY/100)`, Chromium's own inverse) rather than a fixed ±0.1 step handles both device classes in one path. No "gesture ended" signal exists in any engine — a short idle timeout is the portable answer, and it matters for undo granularity, not smoothness. Headline: **`@onwheel:preventDefault` is a silent no-op on the pinned .NET 9 runtime** (Blazor delegates to a passive-by-default `document` listener; the fix landed in 10.0-preview7 and was not backported), so the wheel listener moves into `DiagramCanvas.razor.js` as a non-passive listener on the container — which is the better design regardless of runtime. Pointer Events cannot see a trackpad gesture at all; `gesturestart` would double-count on Safari. Surfaced [Wheel-driven pan and zoom model](issues/17-wheel-pan-and-zoom-model.md).

## Not yet specified

- **Implementation tickets for every decision below.** This map follows `d12canvas-next`'s shape: design tickets resolve the fog and seed ADRs, a `spec.md` is written, then implementation tickets land in this same `issues/` directory. None can be phrased until the decisions they implement exist.
- **Cursor and micro-feedback vocabulary.** What cursor appears over each affordance, whether a drag shows a ghost or moves the real element, whether a dimension readout appears mid-resize. Blocked on the arbitration model and the port affordance model, since both change what affordances exist.
- **Render cost under live gesture geometry.** Tickets 01/02 of `d12canvas-next` established that Blazor re-rendering every mounted child was the real bottleneck, fixed via skip-if-unchanged. Making edges follow a drag live reintroduces per-mousemove render pressure on a different path. Whether that needs its own budget, or falls out of the live-geometry decision, is not yet clear.
- **Whether `D12Canvas.App`'s board editor needs a chrome layout rework.** A selection-anchored property bar, a minimap and a richer context menu may not compose with the current fixed 220px palette rail plus header. Can't be phrased until those three land.
- **Multi-select refinements.** Click-through into a `Group` to select one member (ADR 0006 explicitly punted this to implementation time), and "select all" semantics under grouping. Partly entangled with the clipboard model ticket; revisit once that resolves.

## Out of scope

- **Rotation, and any change giving `Bounds` an angle.** Ruled out while naming the destination: rotation propagates into hit-testing, port placement, edge geometry and the persistence format, which would reopen ADRs 0003 and 0004 — both of which this effort holds settled. Aspect-lock and proportional multi-resize were bundled with it and go out with it; either may return as a fresh effort.
- **Touch and pen as built, tested features.** Non-foreclosed by the arbitration model, but not delivered here — there is no device-testing story in this repo and standing one up is beyond this destination.
- **Real-time multi-user collaboration.** Inherited from `d12canvas-next`; unchanged.
