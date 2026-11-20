# One rule names a border point's side, screen distances keep a fixed order, and numeric guards keep coordinates and scale well formed

ADR 0028 partitions an instance's border, ADR 0049 routes edges by side, and ADR 0011 leaves zoom unbounded with optional host limits. Each leaves a small rule unstated that several types depend on: which side a corner belongs to, which orthogonal lane wins, how finely a curve is flattened, how the screen-pixel distances relate, and what keeps coordinates and scale well formed.

**`BorderPartition.SideOf` is the one rule for which side a border point lies on, and a corner belongs to the top or bottom side. The distances in `ScreenPixels` keep a fixed order. `GridSnap.NearestLine` never returns negative zero. `ZoomPanTracker.SetZoomLimits` takes both limits in one call and re-clamps at once, and `Scale` never falls below a numerical floor.**

## A corner belongs to the top or bottom side

`BorderPartition.SideOf` checks Y first. A Y fraction of 0 or below names Top, 1 or above names Bottom, and only then does an X fraction of 0 or 1 name Left or Right. So a corner belongs to the top or bottom side, and a point off the border has no side.

Adding a port pins its cross fraction to its side's own 0 or 1, so every port the product creates lies on exactly one side, a corner port counting as on its top or bottom side. A port inside the bounds is on none.

`EdgeRouter.SideOfFraction` uses the same rule. For a fraction inside the shape it takes the nearest border, through `AutoPortSide.Facing`. The router and the partition therefore never disagree about which side a custom port leaves from.

ADR 0049's table says a corner fraction takes the first matching side in `StandardPorts.All` order. That order is Top, Right, Bottom, Left, which would put the bottom-right corner on Right. The code puts it on Bottom, and this ADR records what the code does.

## Orthogonal routes prefer the centred lane

`EdgeRouter.Lanes` lists the gap between the two shapes and the midpoint between the stubs before any other coordinate. Among equally cheap orthogonal paths the first one found wins, so the centred one is chosen.

## A curve is flattened into 64 chords

`EdgeRouter.CurveChords` is 64. That is enough that halfway along the chords is within a fraction of a board unit of halfway along the curve, for any edge a board holds. The label of a curved edge sits at that halfway point.

## Screen distances keep a fixed order

The distances in `ScreenPixels` are screen pixels because each says how precisely a hand aims a pointer, not anything about the board. A gesture divides them by scale before comparing board units.

Their order matters more than their values. A press must count as a drag (`DragThreshold`) before it can snap (`ObjectSnapTolerance`). A snap must let go before an edge's hit band (`EdgeHitBand`) would claim the pointer. The smallest affordance left on a border (`AffordanceFloor`) is still wider than a snap. `PortTarget` is the one border number, and `AffordanceFloor`, `CornerReserve` and `CornerTarget` derive from it. ADR 0028 sets `PortTarget` at 24 and the floor at half of it. Retuning one value keeps that order.

## Grid snapping never yields negative zero

`GridSnap.NearestLine` adds `0.0` to its result. `Math.Round(-0.5)` is `-0`, which would render as `-0px` and serialise as `-0`. Adding zero turns it into `0`.

## Zoom limits are set in one call

`ZoomPanTracker` has no zoom limits of its own. Both default to unbounded, and a host may set a minimum and a maximum through `ZoomPanTracker.SetZoomLimits(minZoom, maxZoom)`. Each limit is null or positive, and a minimum above the maximum throws `ArgumentException`.

A host that changes both limits together, as `DiagramCanvas`'s own parameters do, must never pass through a state where the minimum exceeds the maximum. With two setters, whether that state appears would depend on which field gets assigned first. One call that takes both values and validates the pair removes the ordering question. A host cannot set limits one at a time. It passes both, with null for the one it leaves open.

`SetZoomLimits` re-clamps the current `Scale` before it returns, without waiting for the next `ZoomIn` or `ZoomOut`. `Scale` never sits outside the configured bounds, so a host that narrows the range sees the canvas move into it at once.

## Scale stays positive

With no host minimum, repeated zoom-out approaches zero and never reaches it. `ZoomPanTracker` clamps scale to at least `MinPositiveScale`, 0.0001, whatever the host's limits. A scale of zero or below would flip the canvas and break `Viewport`, which divides by `Scale`. The floor is a numerical guard, far below any zoom a host would want, and it is not offered to hosts as a zoom limit. Zoom stays unbounded in practice.

## Consequences

A new border rule calls `BorderPartition.SideOf` instead of writing its own corner test.

A change to any `ScreenPixels` value checks the order above.

Tests pin the zoom rules: a thousand zoom-outs stay above zero, a limit set past the current scale moves the scale, and a minimum above the maximum throws.
