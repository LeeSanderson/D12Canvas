# Port affordance model

Type: prototype
Status: open
Blocked by: 01

## Question

Decide when a component instance's ports are visible, and where they sit relative to the resize handles — currently the two collide both in trigger and in geometry.

Two seed notes land here. Ports appear on hover, which the reported experience says is wrong: they should appear while a connector is being dragged, or when the instance has focus. And the resize handles overlap the ports badly enough to make resizing difficult.

The geometry collision is exact, not approximate. In `ComponentContainer.razor`'s style block, `.port-top` is a 20px circle at `top: -10px; left: calc(50% - 10px)`, and the `.top` resize handle is a 10px circle at `top: -5px; left: calc(50% - 5px)` — perfectly concentric. Ports render before the resize handles in markup, so the handles win the stacking tie-break and sit on top of the ports at all four border centres. Visibility is CSS-only today (`.component-container:hover .port, .component-container.selected .port`), with no C# state involved — a property worth keeping if the new model allows it.

Build a prototype and decide:

- The visibility trigger. Focus, active connector drag, proximity, a modifier key, or some combination — and what happens for a *target* instance during a drag originating elsewhere, which is when ports matter most and when the source instance's own hover state is irrelevant.
- The geometry. Move ports outboard of the handles, move handles inboard, offset them along the edge, or change what appears at border centres versus corners. Whichever way, resize must stay reachable at every border midpoint.
- Hit-target sizing under zoom. Ports and handles are fixed CSS pixel sizes inside a scaled container, so at low zoom they shrink toward unusable and at high zoom they bloat over the content. ADR 0011 made zoom unbounded, which sharpens this considerably.
- Interaction with the port strips. Double-clicking a border strip adds a custom port (`.port-strip`, 14px wide, straddling the border) — it must not become unreachable, and must not fire when the user meant to resize.
- Whether custom ports, which sit at arbitrary fractional positions, can collide with handles the same way and need the same treatment.
- Whether any of this survives the touch non-foreclosure constraint, given hover has no touch equivalent.

Amends ADR 0005, which specified the four standard ports but not their affordance.
