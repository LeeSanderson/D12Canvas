# 69 — Unbounded extent & zoom, configurable limits

**What to build:** An end user never hits an artificial wall: board extent and zoom range are unbounded by default, replacing the prototype's fixed 3000×3000 extent and 0.6x–6x zoom constants. A host developer who needs bounds sets optional min/max zoom parameters, which the canvas honours. (ADR 0011.)

**Blocked by:** 22 (Canvas renders a Board), 25 (Windowed mounting + render discipline)

**Status:** resolved

- [x] No fixed board extent remains — content can be placed and panned to arbitrarily far coordinates
- [x] Zoom has no built-in floor or ceiling by default
- [x] Host-configurable min/max zoom parameters are honoured when set
- [x] Pan/zoom remain numerically stable at extreme (but realistic) coordinates and scales
- [x] xUnit/bUnit coverage of limit enforcement; screenshot sanity case at an extreme zoom

## Comments

`ZoomPanTracker` lost `CanvasWidth`/`CanvasHeight`/`SetCanvasSize` and the `MIN_SCALE`/`MAX_SCALE`
constants outright, along with `ApplyPanPositionConstraints` (the pan clamp that derived from
canvas size) - pan is now never clamped at all. `DiagramCanvas` no longer calls
`SetCanvasSize(3000, 3000)` on first render. Zoom bounds are exposed as `MinZoom`/`MaxZoom`
parameters on `DiagramCanvas` (both `double?`, default `null` = unbounded), forwarded to a new
`ZoomPanTracker.SetZoomLimits(min, max)` method every `OnParametersSet`. A tiny internal
`MinPositiveScale` floor (0.0001) guards `Scale` against ever reaching zero/negative under
sustained zoom-out with no host-set `MinZoom` - a numerical-stability backstop, not a product-facing
limit, since `Viewport` divides by `Scale`.

`.canvas-content`'s CSS `width`/`height` moved from an inline style driven by the now-deleted
`ZoomPanTracker.CanvasWidth`/`CanvasHeight` to a plain static `3000px`/`3000px` rule in the same
`<style>` block - a deliberate scoping call, not an oversight: this box is now purely a rendering
backdrop (the grid pattern's canvas), no longer tied to any pan/placement limit. Content still
places and pans correctly far past it (see
`DiagramCanvasBoardRenderingTests.AnInstanceFarBeyondTheOldFixedExtentStillMountsOncePannedIntoView`),
but the visual backdrop itself still visibly ends at 3000 board units - replacing it with a
genuinely unbounded background is ticket 71's adaptive multi-layer grid, per ADR 0011's own
staging ("purely a visual/rendering concern, exactly like today's grid"), not attempted here.

New coverage: `ZoomPanTrackerTests` (unbounded zoom by default, `MinZoom`/`MaxZoom` clamping and
immediate re-clamp on change, positivity/NaN/min-not-exceeding-max validation, unbounded pan in
every direction, `Viewport` staying finite at extreme scale+pan) and
`DiagramCanvasZoomLimitsTests` (bUnit - `MinZoom`/`MaxZoom` parameters honoured through the real
mouse-wheel path, runtime parameter changes re-clamping immediately). `BoardRenderingVisualTests`
gained `ExtremeZoomIn_MatchesBaseline`, a Playwright sanity screenshot at 10x zoom (90x `PageUp`,
well past the old fixed 6.0x ceiling) confirming rendering stays well-formed rather than actually
depicting anything legible (that's the LOD placeholder's job, ticket 70).

`/code-review` (Standards + Spec sub-agents) findings and how they were addressed:
- **Standards**: no hard violations, but flagged Duplicated Code (near-identical `MinZoom`/
  `MaxZoom` property setters) and a Data Clump (the pair travelling everywhere with no shared
  invariant). Addressed together: replaced the two settable properties with one
  `SetZoomLimits(min, max)` method that validates and assigns both atomically - this also fixed
  the Spec-side gap below, since one call site can no longer observe a transient invalid state.
  Also flagged the static `3000px` CSS backdrop as a magic number reintroducing something that
  reads like the deleted fixed extent - addressed by documenting the scoping decision above rather
  than silently leaving it unexplained.
- **Spec**: caught two real gaps. `MinZoom > MaxZoom` wasn't rejected (the max-clamp always ran
  last and silently won) - fixed by `SetZoomLimits` validating the pair together. `NaN` slipped
  past the `value <= 0` check (a well-known C# gotcha - relational comparisons against `NaN` are
  always `false`, so `<= 0` doesn't catch it, though `> 0` correctly excludes it, confirmed by a
  new test) - fixed via the same validation. Also noted no test placed/panned to content beyond
  the old fixed extent - added
  `AnInstanceFarBeyondTheOldFixedExtentStillMountsOncePannedIntoView` to close that.

Full `D12Canvas.Tests` suite (660 tests, 1 pre-existing skip) and the full `D12Canvas.VisualTests`
suite (53 tests, run in the pinned Playwright Docker image) pass.
