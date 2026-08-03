# 71 — Adaptive multi-layer grid

**What to build:** An end user always has a position/scale reference: the grid runs concurrent layers stepping by 10x spacing, each crossfading in and out as zoom crosses its legibility threshold — simulating infinite depth in either zoom direction. Replaces the old fixed grid. Purely a rendering concern of the canvas: not board content, never persisted. (ADR 0011.)

**Blocked by:** 69 (Unbounded extent & zoom, configurable limits)

**Status:** resolved

- [x] Grid layers step by 10x spacing and render concurrently
- [x] Layers crossfade smoothly as zoom crosses each legibility threshold — no popping
- [x] The grid reads correctly at extreme zoom in both directions
- [x] The grid is not part of the `Board` and never persists
- [x] Screenshot cases at several zoom levels spanning at least two layer transitions

## Comments

The old single fixed-spacing grid (a `background-image` on `.canvas-content` itself, scaled for
free by that element's own CSS `transform`) is replaced by up to two `.grid-layer` divs plus a
`.grid-backdrop` div, all new siblings of `.diagram-canvas` under `.diagram-container`. Each
layer's `background-size`/`background-position`/`opacity` is computed in `DiagramCanvas.razor.cs`
from the current `ZoomPanTracker` scale/pan, rather than relying on a CSS transform, since the
layers must cover the whole viewport independent of `.canvas-content`'s own (still-present, still
board-content-sized) box.

`VisibleGridLayers()` treats the continuous quantity `level = -log10(scale)` as a fractional index
into an infinite sequence of 10x-apart layers (board spacing `20 * 10^level`) and renders exactly
the two neighbouring integer layers, weighted by linear interpolation on the fractional part - the
same "blend between two adjacent mip levels" technique continuous-LOD texture filtering uses. At
any exact integer level (e.g. the default scale 1.0) only one layer has non-zero weight, so it's
skipped down to a single rendered element - reproducing the legacy grid's exact look at the
default zoom pixel-for-pixel. `GridLayerStyle`'s `PositiveMod(pan, onScreenSpacing)` keeps each
layer's phase anchored to board coordinate 0 regardless of how far `PanX`/`PanY` have moved,
matching `ComponentInstance.Bounds`' own coordinate space.

Two rendering pitfalls surfaced only under the full Playwright suite (a single demo-page screenshot
never exercises them):
- `.grid-backdrop`/`.grid-layer`, if given `width:100%;height:100%;`, size against their own
  positioned ancestor (`.diagram-container`) - correct in principle, but on a real host layout
  (`GroupTabStopVisualTests`' `/placement-demo`, a flexbox container) this produced a fractional
  pixel width that rounded slightly differently than the legacy fixed-3000px-clipped-by-ancestor
  box did, silently dropping the rightmost grid column (measured 0.94% pixel diff against the
  pre-existing baseline). `calc(100% + 4px)` narrowed but didn't close this. `100vw`/`100vh`
  (oversized relative to the actual browser viewport, always clipped by `.diagram-container`'s own
  `overflow: hidden` the same way the legacy 3000px box was) measured byte-for-byte against every
  existing baseline. This carries the same "assumes the canvas doesn't exceed the viewport"
  characteristic the old fixed 3000px value had - not a new limitation, and out of this ticket's
  scope to generalize further.
- Splitting the old single element's combined `background-color` + `background-image` across two
  *separate* elements (a shared backdrop plus a color-less pattern layer) reintroduced sub-pixel
  anti-aliasing drift at gridline edges even when only one layer is active. Giving `.grid-layer`
  its own `background-color` too (redundant with, and painted over, `.grid-backdrop` whenever only
  one layer renders) restored pixel parity for that common case; `.grid-backdrop` alone still
  prevents the "washes toward white" artefact a bare crossfade would otherwise show where two
  partially-transparent layers overlap.

`.canvas-content` keeps its `width: 3000px; height: 3000px;` - ticket 69's comment called this box
"purely a rendering backdrop (the grid pattern's canvas)," which is now stale (the grid moved out
to the elements above), but the box has an unrelated, still-live purpose: it's what `.edges-layer`
(the SVG holding edges/floating-endpoints, sized `width: 100%; height: 100%;` of it) sizes itself
against. Removing it would collapse that SVG to 0x0, since every one of `.canvas-content`'s
children is `position: absolute` and contributes nothing to its own auto-sizing.

`/code-review` (Standards + Spec sub-agents) findings and how they were addressed:
- **Standards**: one hard violation - the new test files' class-summary comments cited "(ADR
  0011)", violating the repo's "never cite tickets/ADRs in comments" rule (this applies everywhere,
  not just `<style>` blocks). Fixed by dropping the citation and keeping the rest of the sentence.
  Judgement-call smells noted but left as-is: `.grid-backdrop`/`.grid-layer` share an identical
  `position`/`inset`/`pointer-events` declaration (Duplicated Code - no clean extraction exists in
  plain CSS without introducing a comment explaining the split, which style blocks forbid), and
  `VisibleGridLayers`/`GridLayerStyle` reach into `_zoomPanTracker` several times each (Feature
  Envy - an existing pattern `ContentStyle` already has, not introduced here).
- **Spec**: no missing or partially-implemented checkbox items and no scope creep found. Flagged
  the `100vw`/`100vh` sizing choice as arguably not tracking "the canvas's own rendered area" if a
  host's `DiagramCanvas` exceeds the browser viewport - addressed above (own comments section);
  also flagged `.canvas-content`'s 3000px box as looking vestigial given the grid moved out -
  addressed above (still load-bearing for `.edges-layer`, just for a different reason than ticket
  69's now-stale comment states).

Full `D12Canvas.Tests` suite (687 tests, 1 pre-existing skip) and the full `D12Canvas.VisualTests`
suite (58 tests, run in the pinned Playwright Docker image) pass. Every pre-existing visual-test
baseline's `.verified.html` picked up the same shared `<style>`/markup addition (new `.grid-backdrop`/
`.grid-layer` elements, the `.canvas-content` background rules moving out) with no other change -
expected fallout from editing shared canvas-chrome markup, per the precedent noted in
`DiagramCanvas.razor`'s own arrowhead-marker comment. Three baseline `.verified.png` files changed
pixels too, all as intended: `BoardRenderingVisualTests.ExtremeZoomIn`/`ZoomedAndPannedBoard` and
`LodPlaceholderVisualTests.DenseZoomedOutBoard` now show a legible adaptive grid at their existing
non-default zoom levels, instead of the old fixed grid's near-solid blur or barely-visible stretched
lines. New `AdaptiveGridVisualTests` adds four screenshot cases spanning a zoom-out transition
(default → layer 1 dominant) and a zoom-in transition (default → layer -1 dominant).
