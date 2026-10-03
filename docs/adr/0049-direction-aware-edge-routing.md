# Orthogonal and curved edges leave each end along its side, and orthogonal edges route around the shapes they connect

ADR 0005 made `RoutingStyle` a per-edge choice and never said what each style draws. `EdgePathD` filled the gap with one horizontal-first path for both styles that never reads `PortId`, so an orthogonal edge on a `Top` port leaves sideways and turns at once. This decides what `Orthogonal` and `Curved` draw. `Straight` is unchanged.

## Every end gets a side

| End | Side |
|---|---|
| Standard port | the port's own side |
| Custom port | the border its fraction lies on. Every custom port the product can create sits on a border (ADR 0028). A corner fraction takes the first matching side in `StandardPorts.All` order |
| `Auto endpoint` | the side ADR 0027 resolves, so it never faces away from the other end |
| `FloatingEndpoint` | a **pseudo-side**: whichever axis points more towards the other end's point, in that direction. A tie goes horizontal |

A floating end has nothing to leave from, so the pseudo-side exists only for routing and is never stored. Leaving a floating end unconstrained gave the same path most of the time, but the route could flip when the floating end crossed a diagonal. Horizontal-first is today's defect kept for one case.

## Orthogonal

1. Each end leaves along its side's outward normal by the **stub**, 20 board units.
2. The router joins the two stub points with the cheapest orthogonal path that stays clear of both connected shapes, each inflated by the stub. Cost is length plus a fixed penalty per bend, so a shorter path with more turns loses to a slightly longer one with fewer.
3. A path never doubles back into a stub.
4. If no path clears both shapes, because they overlap or one stub point is inside the other shape, the router takes the cheapest path that ignores the shapes.

**A port that faces away routes around its own shape.** A `Right` port with its target to the left leaves rightward, goes over or under its shape, and comes back. That costs two extra bends. The alternative leaves the line running under its own shape, and since edges paint beneath instances the line appears to come out of the wrong side. Every elbow router in the reference teardown routes around.

**Only the two connected shapes are obstacles.** Avoiding every instance on the board is a general routing problem, and none of the reference tools do it for elbow connectors.

Ends on different axes need no special case. A `Right` port meeting a `Top` port gets an L when the L clears both shapes, and the router finds something longer when it does not.

## Curved

A cubic with each control point on its end's outward normal, at an offset of `max(stub, 0.4 × end-to-end distance)`. The side becomes a control-point offset instead of a real segment, so `Curved` needs no router and no obstacles. A port that faces away loops outward on its own. A floating end uses its pseudo-side the same way.

The offset follows the distance between the ends and not the gap along the side. React Flow's gap-along-the-side rule was tried: it is tighter when ends face each other, but it collapses towards the stub when the two sides are perpendicular, which kinks the curve.

## The stub is in board units

An edge is board content, and its shape should not depend on the zoom. A screen-constant stub grows in board units as the user zooms out, so two shapes 40 units apart route straight at 1x and around at 0.25x. Zooming would reroute edges. It would also make every route stale on every zoom frame.

The value is `GridBaseSpacing`, so under `Snap-to-grid` at the base level a stub from a grid-aligned port ends on a grid line and the elbows land on the grid. Per ADR 0025 that is asserted as a relationship: the stub equals the base grid step. A fraction-of-distance stub was rejected because close shapes got stubs too short to read as leaving along a side.

## Labels sit on the path

An edge label anchors at the straight-line midpoint between the endpoints today, and `EdgeLabelStyle`'s comment calls that a reasonable stand-in for the other styles. A route that goes around a shape breaks it: the chord midpoint can sit in empty canvas or over a shape, far from the line. **A label anchors at the point halfway along the routed path's length** for `Orthogonal`, and at `t = 0.5` for `Curved`. `Straight` lands where it does today. A new label from double-click uses the same point.

## The endpoint drag preview routes too

While an end of an existing edge is dragged, the preview stands in for the edge. It draws in the edge's own style through the same router, treating the dragged end as floating, so the user sees the shape they are about to commit. A brand-new connector drag stays a straight line, because a new edge is `Straight` (ADR 0005).

## A route is computed when its inputs change

A route depends only on each end's point and side and on the two connected `Bounds`. Pan and zoom change none of these, and the board-unit stub keeps it that way. Cache the route per edge on those inputs. Per ADR 0025's counts-not-clocks rule, the test is that a pan frame computes zero routes.

## How this is verified

Unit tests on the router as a pure function, with no canvas:

- An orthogonal edge on a `Top` port has a first segment that goes up, with length at least the stub.
- A facing-away pair produces a path that enters neither shape's interior.
- The last segment of an orthogonal edge arrives along the target side's inward normal, so the arrowhead points into the port.
- A floating end's first segment follows its pseudo-side.
- A curved edge's control points lie on each end's normal, and the offset is never below the stub.
- The stub equals `GridBaseSpacing`.
- The label anchor lies on the path.

A count test shows a pan frame computes no routes. Visual baselines that include `Orthogonal` or `Curved` edges will move.

## What this amends

**ADR 0005** gains what each routing style draws.

**ADR 0027** said the router reads neither endpoint field. It now reads the side, and the auto endpoint is its best input, as ADR 0027 predicted. Its decision to keep `RoutingStyle` independent of endpoint kind holds and is stronger: the reference tools couple the two because their routers need a side, and here every end, floating ones included, now has one.

**ADR 0025** is applied unchanged: one relationship constant and one count test.

The router needs `Auto endpoint` (ADR 0027) to have a side to read. Until that ships, an attached end is always a named port, which already has one.

## Considered and rejected

- **A fixed stub-then-midline rule that never looks at the shapes**: simpler, but a port that faces away sends the line back under its own shape.
- **Today's horizontal-first path**: ignores the side, which is the defect.
- **Leaving inward and crossing the port's own shape when it faces away**: the line appears to leave from the opposite side.
- **A screen-constant stub**: zooming would reroute edges and invalidate every cached route on every frame.
- **A stub as a fraction of the end-to-end distance**: close shapes get stubs too short to read.
- **An unconstrained floating end**: the route can flip as the floating end crosses a diagonal.
- **Horizontal-first for floating ends only**: keeps the defect for the palette connector, which starts with both ends floating.
- **React Flow's gap-along-the-side curve offset**: kinks when the two sides are perpendicular.
- **Avoiding every instance on the board**: a general routing problem that no reference tool solves for elbow connectors.
- **Constraining which endpoints a style may use**: ADR 0027 already rejected it, and with a router that reads the side there is nothing left for it to protect.
