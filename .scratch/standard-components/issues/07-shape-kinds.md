# Shape kinds

Type: grilling
Status: open
Blocked by:
Map: [Better standard components](../map.md)

## Question

Which shape kinds does Rectangle support, and how are they modelled?

Decide:

- the kinds: rounded rectangle, ellipse, diamond, triangle, parallelogram, hexagon, cylinder, others
- a `Kind` prop on one component (Rectangle renamed, or kept as the key `rectangle` for old boards) versus a separate Palette entry per kind
- how a user picks the kind: Palette, a Property bar chip, or both
- per-kind extras such as corner radius
- where default ports sit on each outline, and whether a port placed by hand snaps to the outline rather than the bounding box
- how each kind renders (SVG path per kind) and how its hit area follows the outline or the bounds
