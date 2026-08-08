# Live gesture geometry

Type: grilling
Status: open
Blocked by: 01

## Question

Decide how geometry that is *in flight* during a gesture reaches everything that depends on it, before the gesture commits.

Two of the seed notes — connectors not staying attached through a resize, and connectors updating only after a shape stops moving — are the same defect. `ComponentContainer` mutates its own local `X`/`Y`/`Width`/`Height` during a drag or resize and only raises `OnMoved`/`OnResized` on mouseup. `DiagramCanvas` computes every edge's endpoints from `ComponentInstance.Bounds` on the `Board`, which does not change until that commit. So the container moves and the edges do not. The move branch in `HandleMouseMove` does not even call `NotifyStateChanged`, so nothing outside the container re-renders at all.

Decide:

- Where in-flight geometry lives. Options include a transient overlay on `Board` reads, a gesture-scoped store owned by the arbitration model from ticket 01, or the container continuing to own it with a push channel outward. ADR 0003 is settled, so `Board`'s persisted shape cannot change — but whether *reads* can be intercepted is open.
- What else needs live geometry besides edges. The selection-anchored property bar (08) must track a moving selection; alignment guides (11) are meaningless without it; multi-selection bounding boxes and group bounds are computed from members.
- How this stays consistent with the gesture-level history model in ADR 0007. A gesture is exactly one history entry; live geometry must not produce intermediate commands, and must roll back cleanly if the gesture is cancelled or leaks (ticket 04).
- The render-cost question. `d12canvas-next` tickets 01/02 found that Blazor re-rendering every mounted child regardless of its own state was the real bottleneck, fixed with skip-if-unchanged plus a throttled pan `StateHasChanged`. Live edge updates reintroduce per-mousemove pressure on a different path — decide whether the same throttling applies, whether edges need their own render path, and whether this needs a stated budget or falls out of the design.
- Whether the LOD placeholder path (ADR 0011) and windowed mounting need to participate, or can ignore in-flight geometry entirely.
