# 94 — Edge colour, selection halo and themed edges

**What to build:** Edges are readable on the dark theme by default, an end user can colour an edge, and a selected edge keeps its colour with selection shown as a halo under the stroke (ADRs 0016, 0041 halo half). `Edge colour` is a nullable field on `EdgeStyle`; null resolves to a new `--d12-edge` token, the first content-role token, at paint time via an inline rebind of an override custom property emitted only when non-null. Arrowheads follow the stroke via `fill="context-stroke"`, closing the live defect where marker content did not inherit the referencing stroke. Selection never repaints an edge: it adds a translucent accent halo beneath the edge's own stroke in the edge band. One optional envelope field, no schema bump. The property bar's edge rows (ticket 109) are what make colouring reachable from chrome; this ticket makes `ChangeEdgeStyleCommand` carry the colour.

**Blocked by:** 79 (Four paint layers)

**Status:** resolved

- [x] An uncoloured edge paints in the edge token and is visible on both themes; a coloured edge paints its colour on both
- [x] Selecting a coloured edge shows a halo beneath it and the stroke colour is unchanged; the arrowhead matches the stroke in both states
- [x] A board saved without edge colours loads unchanged and serialises byte-identically; a coloured edge round-trips
- [x] `ChangeEdgeStyleCommand` round-trips the colour through undo
- [x] bUnit asserts the token declaration, the override emission only when set, and the halo element
- [x] A demo page shows edges on a dark board for the visual suite, since none exists today; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Edge colour` and `Theme token` terms describe what shipped

## Comments

Built as specified. These calls were made along the way.

`Edge.Color` is a nullable CSS string, and its setter stores an empty string as null. `EdgeStyle` gains `string? Color = null` as ADR 0016 shows, and `ChangeEdgeStyleCommand` sets it with the other three. The default is a hazard for the property bar (ticket 109): an "after" snapshot built from three arguments clears an authored colour, so that caller should copy the edge's current colour.

`--d12-edge` is `#4a4a4a` on light, the old literal, and `#a0a0a0` on dark, declared in all four token blocks. A bUnit test computes its contrast against `--d12-surface` on both themes and requires at least 3:1. `.edge-line` paints `var(--d12-edge-override, var(--d12-edge))`, and `EdgeView` writes `style="--d12-edge-override: …"` only when the edge has a colour. Colour is part of `EdgeView`'s render key, so a colour change redraws the edge.

The two arrowhead markers became one, `edge-arrow`, whose path has `fill="context-stroke"`. The `.edge-arrowhead` rules, the selected marker and the `selected` class on the line are gone; `aria-selected` stays on the line.

A selected edge draws an `.edge-halo` copy of its line or path just before it in the edge band: accent, opacity 0.35, width 10 board units, round caps and joins, no markers. The halo scales with zoom like the line does. The 2-to-3 stroke-width bump is dropped, since the halo is now the non-colour cue. The halo of a later edge can paint over an earlier edge's line where they cross.

Colour is written to `EdgeEnvelope` only when set (`JsonIgnore` when null), so a board without edge colours serialises byte-identically, tested through both load paths. Colour values are not validated, the same as every other colour prop. A colour containing `;` from board JSON could add declarations to the line's inline style.

Baselines: the new `/edge-colour-demo` shows the same board on a light and a dark pane, with an uncoloured straight edge and two edges in `#e5246b`. It backs two baselines, one unselected and one with two edges selected on each pane, plus browser checks of the computed stroke colours before and after selection. Every HTML baseline with an edge moved for the marker change. The three with a selected edge (edge selection twice, labelled edge) also moved for the halo, and those PNGs moved. No other PNG moved. All 1470 bUnit tests pass (1 skipped). The full visual suite of 205 tests then passed in the pinned image under `-parallel none`.
