# Connector style props

Type: grilling
Status: open
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
