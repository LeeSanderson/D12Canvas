# 55 — Custom ports at runtime

**What to build:** An end user adds extra ports to a specific component instance at runtime, beyond the four standard ones — e.g. to attach several edges along one side. Custom ports are instance-scoped runtime state (nothing is declared at registration), fractionally positioned so they survive move/resize, persisted with the instance, and attachable exactly like standard ports. (ADR 0005.)

**Blocked by:** 48 (Drag port-to-port creates an edge)

**Status:** resolved

- [x] An end user can add a custom port to an instance at a chosen position on its border
- [x] Custom ports are fractional — they stay proportionally placed through move and resize
- [x] Edges attach to custom ports exactly as to standard ports
- [x] Custom ports persist with the instance and round-trip, with attached edges intact
- [x] The registration contract is unchanged — component authors declare nothing
- [x] Screenshot case for an instance with a custom port and attached edge

## Comments

Implemented as: `ComponentInstance.CustomPorts: List<PortDef>` (`PortDef { Id, FractionX, FractionY }`),
a new `CustomPortEndpoint(ComponentId, PortId)` `IEdgeEndpoint` shape alongside `PortEndpoint`/
`FloatingEndpoint`, and a small `PortRef` union so the connector-drag gesture (`StartPortDrag`
onward) doesn't need to know whether it started from a standard or custom port until it builds the
actual endpoint. `Board.ResolveEndpoint`/`FindPortNear`/`FindEdgeAttachedTo` generalized to cover
both port kinds uniformly.

UI: each `ComponentContainer` renders four invisible border strips (visible/interactive only while
selected-and-not-multi-selected, same gate as the resize handles) - a double-click anywhere along
one adds a custom port at that fractional position via a new `AddCustomPortCommand` (undoable).
Custom ports render and drag exactly like standard ports (same `.port` affordance, positioned via
inline style from their own fraction).

Persistence: `ComponentInstanceEnvelope.CustomPorts`/`EdgeEndpointEnvelope.CustomPortId` both
default to null/absent so older boards still deserialize.

Note: touching `ComponentContainer.razor`'s shared per-instance markup/CSS (as ticket 50's own
comment anticipated) invalidated every existing Playwright HTML baseline that renders a
`ComponentContainer` - all were re-promoted after confirming each diff was exactly the new
`.port-strip` CSS/markup, with the handful of re-encoded PNG baselines checked visually and
confirmed unchanged (the strips have no border/background, only `cursor: copy`, so they paint
nothing).
