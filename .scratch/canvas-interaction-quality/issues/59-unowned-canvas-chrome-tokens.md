# Tokens for the unowned canvas chrome literals

Type: grilling
Status: open
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
