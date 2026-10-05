# 94 — Edge colour, selection halo and themed edges

**What to build:** Edges are readable on the dark theme by default, an end user can colour an edge, and a selected edge keeps its colour with selection shown as a halo under the stroke (ADRs 0016, 0041 halo half). `Edge colour` is a nullable field on `EdgeStyle`; null resolves to a new `--d12-edge` token, the first content-role token, at paint time via an inline rebind of an override custom property emitted only when non-null. Arrowheads follow the stroke via `fill="context-stroke"`, closing the live defect where marker content did not inherit the referencing stroke. Selection never repaints an edge: it adds a translucent accent halo beneath the edge's own stroke in the edge band. One optional envelope field, no schema bump. The property bar's edge rows (ticket 109) are what make colouring reachable from chrome; this ticket makes `ChangeEdgeStyleCommand` carry the colour.

**Blocked by:** 79 (Four paint layers)

**Status:** ready-for-agent

- [ ] An uncoloured edge paints in the edge token and is visible on both themes; a coloured edge paints its colour on both
- [ ] Selecting a coloured edge shows a halo beneath it and the stroke colour is unchanged; the arrowhead matches the stroke in both states
- [ ] A board saved without edge colours loads unchanged and serialises byte-identically; a coloured edge round-trips
- [ ] `ChangeEdgeStyleCommand` round-trips the colour through undo
- [ ] bUnit asserts the token declaration, the override emission only when set, and the halo element
- [ ] A demo page shows edges on a dark board for the visual suite, since none exists today; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Edge colour` and `Theme token` terms describe what shipped
