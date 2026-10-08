# 92 — Border partition and ports on selection

**What to build:** Ports appear on a single selected shape and on the shape under the pointer during a connector drag, never on hover, and a shape's border is partitioned between connecting and resizing with the cursor as the only indicator (ADR 0028). `Hit region`s are real elements sized against a `--d12-scale` custom property that the content style publishes, so they stay constant in screen pixels. Per side, the run is the length in screen pixels minus a corner reserve at each end; each port takes the stretch nearest it capped at the port target; close ports split at the midpoint; resize takes what is left; anything below the floor drops from render and hit together; in a below-floor collision the standard port keeps its full width and the custom port is clipped. One number, the port target of 24 screen pixels, is owned by C# and published through the content style, with floor and corner reserve derived from it. `PortHitRadius` is deleted. Visuals (port dot 20px, corner handle 10px) clamp below 0.25x while targets do not. Side resize loses its drawn handle; the cursor draws the partition (`crosshair` on a port span, `ns-resize` or `ew-resize` on a resize span, corner cursors on corners, `move` on the body, none on author content). No two affordance regions on a border intersect at any zoom. Where a port is not rendered it is not hittable, so a drop there is auto. The prototype on `prototype/port-affordance` is evidence, not code to merge.

**Blocked by:** 75 (ResizeSelection), 91 (Auto endpoint)

**Status:** resolved

- [x] Ports render only on a single selected shape and on the drop target during a connector drag; hovering shows nothing
- [x] Pressing near a side's centre starts a connector and pressing nearer a corner resizes, with the cursor telling which before the press
- [x] A corner handle is grabbable at 0.1x and at 4x and with six custom ports on one side
- [x] At a zoom where a side's resize span would fall below the floor it is gone from both render and hit, and zooming in brings it back
- [x] Hit regions are real elements and a predicate in C# decides which entities have one, read by both the markup and the marquee
- [x] A board-space radius no longer decides any drop
- [x] Pure C# tests for the partition over port counts and side lengths assert non-intersection and the floor; constants asserted by ordering
- [x] Baselines for a selected shape's ports, a crowded side and a sub-0.25x clamp; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Port`, `Border partition` and `Hit region` terms describe what shipped

## Comments

Built as specified. These calls were made along the way.

The partition is the pure `BorderPartition`. Each span edge is a fraction of its side plus a number of port targets, so the markup places it as `calc(F% + T * var(--d12-port-target) / var(--d12-scale))` and C# decides only which spans exist at the current zoom. A container re-renders on a zoom while its ports show. `--d12-port-target` is published beside `--d12-scale`, and `ScreenPixels` gains the port target with the floor, corner reserve and corner target derived from it.

Two geometric cases the ADR did not cover: on a shape thinner than a port target, opposite sides' spans would meet, so a span reaches inward at most halfway across; and on a side shorter than two corner targets the corners would meet, so a corner target narrows to half the side. The property test models both. A run shorter than the floor holds nothing.

Below-floor collision: when the midpoint split leaves the standard port under the floor, it takes its full stretch and every custom port on either side is clipped against it. A port with no span draws no dot, unless the keyboard pick highlights it.

The drop target is the topmost component under the pointer on each move, which the listener now reports with a hit stack while a press carries an edge end. The shape the anchor is on is never the drop target. A drop target shows port spans only, no resize spans or corners.

The port strip, its role and its double-click are gone, so no pointer gesture adds a custom port until ticket 104's menu row. `IGestureContext.AddCustomPort` went with it; `AddCustomPortCommand` stays for that row.

Ticket 91's rule that a press on the port an auto end resolves to carries that end is removed. ADR 0028 makes the span around a side's midpoint the place a connector is pulled from, and which port an auto end sits on changes as the other end moves, so the same press would start a connector or move an edge depending on something the user cannot see. A press on a port span now carries only an end pinned to that port. The cost: an auto end has no pointer route to be moved; it can be deleted and redrawn.

The hit-region predicate is `HasHitRegion`, read by the container markup, the LOD placeholder and the marquee. Today it is true for every instance on the board; locking is its first case that will be false.

Not changed, and worth a look: `.component-container` keeps `min-width: 100px; min-height: 100px`, so a shape under 100 board units renders larger than its `Bounds`. Edges already attach at the bounds-derived points; the partition is computed from `Bounds` too, while its percentages resolve against the rendered box. Author content inherits the body's `move` cursor, as before.

Baselines: three new ones, a selected shape's ports, a crowded side and a selected shape at a fifth of full zoom, plus the custom-port baseline moved to the new `/border-partition-demo` page and the hover-ports baseline deleted. Removing the always-rendered port dots moved every HTML baseline with an instance. The PNGs that moved are the ones where hover or a multi-selection used to show ports, and the connector drags, which now select their source first. All 1350 bUnit tests pass (1 skipped). The full visual suite of 198 tests passed in the pinned image under `-parallel none` after folding, which also confirms ticket 91's two baselines.
