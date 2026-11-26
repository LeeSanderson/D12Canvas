# Connector style props

Type: grilling
Status: resolved
Blocked by:
Map: [Better standard components](../map.md)

## Question

What style props does `Edge` gain, and how are they edited?

`Edge` has `RoutingStyle`, `SourceArrow`, `TargetArrow` and `Color` today.

Decide:

- line width: a free number or presets (thin, medium, thick), and the default
- line pattern: solid, dashed, dotted, and whether dash spacing scales with width
- arrowhead size: per end or per edge, a free number or presets, and whether it scales with line width by default
- new `ArrowStyle` values, if any (open arrow, diamond, circle)
- the Property bar chips for each, and whether the PropertyPanel shows edge fields (the icons map ruled edge fields out of the panel)
- how hit testing and route caching react to a thicker line

## Answer

`Edge` gains two props, `ArrowStyle` grows, and arrowheads stop being a shared `<marker>`. ADR 0080 records the rendering change. Terms in `CONTEXT.md`: Line width, Line pattern, Arrowhead.

**Line width.** `double LineWidth`, a free number, default 2 (today's look), clamped to 1–16 in canvas units. Presets were rejected: they'd fix three values and add a chip type the bar doesn't need.

**Line pattern.** `LinePattern` enum: `Solid` (default), `Dashed`, `Dotted`. Spacing scales with the width w so a pattern reads the same at every width:

| Pattern | `stroke-dasharray` | Caps |
|---|---|---|
| Solid | none | butt |
| Dashed | `4w 3w` | butt |
| Dotted | `0 2w` | round, so each dot is a circle w wide |

EdgeView computes the dash array inline from the width. No dash-dot and no custom arrays.

**Arrowhead styles.** `ArrowStyle` becomes `None`, `Arrow` (filled triangle, today's), `OpenArrow` (a "V" of two strokes), `HollowTriangle`, `Diamond`, `HollowDiamond`, `Circle` (filled). `None` and `Arrow` keep their names. Crow's-foot and bar ends are left out: crow's-foot is a family of markers and would be its own effort.

**Arrowhead size.** No property. The head length is always `3w + 2`: 5 at width 1, 8 at width 2 (today's 8×8), 50 at width 16. Both ends of an edge are the same size.

**Rendering.** Each head is its own `<path>` in EdgeView, placed and rotated from the route's end tangent, with path data computed in C# from (style, w). The shared `<marker id="edge-arrow">` and its `<defs>` block go. A head fills (or, if hollow, strokes) with the same `var(--d12-edge-override, var(--d12-edge))` as the line, so the Edge colour rule that heads match their line still holds without `context-stroke`. Each `ArrowStyle` has an inset, the distance from tip to back, and the drawn line stops that far short of the endpoint at that end. That keeps the line out of hollow heads and stops a thick line's square end from showing beside a tip. Dotted and dashed edges get solid heads.

**Hit testing and selection.**
- The transparent hit stroke is `max(band / scale, w)`, so a thick line hits across its whole visible width and a thin one keeps the 20 screen-pixel band (ADR 0017).
- Head paths carry `data-d12-role=Edge` and `pointer-events: all`, so a press on any head, hollow ones included, selects the edge.
- The halo is `w + 8` instead of a fixed 10, and the heads get a halo too.
- The hit stroke and the route run to the true endpoints. Only the painted line is shortened.

**Route cache, marquee, lasso.** No change. Width, pattern and heads are paint only, so `RouteRequest` stays (endpoints, routing) and the marquee keeps testing route geometry.

**Property bar.** For an edge selection the order is Colour, Width, Pattern | Routing | Source arrow, Target arrow. New roles: `PropertyRole.EdgeLineWidth`, a glyph and number field (1–16, "–" when mixed), and `PropertyRole.EdgeLinePattern`, a value chip with `OptionGlyphs`. New glyphs: 3 for patterns, 1 for width, and a source and target glyph for each of the 5 new arrow styles, 14 in all, drawn under the icons map's glyph rules. `EdgeStyle` gains `LineWidth` and `LinePattern`, so `ChangeEdgeStyleCommand` covers undo with no new command.

**PropertyPanel.** Still no edge fields. The icons map ruled them out and this doesn't change that.

**New edges.** Fixed defaults: width 2, `Solid`, no source head, `Arrow` at the target. Whether new edges take the last-used style instead is passed to [WYSIWYG connector creation](10-wysiwyg-connector-creation.md), which already asks where a new connector's style comes from.
