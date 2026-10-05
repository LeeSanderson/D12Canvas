# 87 — Object snapping, alignment guides and equal-spacing snap

**What to build:** An optional object-snapping toggle aligns the edges and centres of what the user drags to nearby shapes and draws a full-width guide along the match (ADR 0024). A host binds it beside snap-to-grid as a second parameter pair, off by default; the toggles are independent. Both resolve per axis: object wins where it fires and grid fills the other; Ctrl suppresses both. Nine anchor pairings per axis over the selection box as a rigid body; candidates are the on-screen entities including locked ones and LOD placeholders, excluding edges, floating endpoints and groups; tolerance 8 screen pixels; sticky to 1.75 times tolerance; skipped above 3 screen pixels per millisecond; the search runs twice per move so the guide describes where the selection now is. `Equal-spacing snap` is moves only, with candidates filtered by perpendicular overlap. Resize snaps point-wise for the moving edges. Placement and paste get grid only. `Alignment guide`s are full-bleed board-space lines in the guide `Paint layer`, at half intensity, reading a new content-role token that is not the accent, drawn with `vector-effect="non-scaling-stroke"`. The prototype on `prototype/snap-guides` is evidence, not code to merge.

**Blocked by:** 79 (Four paint layers), 86 (Live modifiers, axis lock and snap-to-grid on by default)

**Status:** ready-for-agent

- [ ] With object snapping on, dragging a shape near another's edge or centre pulls it to the match within tolerance and draws a guide spanning the viewport along it
- [ ] Grid snapping fills whichever axis object snapping did not fire on; Ctrl frees both
- [ ] Dropping a third shape beside two equally spaced ones snaps to the same gap
- [ ] A fast pointer sees no snapping; slowing down brings it back
- [ ] The guide reads its own token, not the accent, in both themes
- [ ] The host-facing toggle pair exists and the canvas menu row for it is added when ticket 89 lands (or here if 89 has already landed)
- [ ] Constants are asserted by ordering (threshold 4 < tolerance 8 < affordance floor < edge band 20), never by value
- [ ] Pure C# tests for the anchor search and equal-spacing candidates; gesture-object tests for per-axis resolution
- [ ] A demo page shows a guide mid-drag for the visual suite; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Object snapping`, `Alignment guide` and `Equal-spacing snap` terms describe what shipped
