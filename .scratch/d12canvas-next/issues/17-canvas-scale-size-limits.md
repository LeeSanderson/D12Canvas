# Canvas scale and size limits

Type: grilling
Status: resolved

## Question

Is the board's coordinate space bounded or effectively infinite, and what governs the zoom range (min/max scale)? Specifically:

(a) should `Board` enforce a maximum extent on component placement (and on what basis), or is the coordinate space unbounded;
(b) what determines the minimum and maximum zoom scale exposed by `DiagramCanvas`, and should either bound be influenced by the DOM-rendering performance ceiling ([DOM-rendering performance ceiling for Blazor components](01-performance-ceiling-research.md)) or set purely on UX grounds (e.g., "zoomed out past X, individual components are illegible anyway");
(c) does windowing's overscan margin ([Virtualization/windowing mitigation stress test](02-virtualization-prototype.md)) fully absorb the risk of a very large or heavily zoomed-out board, or does scale need its own independent guardrail.

## Discussion

1. **Board extent.** Unbounded — no `Board`-enforced maximum, consistent with the "Miro-like canvas" destination; windowing already bounds render cost independent of extent.
2. **Min zoom (zoom-out).** No built-in floor — zoom-out is unbounded, pushing performance risk onto a separate guardrail rather than a scale limit.
3. **Zoomed-out guardrail.** Windowing filters by viewport position, not by how many entities that (potentially huge, at extreme zoom-out) viewport contains, so it doesn't fully absorb the risk on its own. Resolved as a level-of-detail (LOD) cutoff: below a pixel-size threshold, a component swaps to a generic placeholder built from existing registration data, instead of mounting its full interactive tree. No change to the component registration contract (ticket 05).
4. **Max zoom (zoom-in).** Host-configurable, unbounded by default — zooming in shrinks the viewport-intersecting set, so it isn't a performance concern and the library takes no opinion by default.
5. **Min zoom, revisited for symmetry.** Also made host-configurable, unbounded by default, matching the max-zoom parameter shape.
6. **Board extent, revisited for symmetry.** Considered a matching host-configurable cap; rejected — stays categorically unbounded with no knob, since nothing in the destination calls for one.
7. **LOD threshold configurability.** Host-configurable with a built-in default, consistent with the zoom parameters.
8. **Grid rendering breaks under unbounded zoom.** The current grid (a fixed 20px CSS `background-image`, scaled by the same CSS transform as zoom) only worked within the old fixed 0.6x–6x range (12px–120px on screen). Unbounded zoom needs an adaptive design.
9. **Grid purpose.** Primarily a visual/spatial reference; an optional snap-to-grid feature is layered on top rather than the grid existing solely to serve snapping.
10. **Grid layer progression.** Concurrent layers step by 10x spacing (20px, 200px, 2000px, ...); as zoom crosses a layer's legibility threshold, the next layer crossfades in while the old one fades out — a small, fixed set of rendered layers recycled to simulate infinite depth in either direction.
11. **Snap-to-grid persistence.** Ephemeral, session-only UI state, not part of `Board` — matches the `Selection` precedent (ticket 09); no persistence-format (ticket 07) or import/export (ticket 13) impact.
12. **Snap-to-grid granularity.** Snap distance always matches whichever grid layer is currently visually dominant, so it automatically coarsens/refines with zoom rather than using a fixed logical unit.
13. **Snap-to-grid toggle.** Both a bindable `SnapToGrid` parameter and a built-in `Ctrl+'` keyboard shortcut (host can disable the built-in shortcut). `Ctrl+'` avoids collision with every shortcut ticket 14 already established.

## Answer

`Board`'s coordinate space is unbounded — no enforced maximum extent, and no configuration knob for one; windowing already bounds render cost independent of how far a component is placed. `DiagramCanvas` exposes both min and max zoom scale as independent, optional host-configurable parameters, each **unbounded by default** — this replaces `ZoomPanTracker`'s hardcoded `MIN_SCALE = 0.6` / `MAX_SCALE = 6.0` outright; nothing about the old fixed range carries into this rethink.

Because windowing filters by viewport position rather than by count, an unbounded zoom-out on a dense board can still inflate the mounted-component count past the performance ceiling (ticket 01). The independent guardrail is a **level-of-detail (LOD) cutoff**: below a host-configurable pixel-size threshold (with a sensible built-in default), a component instance swaps its full interactive Razor tree for a single generic system-wide placeholder built from data the registration contract already requires (`DisplayName`/`Icon`) — no per-type custom placeholder, no change to ticket 05's contract. This is purely a `DiagramCanvas`-level rendering decision, computed from a component's own bounds and the current scale — no change to `Board`, `Board.GetVisible`, or the persistence format.

The existing single fixed-spacing grid can't survive unbounded zoom, so it's replaced by an **adaptive multi-layer grid**: concurrent layers stepping by 10x spacing, crossfading in and out as zoom crosses each layer's legibility threshold, recycling a small fixed set of layers to simulate infinite depth. This stays a purely visual rendering concern, with no tie to `Board`.

An optional **snap-to-grid** toggle is layered on top: off by default, ephemeral session-only UI state (not persisted, matching the `Selection` precedent), snapping to whichever grid layer is currently visually dominant. It's exposed both as a bindable `SnapToGrid` parameter and a built-in `Ctrl+'` shortcut, with a host-settable flag to disable the built-in shortcut. This adds one row to ticket 14's baseline shortcut table (see that ticket's Comments).

Seeded ADR [0011](../../../docs/adr/0011-unbounded-zoom-lod-and-adaptive-grid.md) and new `CONTEXT.md` terms (Grid, Snap-to-grid, LOD placeholder).
