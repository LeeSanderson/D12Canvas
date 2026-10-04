# The canvas and container chrome read the accent, the canvas frame gets its own token, and the port and endpoint signal colours stay fixed

The colour sweep left a set of literals in `DiagramCanvas.razor` and `ComponentContainer.razor` that no ADR owned. ADR 0016 owns the edge stroke, ADR 0041 the selected edge, ADR 0034 the built-ins and ADR 0035 deletes `.edit-mode`. This decides the rest, and adds a test so the sweep does not have to be run again.

Every `ComponentContainer` mounts inside `.diagram-container`, the demo pages included, so a rule in the container's `<style>` block reads the canvas's tokens through the cascade and needs no fallback.

## Selection chrome reads `--d12-accent`

**`.selection-bounding-box`, `.group-resize-handle` and `ComponentContainer`'s `.selected` read `var(--d12-accent)`.** All three were `#2f80ed`, which is the accent in both themes, so no pixel moves. A host that retunes the accent today recolours the marquee and leaves the outline, the box and its handles behind.

**`.resize-handle` and `.drag-over-affordance` read it too, and their light pixels move.** They were `#3498db` and `#4a90d9`. Nobody chose either as a separate colour, and after the first swap the instance handles and the group handles would be two blues for one affordance. The drag-over fill becomes `color-mix(in srgb, var(--d12-accent) 8%, transparent)`, which keeps its old strength and adds no token.

This is the first break of the convention ADRs 0016 and 0034 follow, that a light value stays byte-identical. That convention protects colours someone decided. These were never decided, so making them escape hatches would turn an accident into public API. About 26 light baselines move once.

## The canvas frame is `--d12-canvas-frame`

**`.diagram-container`'s border reads a new escape hatch, `--d12-canvas-frame`: `#ccc` in light, `#444` in dark.** It is declared in all four of the canvas's token blocks. Light stays byte-identical. Dark loses the light-grey frame it has today.

The canvas's own `--d12-border` is the wrong token. It draws the grid lines and the LOD placeholder, at 10% black, and a frame that colour disappears into the grid. `#ccc` equals the palette's and menu's `--d12-border`, but that token lives on another root and means the edge of a floating panel. The dark value is set to sit on `#1e1e1e` about as `#ccc` sits on white, and is confirmed against the dark baselines when built.

A host that wants no frame, such as a full-bleed `D12Canvas.App`, sets the token to `transparent`.

## The port fill reads `--d12-connector-preview`

**The `.port` fill reads `var(--d12-connector-preview)`.** Both are `#2ecc71`, and that is a relationship: the dot you press and the line you drag out of it are one affordance. A host that retunes the preview line would otherwise get a green dot launching a line of another colour. No pixel moves. The README says the token colours ports as well as the preview line.

A separate `--d12-port` token was the cleaner name, but it gives one colour two public tokens, and defaulting one to the other on the same root runs into the specificity ADR 0012's override selectors already work around.

## The signal colours stay fixed

**`#f39c12` and `#ffffff` stay literals, on purpose.** They are:

- the `.port-focused` outline, which ADR 0050 reuses hollow for the provisional port
- the `.floating-endpoint` fill
- the white border on `.port` and stroke on `.floating-endpoint`

They carry meaning, not theme. The orange reads on `#fff` and `#1e1e1e` alike. It must not become the accent, because a focused port always sits inside a selected instance's accent outline and the orange is what separates the two. The white ring separates a dot from whatever is under it, usually author content in any colour, and does that on both backdrops.

Port focus and the floating endpoint share a value, not a role: one is the port being chosen, the other an edge end attached to nothing. Neither gets a token until a host asks. Listing them here is what stops the next sweep counting them as unowned.

## `--d12-shadow` gets a dark value

**`SelectionContextMenu`'s two dark blocks declare `--d12-shadow: rgba(0, 0, 0, 0.5)`.** Today dark inherits the light `0.15`, which barely shows on `#1e1e1e`. Over empty canvas the menu still stands off through its lighter surface, but it opens over the instance that was right-clicked, and over a dark instance only a 12%-white hairline is left. The README's line saying both escape hatches are declared once stops being true and is corrected.

## How this is verified

Per ADR 0025:

- Every pixel this decision moves falls in a baseline that already exists: the resize handles in light and dark, `DragAndDropPlacement.DragInProgress` in light, the frame in every dark canvas baseline, the shadow in the dark context-menu baselines. No new demo page is added. The missing dark edge, inline-editor and property-panel cases belong to ADRs 0016, 0034 and the property panel's own decision.
- The swaps that move nothing exist so that a retuned token reaches every piece of chrome, and no screenshot shows that. A guard test extends `DiagramCanvasThemeTokensTests`: over the `<style>` blocks of `DiagramCanvas.razor` and `ComponentContainer.razor`, every colour literal sits inside a token declaration block or is on this ADR's fixed list. `transparent` and `inherit` are not colours for this purpose.
- **The guard test ships with whichever of ADR 0016, ADR 0041 and this decision is built last.** The edge literals those two own are still in `DiagramCanvas.razor`, so the test cannot pass before then, and a "pending" allowlist would outlive its reason. The swaps here do not depend on the edge work and can land first.
- The test does not cover `PropertyPanel` or the built-ins. Their decisions extend it when they are built.

## Amends, confirms

- **Uses ADR 0012's escape-hatch rule** for `--d12-canvas-frame`, an element that needs to diverge from the shared `--d12-border`. ADR 0012 says nothing about whether an escape hatch has a dark value. Only the README says they are declared once, and after this decision only `--d12-connector-preview` is, so the README is corrected.
- **Extends `--d12-connector-preview`** to the port fill, without renaming it.
- **Confirms ADR 0050's** provisional port style, which keeps the fixed orange.
- **Departs from the byte-identical light convention** of ADRs 0016 and 0034 for two literals, for the reason given above. The convention stands for decided colours.

## Considered and rejected

- **Escape hatches holding `#3498db` and `#4a90d9`.** Keeps every pixel by publishing two colours nobody chose, and leaves two blues on one affordance.
- **The frame reading the canvas's `--d12-border`.** Wrong role: as faint as a grid line, and it moves every canvas baseline in both themes.
- **Deleting the frame and leaving it to the host.** Arguably the cleanest split under ADR 0002, but every existing host loses a frame it never asked to lose, and every baseline in both themes moves.
- **A `--d12-port` token with the preview defaulting to it.** Two public tokens for one colour.
- **The accent for port focus.** Merges the picked port into the selection outline around it.
- **One shared token for port focus and the floating endpoint.** Built on a matching value, not a matching role.
- **Escape hatches for the orange and white now.** Tokens nobody asked for become API to keep. Naming them as fixed solves the sweep problem as well.
- **A dark demo page for every swapped state.** Nothing these swaps change in dark is missing from an existing baseline.
- **Shipping the guard test now with the edge literals allowlisted as pending.** A temporary allowlist with no owner to remove it.
