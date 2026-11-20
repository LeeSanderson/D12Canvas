# WYSIWYG connector creation

Type: prototype
Status: open
Blocked by: 08
Map: [Better standard components](../map.md)

## Question

What does a connector look like while it's being drawn, and what style does it start with?

Decide, with a throwaway Demo prototype to react to:

- the preview draws with the real routing style, arrowheads, colour, width and pattern, rather than today's provisional line
- the preview's route as the pointer hovers a shape or port it will attach to (snapped) versus open board (floating)
- where a new connector's style comes from: library defaults, the last connector edited, or a "current connector style" set in the Property bar before drawing
- whether the host can set the defaults through `D12CanvasOptions`
