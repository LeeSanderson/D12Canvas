# Text inside shapes

Type: grilling
Status: open
Blocked by: 03, 07
Map: [Better standard components](../map.md)

## Question

How does a rectangle (and each shape kind) hold and show text?

`RectangleProps` has no text today.

Decide:

- a `Text` prop on the shape's props, edited inline with a double-press as a sticky note is
- text colour, font size, horizontal and vertical alignment props, and their defaults
- whether shape text shrinks to fit like a sticky note's or uses a fixed size
- the text area inside non-rectangular kinds (an ellipse or diamond inset), and whether text clips to it
- whether an empty shape behaves as today (no placeholder, no `IsEmpty` deletion)

## Comments

- From [Markdown subset](03-markdown-subset.md): shape text uses the Basic level: bold, italic, underline and line breaks.
- From [Inline editor markdown round trip](05-inline-editor-markdown-round-trip.md): shape text uses the same shared editor as every formatted text: a `contenteditable` container seeded with C#-rendered HTML and committed as markdown. The static render is a `MarkupString` from the C# HTML writer. Whatever inset and clipping a shape kind needs applies to that container in both states.
- From [Shape kinds](07-shape-kinds.md): the component is now `Shape` (key `shape`) with a `Kind` prop over eight kinds: rectangle, rounded rectangle, ellipse, diamond, hexagon, cylinder, parallelogram, trapezoid. Each is an inline SVG drawn from props, with fixed geometry taken from the bounds. This ticket picks each kind's text inset, for example an ellipse's or diamond's inscribed box, and the cylinder below its cap.
