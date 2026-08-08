# Edge attachment without a named port

Type: grilling
Status: open

## Question

Decide whether `EdgeEndpoint` gains a case that attaches to a component *without* naming a port — letting routing pick the side — and whether `RoutingStyle` constrains which endpoints are legal.

Ticket 03 found this to be the largest single divergence between D12Canvas's settled decisions and the reference bar. None of Miro, FigJam, tldraw or Excalidraw exposes discrete ports as the primary attachment model; all four bind to a normalized anchor over the target's bounds and treat named sides as sugar over it. Every one of them has an auto/centre/imprecise state that `EdgeEndpoint` cannot currently express:

- Miro: `snapTo` (`top`/`bottom`/`left`/`right`/`auto`) and `position` (fractional) are "always in sync, and mutually exclusive" — and it surfaces the distinction as two user-visible drop targets on the same object: drop on the **body** for a connection that re-routes to the shortest path, drop on an **edge dot** for one that stays pinned.
- Figma: `ConnectorEndpoint` is a three-variant union whose magnet includes `AUTO` and `NONE`.
- tldraw: `normalizedAnchor` defaulting to the shape centre, with `isPrecise: false`.
- Excalidraw: `mode: "inside" | "orbit" | "skip"` alongside a `fixedPoint` anchor.

ADR 0005 rejected "dynamically-calculated, unnamed anchor points" — but the teardown's reading is that it rejected the wrong thing, since the reference anchors are both named *and* dynamic: "auto" is a name.

Decide:

- **Whether `EdgeEndpoint` gains an attach-to-component-without-a-port case**, and if so whether it is a third union case or a distinguished `PortId` value.
- **What the common drop gesture produces.** ADR 0009 says dragging from a port starts an edge, but the reference bar's common case is dropping on the *body* of a target with the edge dot as the precise case. `CompletePortDrag` should be read against that — dropping on a component rather than a port is the gesture that most needs to work well, and it is currently the one with no endpoint to produce.
- **Whether `RoutingStyle` and endpoint choice stay independent per-edge fields.** Every tool that models both couples them: Figma constrains magnets by line type ("Elbowed connectors may use all magnets besides CENTER. Straight connectors may use only CENTER or NONE"), Excalidraw's midpoint magnets exist only for elbow arrows, tldraw's directional handles are elbow-only. D12Canvas can express `Straight` + `PortId.Top`, which all three consider ill-formed. Either state the coupling, or decide what four ports mean for a straight edge — no reference tool has an answer for the latter.
- **Whether custom ports survive as authored entities**, given Excalidraw reached the same expressive power with a handle owned by the *arrow endpoint* rather than the shape — one per endpoint, created by binding, hidden when redundant, and costing no per-instance persisted state or affordance on every unselected shape.

Amends ADR 0005. Blocks ticket 06: the port affordance question cannot resolve cleanly while the model has no way to express "you pick".
