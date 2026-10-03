# Theming the property panel

Type: grilling
Status: open
Blocked by:

## Question

Decide how `PropertyPanel` joins the token layer. Today it has none: five literals, no `--d12-*` declarations and no dark values, so it renders white on a dark host.

[Enumerate every hard-coded colour left in the library](47-hard-coded-colour-sweep.md) found this. ADR 0012's list of chrome elements never named the panel, and `d12canvas-next` ticket 74 missed it. [Chrome layout for the acceptance surface](39-app-chrome-layout-rework.md) now floats the panel over the full-bleed canvas, so it is the most visible case.

Decide:

- **Whether the panel declares its own four token blocks**, as `Palette` and `SelectionContextMenu` do, and whose values it copies. Its light literals equal `Palette`'s light tokens byte for byte, so copying `Palette` moves no light baseline. The canvas's values differ from both.
- **Whether that difference between canvas and floating-chrome values is a rule.** ADR 0012 calls each component's tokens a "copy", but the canvas and the floating panels disagree on surface, border and muted text in both themes. If floating chrome deliberately stands off the board, say so, since the property bar (ADR 0021) and the minimap will inherit it.
- **What the form controls do in dark.** Inputs, selects and `<input type="color">` take browser defaults. Whether the panel sets `color-scheme`, styles them through tokens, or leaves them alone.
- **Whether a dark-case baseline ships with it.** There is none today.
