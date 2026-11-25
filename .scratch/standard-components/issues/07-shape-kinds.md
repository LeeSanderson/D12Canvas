# Shape kinds

Type: grilling
Status: resolved
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

## Answer

One component, `Shape`, with a `Kind` prop. Terms in `CONTEXT.md`: Shape, Shape kind.

**Kinds.** Rectangle (default), rounded rectangle, ellipse, diamond, hexagon, cylinder, parallelogram, trapezoid. No circle kind locked to 1:1: an ellipse in a square box is a circle. Rhombus is the diamond.

**Model.** `Rectangle` becomes `Shape`: key `rectangle` becomes `shape`, display and accessible name "Shape". The project is unreleased, so there's no migration. `ShapeProps` keeps `FillColor`, `StrokeColor` and `StrokeWidth` and gains `Kind`. Default size stays 160×100.

**Picking a kind.** The Palette has one Shape button with the existing rectangle glyph, and it always places a rectangle. It doesn't remember the last kind. A new `ShapeKind` property role shows as a choice chip, first in the Property bar, and opens the library flyout with one glyph per kind (8 new glyphs). A mixed selection shows Mixed. The Property panel gets the row through `[PanelEditable]`.

**No per-kind props.** Geometry is fixed and comes from the bounds:

| Kind | Geometry |
|---|---|
| Rounded rectangle | corner radius `min(12, 25% of short side)` board units |
| Parallelogram | slant 20% of width, top edge shifted right |
| Trapezoid | top edge inset 20% of width at each end, narrow at the top |
| Hexagon | points left and right, each 20% of width deep |
| Cylinder | cap ellipse 15% of height deep, top cap drawn as a full ellipse |

The spec may tune these numbers. Switching kind leaves no unused data on the instance.

**Rendering.** Each kind is an inline SVG drawn from props alone. The component still doesn't receive its size, and the library's component seam doesn't change. Rect, rounded rect (`rx="12"`), ellipse and the cylinder's parts use SVG percentage attributes. Diamond, hexagon, parallelogram and trapezoid draw in a 0–100 viewBox stretched to the box. The stroke width is scaled by `--d12-scale` under a non-scaling stroke, so slanted lines are as thick as straight ones, and the shape follows a live resize with no C# re-render. Colours still go through the `--d12-board-*` overrides.

**Hit area.** The body follows the outline, filled or not. A press in a diamond's or ellipse's empty corner goes to whatever lies below it, or to the canvas, which starts a marquee. The SVG catches presses only where it paints, and the library's hit test already reads the DOM. Selection chrome (resize spans, corner handles, port spans) and the marquee test stay on the bounding box.

**Ports.** A new optional registration hook maps props to the four standard port fractions. A type that doesn't set it keeps the border centres. On six kinds the border centres are already on the outline, and the cylinder's top port sits at the top of its cap. Parallelogram and trapezoid move left and right to `(0.1, 0.5)` and `(0.9, 0.5)`. Standard ports aren't stored, so changing kind moves them, and attached edges follow because their ends resolve live. Port spans stay on the box sides, and only the dot moves. Auto endpoints still pick a side against the box and resolve to that side's standard port. Custom ports don't change: they stay fractions of the box, and snapping them to an outline goes to [Connection points](11-connection-points.md).

**Fog.** A Palette kind flyout, so you could place a diamond in one go, waits until switching kind after placement proves slow in use.
