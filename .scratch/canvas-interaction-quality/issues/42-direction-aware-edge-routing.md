# Direction-aware orthogonal and curved routing

Type: prototype
Status: open

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
