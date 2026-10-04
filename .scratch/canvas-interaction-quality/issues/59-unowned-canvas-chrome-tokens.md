# Tokens for the unowned canvas chrome literals

Type: grilling
Status: resolved
Blocked by:

## Question

Decide what each colour literal left on the canvas and the container becomes, for the ones no ADR owns yet.

[Enumerate every hard-coded colour left in the library](47-hard-coded-colour-sweep.md) lists them with their baselines. ADRs 0016, 0034, 0035 and 0041 own the rest. These remain:

- **Byte-identical to `--d12-accent`**: `.selection-bounding-box`, `.group-resize-handle`, `ComponentContainer`'s `.selected`. Swapping moves no pixel in either theme. Is there any reason not to?
- **Near-misses against `--d12-accent`**: `.resize-handle` (`#3498db`, about 25 baselines) and `.drag-over-affordance` (`#4a90d9` plus its `rgba` fill, one baseline). Every earlier tokenisation kept light byte-identical. Breaking that here is a correction that aligns two handle sets that look alike but aren't. Is that worth 25 baseline updates, or do these take escape hatches with the old values?
- **`.diagram-container`'s `#ccc` border.** The canvas's own `--d12-border` is much lighter. Swapping moves every canvas baseline. The dark theme currently gets a light-grey frame.
- **Roles with no token**: the port's green fill and white ring, the orange `.port-focused` ring (which ADR 0050 reuses for the provisional port), and `.floating-endpoint`'s orange fill and white stroke. Shared role tokens, per-element escape hatches, or left as fixed colours because they read on both backdrops? The port green equals `--d12-connector-preview` by value. Say whether that is a relationship or a coincidence.
- **`--d12-shadow`** has no dark value in `SelectionContextMenu`. Does it need one?

Also decide whether each swap ships with a dark-case baseline. The sweep found none of the edge, floating-endpoint or drag-over states screenshotted in dark.

## Answer

Grilled 2026-10-04. Recorded as [ADR 0063](../../../docs/adr/0063-canvas-and-container-chrome-colours.md).

- **Byte-identical accents.** `.selection-bounding-box`, `.group-resize-handle` and `.selected` read `--d12-accent`. No pixel moves. No reason not to.
- **Near-miss blues.** `.resize-handle` and `.drag-over-affordance` read `--d12-accent` too, the fill via `color-mix(in srgb, var(--d12-accent) 8%, transparent)`. About 26 light baselines move once. This is the first break of the byte-identical light convention, and it is justified because nobody chose those blues. Escape hatches would publish an accident as API.
- **Canvas frame.** A new escape hatch, `--d12-canvas-frame`: `#ccc` light, `#444` dark. The canvas's own `--d12-border` is the grid-line colour, the wrong role. Only dark pixels move.
- **Port green.** A relationship, not a coincidence: the port fill reads `--d12-connector-preview`. No pixel moves. The README notes both uses.
- **Orange and white.** They stay fixed literals on purpose and the ADR lists them. The orange must not become the accent, because a focused port sits inside an accent outline. Port focus and the floating endpoint share a value, not a role.
- **`--d12-shadow`.** It gets `rgba(0,0,0,0.5)` in both dark blocks, because the menu opens over the right-clicked instance, which can be dark.
- **Dark baselines.** None added. Every moved pixel is already in a baseline. Instead, a guard test asserts every colour literal in the canvas and container style blocks is a token declaration or on the fixed list. It ships with whichever of ADR 0016, ADR 0041 and this decision is built last, since the edge literals would fail it today.

Found on the way: the README, not ADR 0012, says escape hatches are declared once without a dark value. ADR 0012 says nothing about it.
