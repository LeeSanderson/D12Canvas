# Board extent and zoom stay unbounded; a level-of-detail cutoff guards zoomed-out performance, backed by an adaptive multi-layer grid with optional snap

`Board`'s coordinate space has no enforced maximum extent — consistent with the "Miro-like canvas" destination, and because the existing viewport-windowing mechanism (from the virtualization stress test) already bounds render cost independent of how far a component is placed. No configuration knob exists for extent; it isn't a scale/viewport concern the way zoom is, and nothing in this map's destination calls for a host to cap placement.

`DiagramCanvas` exposes both a minimum and maximum zoom scale as optional, independent parameters, each defaulting to unbounded. This replaces the current `ZoomPanTracker`'s hardcoded `MIN_SCALE = 0.6` / `MAX_SCALE = 6.0` constants outright — nothing about the prior fixed range needs to survive into this rethink. Zooming in shrinks the viewport-intersecting set and is purely a UX ceiling a host may choose to apply; zooming out grows it, and is where the performance ceiling (the DOM-rendering performance ceiling research) actually bites.

## Windowing alone doesn't bound zoomed-out risk

Viewport windowing filters by position — what's inside the (possibly very large, at extreme zoom-out) viewport rect — not by how many entities that produces. With no min-zoom floor, an unbounded zoom-out on a dense board can still inflate the mounted-component count past the ~200–500 concurrently-interactive ceiling the performance research established, independent of total board extent.

**Level-of-detail (LOD) cutoff:** below a host-configurable pixel-size threshold (a component's rendered size = its `Bounds` size × current scale), a component instance swaps its full interactive Razor component tree for a single generic system-wide placeholder — a plain box built from data the registration contract already requires (`DisplayName`/`Icon`/an accent color), not a per-type custom rendering. This needs no change to the component registration contract: no new field, no per-type authoring burden. The threshold has a sensible built-in default but is host-configurable, matching the zoom parameters.

This is purely a `DiagramCanvas`-level rendering decision — computed at render time from a component's own bounds and the current scale — with no change to `Board`, `Board.GetVisible`, or the persistence format.

## Adaptive multi-layer grid

The current grid (a CSS `background-image` cross-hatch at a fixed 20px spacing, itself CSS-transform-scaled with zoom) only stayed legible because zoom was bounded to 0.6x–6x (12px–120px on screen). Unbounded zoom breaks that: at extreme zoom-out the pattern collapses to a solid blur; at extreme zoom-in it stretches to a few, barely-visible lines.

Replaced with concurrent grid layers stepping by **10x** spacing (20px, 200px, 2000px, ... board units). As the zoom level crosses a layer's legibility threshold, the next coarser (or finer) layer crossfades in while the current one fades out — a small, fixed number of rendered layers recycled to give the effect of infinite depth in either zoom direction, rather than an actual unbounded set of concurrently-rendered layers. This stays a purely visual/rendering concern, exactly like today's grid — no `Board` tie, no persistence.

## Snap-to-grid

An optional, off-by-default toggle. When on, placement/move snaps to whichever grid layer is currently the visually dominant one — snap granularity coarsens or refines automatically with zoom, so there's one mental model ("what's on screen") rather than two ("visual grid" vs. "snap grid").

The toggle is **ephemeral, session-only UI state** — not part of `Board`, matching the precedent set for `Selection`: an interaction/view-state concept, not board content. It has no persistence-format or import/export impact.

Exposed two ways simultaneously: a bindable `SnapToGrid` bool parameter the host can read or set directly, and a built-in `Ctrl+'` keyboard shortcut that toggles it — with a separate host-settable flag to disable the built-in shortcut, in case it conflicts with a host's own bindings. `Ctrl+'` was chosen because it's unused by any shortcut in the baseline set (ADR 0009).

**Considered and rejected:**
- **A fixed maximum board extent** (in the spirit of today's fixed 3000×3000 canvas) — rejected; nothing about the destination needs one, and windowing already keeps render cost independent of extent.
- **A host-configurable extent cap, defaulting to none** — rejected for symmetry's sake alone; adding an unused config knob is speculative until a real host need for capped placement appears, at which point it's a small, isolated addition.
- **Tying min-zoom to the performance ceiling directly** (deriving a floor from expected density) — rejected in favor of no floor at all; the LOD cutoff addresses the actual cost driver (mounted interactive components) more precisely than an indirect scale-based guess.
- **A hard rendered-count cap** as the zoomed-out guardrail (mount only the first N components in the viewport, dropping the rest) — rejected; produces an abrupt "components are just missing" experience, where LOD degrades gracefully and doubles as a legibility aid.
- **Per-type custom LOD placeholders** (an author-supplied simplified `RenderFragment` per component type) — rejected; adds authoring burden to every component type for a case the generic placeholder already covers, and would amend the registration contract for no clear benefit yet.
- **Fixed built-in zoom constants** (keeping something like today's 0.6x/6.0x) — rejected outright; this map is a rethink of the current base framework, not a continuation of its specific numbers.
- **A 1-2-5 grid progression** (1x, 2x, 5x, 10x, 20x, 50x...) — rejected in favor of a straight 10x step; fewer distinct layers to manage across a given zoom range, at the cost of coarser steps between them.
- **Persisting snap-to-grid on `Board`** — rejected; it's an editing preference, not diagram content, and keeping it off `Board` sidesteps the already-locked persistence format and import/export design entirely.
- **A fixed logical snap unit independent of the visual grid** — rejected; would let the snap grid diverge from what's actually rendered on screen at extreme zoom, which reads as inconsistent behavior.
- **Host-wired-only snap toggle** (matching import/export's host-only trigger) — rejected as the sole mechanism; import/export was host-wired specifically because the trigger was inseparable from host-owned storage timing, a justification that doesn't apply to a pure interaction-state toggle. A built-in shortcut is added alongside the bindable parameter, not instead of it.
