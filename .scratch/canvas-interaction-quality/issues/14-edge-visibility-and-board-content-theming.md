# Edge visibility and board-content theming

Type: grilling
Status: open

## Question

Decide how edges get their colour — and, behind that, whether board content has any theming story at all.

The seed note reports connectors being hard to see against dark backgrounds. The immediate cause is plain: `.edge-line` in `DiagramCanvas.razor` is `stroke: #4a4a4a`, a hard-coded mid-grey that never joined the token layer. Ticket 74 of `d12canvas-next` swept the remaining chrome into `--d12-*` tokens and did not touch edges.

The reason it did not is the actual question. ADR 0012 draws a hard line: theme tokens style *library-owned chrome*, while per-instance visual fields are ordinary `TProps` business data with no separate theming model. `CONTEXT.md` restates it as an explicit "avoid". An `Edge` is board content — a persisted entity in `Board.Edges` — so by that rule it should carry its own colour as data, not read a token. But its colour has never been exposed as data either, so it currently gets neither.

Decide:

- Which side of ADR 0012's line an edge falls on. It is board content that is nonetheless rendered entirely by library chrome, with no author-supplied component behind it — genuinely unlike both a sticky note and the grid.
- If tokens: how a user-set per-edge colour would coexist with a themed default later, without ADR 0012 having to be reopened a second time.
- If data: where the colour lives on `Edge`, how it defaults, how it round-trips through ADR 0004's envelope, whether it is panel-editable, and what happens to boards already persisted without it.
- **The wider problem this exposes.** The same argument applies to every built-in component type. A sticky note's yellow, a rectangle's fill, a text component's colour are all `TProps` fields with light-mode-shaped defaults (ticket 10 of `d12canvas-next` put visual fields in props deliberately). On a dark theme they are all wrong, not just edges. Decide whether that is in scope here, a separate ticket, or an accepted consequence of ADR 0012 that this effort records and leaves.
- Whether selected-edge and arrowhead colours, and the `--d12-connector-preview` escape hatch that ticket 74 already created for the drag preview, come out consistent with whatever is chosen.
- Contrast as a requirement rather than an aesthetic: whatever is decided should state how an edge stays legible against an arbitrary user-chosen component fill, which is not a theme problem at all.
