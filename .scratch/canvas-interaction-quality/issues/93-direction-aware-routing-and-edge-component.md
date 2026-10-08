# 93 — Direction-aware routing and the edge component

**What to build:** An orthogonal edge leaves each end straight out from its side and routes around both connected shapes, a curved edge bends out along each side, and labels sit halfway along the drawn path (ADRs 0049, 0020 edge half). The router is a pure function reading each end's side. `Orthogonal` leaves by a stub of 20 board units (equal to the base grid spacing) and takes the cheapest orthogonal path (length plus a per-bend penalty) clearing both connected shapes inflated by the stub, falling back to ignoring the shapes when no clear path exists. `Curved` places control points on each side's normal at the larger of the stub and 0.4 times the distance. A floating end gets a pseudo-side facing the other end. Routes are cached per edge on endpoints, sides and the two bounds, so a pan computes zero routes. Each `Edge` becomes its own component with `ShouldRender` comparing resolved endpoints, so dragging one shape on a board with hundreds of edges re-renders only the edges it touches. The prototype on `prototype/edge-routing` is evidence, not code to merge.

**Blocked by:** 91 (Auto endpoint)

**Status:** resolved

- [x] An orthogonal edge from a top port leaves upward, never sideways; an edge between two overlapping-column shapes routes around rather than through
- [x] A curved edge's tangents at both ends are normal to their sides
- [x] A label on an orthogonal edge sits on a segment, not off in the corner of the bounding box
- [x] A straight edge still renders as a line with the same attributes existing tests assert
- [x] Panning a board with five hundred edges computes no routes; dragging one shape with ten attached edges re-renders ten edge components and no others, asserted by render count
- [x] Pure C# tests for the router over the side combinations and the fallback; bUnit for the render count
- [x] Baselines for orthogonal and curved edges from each side on the edge-styles demo page; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Edge` term already describes this; no vocabulary change

## Comments

Built as specified. These calls were made along the way.

The router is the pure `EdgeRouter`. `Orthogonal` tries a fixed set of candidate paths between the two stub points: straight, an L either way, a Z through one lane, and a path with three interior turns through a pair of lanes. The lanes are the gap between the shapes, the midpoint between the stubs, each stub point, each shape's inflated edges, and one stub beyond the outermost stub point. Among paths that leave and arrive along their sides and never turn straight back, it takes the cheapest one clear of both shapes inflated by the stub, and otherwise the cheapest one that ignores them. Cost is length plus 40 per bend, twice the stub. The gap and midpoint lanes come first, so between equally cheap paths the centred one wins. The extra outer lane exists for two floating ends closer than two stubs, which would otherwise have no path that does not double back.

A custom port's side is the border its fraction lies on, a corner taking `StandardPorts.All` order. That order differs from `BorderPartition.SideOf`, which puts `Bottom` before `Right` for the bottom-right corner; routing follows the ADR.

Each `Edge` renders through a new `EdgeView` component, keyed by edge id, whose `ShouldRender` compares its resolved ends, path data, selection and arrows. The canvas caches each edge's route on a `RouteRequest` of style, both ends' points and sides, and both shapes' live bounds, so a pan or zoom routes nothing. `DiagramCanvas.RoutesComputed` exposes the count for the test.

The endpoint drag preview routes too: a carried end of an existing edge draws a dashed path in that edge's own style with the pointer as a floating end, and its label follows that route. A new connector stays a straight line. The preview is routed directly, not through the cache, because it is the one edge whose inputs change on every frame.

`EdgeBox`, the box the keyboard-opened context menu anchors to, now spans the route's points rather than the chord between the ends.

Baselines: `?sides=orthogonal` and `?sides=curved` on `/edge-styles-demo` draw an edge from each side of a shape into the top of one below and to the right, giving two new baselines. Every HTML baseline with an edge moved for the component boundary, a marker comment and the indentation of the hit line. The routing-and-arrowheads baseline also moved for the curved edge's control points. No existing PNG moved. All 1451 bUnit tests pass (1 skipped). The full visual suite of 200 tests passed in the pinned image under `-parallel none` after folding.
