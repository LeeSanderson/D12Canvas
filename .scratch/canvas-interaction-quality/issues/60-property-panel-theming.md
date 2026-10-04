# Theming the property panel

Type: grilling
Status: resolved
Blocked by:

## Question

Decide how `PropertyPanel` joins the token layer. Today it has none: five literals, no `--d12-*` declarations and no dark values, so it renders white on a dark host.

[Enumerate every hard-coded colour left in the library](47-hard-coded-colour-sweep.md) found this. ADR 0012's list of chrome elements never named the panel, and `d12canvas-next` ticket 74 missed it. [Chrome layout for the acceptance surface](39-app-chrome-layout-rework.md) now floats the panel over the full-bleed canvas, so it is the most visible case.

Decide:

- **Whether the panel declares its own four token blocks**, as `Palette` and `SelectionContextMenu` do, and whose values it copies. Its light literals equal `Palette`'s light tokens byte for byte, so copying `Palette` moves no light baseline. The canvas's values differ from both.
- **Whether that difference between canvas and floating-chrome values is a rule.** ADR 0012 calls each component's tokens a "copy", but the canvas and the floating panels disagree on surface, border and muted text in both themes. If floating chrome deliberately stands off the board, say so, since the property bar (ADR 0021) and the minimap will inherit it.
- **What the form controls do in dark.** Inputs, selects and `<input type="color">` take browser defaults. Whether the panel sets `color-scheme`, styles them through tokens, or leaves them alone.
- **Whether a dark-case baseline ships with it.** There is none today.

## Answer

Grilled 2026-10-04. Recorded as [ADR 0064](../../../docs/adr/0064-property-panel-theming-and-raised-values.md).

- **Token blocks.** The panel declares `Palette`'s four blocks with `Palette`'s values. No light literal moves. The root also gets `color: var(--d12-text)`, which closes a sixth gap the ticket missed: the panel set no text colour, so inputs inherited the host's.
- **The canvas-versus-floating difference is a rule.** The canvas declares **board values**, where the surface is the board background and the border is the grid line. Everything floating over the board declares **raised values** (`Palette`'s) on its own root: `Palette`, the context menu, the panel, the minimap and the property bar. The bar renders inside `.diagram-container` and still redeclares them, as the context menu does. The `#666`/`#6b6b6b` muted-text gap stays.
- **Form controls.** `color-scheme` joins the raised values, `light` or `dark` per block, so the browser draws the select popup, checkbox and spinner to match. Inputs set `background: var(--d12-surface)` and `color: inherit`. `Palette` and the context menu adopt `color-scheme` in the same change. The four light panel baselines move, because input text changes from black to `#212529`.
- **Baselines.** Two dark baselines: Rectangle, and Text with the dropdown. A new `PropertyPanelThemeTokensTests`, and the `Palette` and context-menu token tests assert `color-scheme` too. No override baseline.

`CONTEXT.md` gains a `Raised values` entry, and ADR 0012 points to ADR 0064.
