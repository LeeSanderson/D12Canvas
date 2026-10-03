# Direction-aware orthogonal and curved routing

Type: prototype
Status: resolved

## Question

Decide whether `Orthogonal` and `Curved` routing derive each segment's direction from the side its endpoint attaches to, and what happens when a named port's side faces away from the other end.

Today they do not, and the result is wrong on contact. `EdgePathD` is unconditionally horizontal-first for both styles:

```
Orthogonal: M f.X f.Y  L midX f.Y  L midX t.Y  L t.X t.Y
Curved:     M f.X f.Y  C midX f.Y  midX t.Y  t.X t.Y
```

Nothing consults `PortId`. So an orthogonal edge attached to a **`Top`** port leaves that port **sideways** and immediately turns, and a curved one bends the same way, its control points sitting on the same `midX`. A vertical stack of two shapes connected top-to-bottom happens to look right only because `midX` coincides with both X coordinates and the path degenerates to a straight line.

Surfaced while resolving [Edge attachment without a named port](19-edge-attachment-without-named-port.md), which found this while checking whether `RoutingStyle` should constrain which endpoints are legal. It should not, and the reason is this ticket: the reference tools couple the two fields (Figma's magnets-by-line-type, Excalidraw's elbow-only midpoint magnets, tldraw's elbow-only directional handles) because **their routers read the side**. A legality rule here would hide the defect rather than fix it — the line would still leave sideways in every case the rule still permitted.

Two things make this cheaper than it looks.

**ADR 0027 gives the router a side from every attached endpoint, including auto.** An `Auto endpoint` resolves to the standard port on whichever side its aiming line crosses, so both attached shapes hand the router a `PortId` and it needs no case for auto at all. Auto is in fact the better input: its side is derived from where the other end currently is, so it can never face away from what it connects to.

**A `FloatingEndpoint` has no side**, which is the one input the router cannot derive anything from, and there are already boards full of them — ADR 0009's palette entry creates an edge with both ends floating.

Build a prototype and decide:

- **Whether the first segment leaves along the port's outward normal**, and how far before it turns. A fixed board-unit stub, a screen-constant one, or a fraction of the gap.
- **What happens when a port faces away from the other end.** A `Right` port on a shape whose target is to its left has to either leave rightward and route around, or leave leftward and cross its own shape. Every elbow router in the reference bar routes around; the cost is a longer path with two extra bends.
- **What a `FloatingEndpoint` end does**, having no side to leave along. Falling back to today's horizontal-first, deriving a pseudo-side from the direction of the other end, or leaving the first segment unconstrained at that end only.
- **Whether `Curved` follows the same rule or a different one.** A cubic can express the outward direction as a control-point offset rather than a real segment, which may make it the cheaper of the two rather than a variant of the elbow.
- **Whether an edge's two ends can disagree** about which axis to leave on, and what the middle of the path does when they do.

Amends ADR 0005, which specified `RoutingStyle` as a per-edge choice but never what each style draws.

**Scope note:** ADR 0027 kept routing style and endpoint choice independent, so nothing here reopens that. This ticket may sit past this map's destination if edge routing counts as rendering quality rather than interaction quality — worth ruling on before it is claimed.

## Answer

Recorded as ADR 0049, worked as a prototype. The dev ruled the ticket in scope before claiming it: an edge leaving its port the wrong way is a felt defect on the acceptance surface, and the map already admitted edge visibility on the same grounds.

Three orthogonal routers were prototyped over live controls: route around both connected shapes (A), stub then a shape-blind midline (B), and today's horizontal-first path (C). **A won.**

Decided:

1. **Every end has a side.** A standard port uses its own side, a custom port the border it lies on, and an `Auto endpoint` the side ADR 0027 resolves. A floating end gets a pseudo-side: the axis pointing more towards the other end. It is used only for routing and never stored.
2. **`Orthogonal` leaves each end along its side by a stub, then takes the cheapest orthogonal path that clears both connected shapes**, each inflated by the stub. The cost is length plus a penalty per bend, and a path never doubles back into a stub. A port that faces away routes around its own shape. If no path clears, the router ignores the shapes. Other instances on the board are not obstacles.
3. **`Curved` puts each control point on its end's normal** at `max(stub, 0.4 × distance)`. It needs no router, and a port that faces away loops outward on its own. React Flow's gap-along-the-side rule was rejected because it kinks when the two sides are perpendicular.
4. **The stub is 20 board units, equal to `GridBaseSpacing`.** A screen-constant stub would reroute edges as the user zooms and make every route stale on every zoom frame. A fraction-of-distance stub was too short between close shapes.
5. **Ends on different axes need no special case.** The router gives an L when it clears.

Two consequences the prototype did not show, both decided in ADR 0049:

- **Labels anchor halfway along the drawn path**, not at the straight-line midpoint, which can sit far off a route that goes around a shape.
- **Dragging an end of an existing edge previews it in its own style** through the router, with the dragged end treated as floating. A new connector drag stays straight.

A route is cached per edge on its inputs. A pan frame computes no routes, and the test asserts that count.

Amends ADR 0005 (what each style draws) and ADR 0027 (the router now reads the side, and the independence of style from endpoint kind holds). `CONTEXT.md`'s `Edge` entry no longer says the router reads neither. Prototype on `prototype/edge-routing` (`292aa22`), route `/edge-routing-prototype`. No new tickets and no fog.
