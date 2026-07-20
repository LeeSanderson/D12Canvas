# Connectors/edges model

Type: grilling
Status: resolved
Blocked by: 06, 05

## Question

Design how connectors/edges between components work: how an edge attaches to a component (fixed points vs. dynamically calculated anchor points), routing (straight/orthogonal/curved), labels, and how an edge is represented in the state model from ticket 06. Depends on the component registration contract (05) to know what a component exposes as connectable anchor points.

## Answer

Edges attach to **ports**: every `ComponentInstance` automatically exposes four standard ports at its border centers (top/right/bottom/left), positioned as a fraction of `Bounds` so they stay correct across resize and track the component through move for free. An end user can add further custom ports to a specific instance at runtime (`ComponentInstance.CustomPorts: List<PortDef>`, same fractional representation) — this is per-instance runtime state, not a registration-time concept, so **no change to the component registration contract (ticket 05) is required**.

Each edge endpoint is a `PortEndpoint { ComponentId, PortId }` or a `FloatingEndpoint { Point }` — floating endpoints support a connector dropped on empty canvas, not attached to anything.

Routing (`RoutingStyle: EdgeRouting { Straight, Orthogonal, Curved }`, default `Straight`) and directionality (`SourceArrow`/`TargetArrow: ArrowStyle { None, Arrow }`, default target-only arrow) are both per-edge choices, not board-wide settings.

Labels are rich — a full `ComponentInstance` (reusing the component system, defaulting to a built-in text component), but embedded directly on the edge (`Label: ComponentInstance?`), not a `Board.Components` entry — owned and deleted with the edge, never independently selectable or orphaned.

`Board.Edges` (reserved empty by ADR 0003) is filled in accordingly:

```csharp
class Edge {
    Id: Guid
    Source: EdgeEndpoint
    Target: EdgeEndpoint
    RoutingStyle: EdgeRouting = Straight
    SourceArrow: ArrowStyle = None
    TargetArrow: ArrowStyle = Arrow
    Label: ComponentInstance? = null
}
abstract class EdgeEndpoint {}
class PortEndpoint : EdgeEndpoint { ComponentId: Guid; PortId: string }
class FloatingEndpoint : EdgeEndpoint { Point: Point }

class PortDef { Id: string; FractionX: double; FractionY: double }
```

**Scope boundary:** attaching an edge to a whole `Group` as a single unit is not decided here — carried forward to "Not yet specified" pending the selection/grouping ticket.

Full rationale: `docs/adr/0005-connector-edge-model.md`. Glossary: `CONTEXT.md` (Edge, Port; Board and Entity updated to drop their "future edges" qualifier).
