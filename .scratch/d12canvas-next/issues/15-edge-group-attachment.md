# Edge attachment to whole Group

Type: grilling
Status: resolved
Blocked by: 08, 09

## Question

Given the resolved connectors/edges model (08 — edges attach via ports on individual component instances or float) and the resolved selection model (09 — `Group` is a persisted entity with computed bounds), decide whether an edge can attach to a whole `Group` as a single unit (e.g. a port-like anchor on the group's computed bounding box that tracks as members move/resize), or whether edges only ever attach to individual component instances (or float), with a `Group` never itself being an attachable thing.

## Answer

Edges never attach to a `Group` — only to individual `ComponentInstance` ports, or floating (per ADR 0005 / ticket 08's existing model). No schema change: `EdgeEndpoint` stays `PortEndpoint { ComponentId, PortId } | FloatingEndpoint { Point }`; the scope boundary ticket 08 left open resolves as "no group variant," not an addition.

Rationale:
- `Group` (ticket 09) is a selection/transform convenience — no z-slot of its own, no visual identity — not a diagram node. Making it edge-attachable would elevate it to a first-class node, a bigger role than anything else it's been given.
- Attaching to a whole `Group` creates an orphaning problem for free: ungrouping is a routine action, and an edge referencing a `Group` would have nothing left to reference the moment it happens. Restricting edges to individual instances means ungrouping never has any edge to worry about, since edges never reference `Group` in the first place.
- If diagrams eventually need a real "attach to a container" concept, that's a job for a built-in container/frame component (an actual `ComponentInstance`), not for overloading `Group`.

**No conflict with group-select interaction:** ticket 14 already establishes that hovering an instance reveals its own ports and connector-drawing is a direct-drag-from-port gesture, distinct from the click-to-select gesture that routes to "select the whole group" (ticket 09). Group membership has zero effect on port-level interaction — a user can always draw a connector from/to a specific grouped member's port.

No new ADR, no `CONTEXT.md` changes, no ripple into other tickets — this narrows ticket 08's already-declared scope boundary rather than changing any resolved model.
