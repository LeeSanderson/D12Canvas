# Arrowheads are paths each edge draws, not a shared marker

Edges get a line width (1–16), a line pattern, and seven arrowhead styles, some of them hollow. One shared `<marker id="edge-arrow">` can't carry that. Its size is fixed in user space, so it can't follow each edge's width. A hollow head needs the line to stop at the back of the head, and a marker can't shorten the line it sits on. A marker per (style, width) pair would churn `<defs>` on every width change, which is the same cost ADR 0016 rejected for one marker per colour.

**Each edge draws its own heads as `<path>` elements in `EdgeView`.** C# computes the path data from the `ArrowStyle` and the line width, then places and rotates it from the route's end tangent. The head length is `3w + 2`, with no size property. Each style has an inset from tip to back, and the painted line stops that far short of the endpoint. The route and the hit stroke still run to the true endpoint.

## Consequences

- The `<defs>` block and `marker-start`/`marker-end` go. ADR 0068's rule that `<defs>` renders only when the board has edges no longer applies. Baselines with edges move once, in the slice that makes this change. Baselines without edges don't move.
- Heads take their colour from CSS, `var(--d12-edge-override, var(--d12-edge))`, the same as the line. `context-stroke` (ADR 0016) is no longer used. Heads can be restyled by class again.
- Head paths carry the edge hit role, so a press on a head selects the edge. The halo draws under the heads as well as the line.
- `EdgeView` does more geometry: it needs the end tangent of every routing style. Orthogonal and straight routes end on a straight segment. Curved routes take the tangent from the last control point.

## Considered and rejected

- **A marker per (style, width) pair**, with both in the id: keeps markers but churns `<defs>` and needs a board-wide scan to know which ones to render.
- **`markerUnits="strokeWidth"`**: scales heads with width for free, but can't shorten the line for hollow heads, and it scales linearly. A width-16 line would get a head 64 long.
- **A size property, per edge or per end**: no diagram the effort looked at needed a head size that differs from what the width implies.
