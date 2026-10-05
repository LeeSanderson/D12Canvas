# 92 — Border partition and ports on selection

**What to build:** Ports appear on a single selected shape and on the shape under the pointer during a connector drag, never on hover, and a shape's border is partitioned between connecting and resizing with the cursor as the only indicator (ADR 0028). `Hit region`s are real elements sized against a `--d12-scale` custom property that the content style publishes, so they stay constant in screen pixels. Per side, the run is the length in screen pixels minus a corner reserve at each end; each port takes the stretch nearest it capped at the port target; close ports split at the midpoint; resize takes what is left; anything below the floor drops from render and hit together; in a below-floor collision the standard port keeps its full width and the custom port is clipped. One number, the port target of 24 screen pixels, is owned by C# and published through the content style, with floor and corner reserve derived from it. `PortHitRadius` is deleted. Visuals (port dot 20px, corner handle 10px) clamp below 0.25x while targets do not. Side resize loses its drawn handle; the cursor draws the partition (`crosshair` on a port span, `ns-resize` or `ew-resize` on a resize span, corner cursors on corners, `move` on the body, none on author content). No two affordance regions on a border intersect at any zoom. Where a port is not rendered it is not hittable, so a drop there is auto. The prototype on `prototype/port-affordance` is evidence, not code to merge.

**Blocked by:** 75 (ResizeSelection), 91 (Auto endpoint)

**Status:** ready-for-agent

- [ ] Ports render only on a single selected shape and on the drop target during a connector drag; hovering shows nothing
- [ ] Pressing near a side's centre starts a connector and pressing nearer a corner resizes, with the cursor telling which before the press
- [ ] A corner handle is grabbable at 0.1x and at 4x and with six custom ports on one side
- [ ] At a zoom where a side's resize span would fall below the floor it is gone from both render and hit, and zooming in brings it back
- [ ] Hit regions are real elements and a predicate in C# decides which entities have one, read by both the markup and the marquee
- [ ] A board-space radius no longer decides any drop
- [ ] Pure C# tests for the partition over port counts and side lengths assert non-intersection and the floor; constants asserted by ordering
- [ ] Baselines for a selected shape's ports, a crowded side and a sub-0.25x clamp; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Port`, `Border partition` and `Hit region` terms describe what shipped
