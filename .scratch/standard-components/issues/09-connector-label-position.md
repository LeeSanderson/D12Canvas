# Connector label position

Type: grilling
Status: open
Blocked by:
Map: [Better standard components](../map.md)

## Question

How does a user move a connector's label, and how is its position stored?

Today the label's `Bounds.X/Y` are never read; the label sits at the route's midpoint.

Decide:

- the stored position: a 0 to 1 fraction along the route, plus a perpendicular offset or none
- how the user drags it: pressing the label drags it along the line, or freely with the offset
- what happens when the route changes (endpoint moved, routing style changed): the fraction holds
- whether the label background masks the line under it
- snapping to the midpoint
