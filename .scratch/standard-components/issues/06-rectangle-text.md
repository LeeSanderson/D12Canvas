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
