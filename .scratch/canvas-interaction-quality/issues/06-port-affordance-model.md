# Port affordance model

Type: prototype
Status: open
Blocked by: 01, 19

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

**Constraint from ADR 0026 (ticket 16 resolved):** the keyboard-parity work could not answer what this decision means for keyboard users, because this ticket still changes what affordances exist. What it hands over is a constraint rather than a preference: **ports must be reachable and visible without hover.** ADR 0010's mouse-free connector attachment already depends on it, and the code is live rather than planned — `Enter` enters port picking, arrows jump to the four standard ports, `Space` reaches a custom one. If ports become visible on focus rather than on hover, record that as the accessibility improvement it is; if they stay hover-dependent, this ticket owes an explanation of how a keyboard user aims.

Separately, ADR 0025's relationship test handed this ticket `PortHitRadius = 10`, which is board space by its own comment and therefore cannot join the screen-pixel ordering. ADR 0026 adds one more member to that ordering worth checking against: the grid nudge step, whose dominant cell is 6.3 to 63 screen pixels.
