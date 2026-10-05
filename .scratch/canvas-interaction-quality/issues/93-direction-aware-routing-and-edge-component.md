# 93 — Direction-aware routing and the edge component

**What to build:** An orthogonal edge leaves each end straight out from its side and routes around both connected shapes, a curved edge bends out along each side, and labels sit halfway along the drawn path (ADRs 0049, 0020 edge half). The router is a pure function reading each end's side. `Orthogonal` leaves by a stub of 20 board units (equal to the base grid spacing) and takes the cheapest orthogonal path (length plus a per-bend penalty) clearing both connected shapes inflated by the stub, falling back to ignoring the shapes when no clear path exists. `Curved` places control points on each side's normal at the larger of the stub and 0.4 times the distance. A floating end gets a pseudo-side facing the other end. Routes are cached per edge on endpoints, sides and the two bounds, so a pan computes zero routes. Each `Edge` becomes its own component with `ShouldRender` comparing resolved endpoints, so dragging one shape on a board with hundreds of edges re-renders only the edges it touches. The prototype on `prototype/edge-routing` is evidence, not code to merge.

**Blocked by:** 91 (Auto endpoint)

**Status:** ready-for-agent

- [ ] An orthogonal edge from a top port leaves upward, never sideways; an edge between two overlapping-column shapes routes around rather than through
- [ ] A curved edge's tangents at both ends are normal to their sides
- [ ] A label on an orthogonal edge sits on a segment, not off in the corner of the bounding box
- [ ] A straight edge still renders as a line with the same attributes existing tests assert
- [ ] Panning a board with five hundred edges computes no routes; dragging one shape with ten attached edges re-renders ten edge components and no others, asserted by render count
- [ ] Pure C# tests for the router over the side combinations and the fallback; bUnit for the render count
- [ ] Baselines for orthogonal and curved edges from each side on the edge-styles demo page; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Edge` term already describes this; no vocabulary change
