# State/data model

Type: grilling
Status: resolved
Blocked by: 02

## Question

Design the in-memory (and eventually serializable) representation of a board's content — component instances, their registered-component-type reference and props, groups, and (per the connectors ticket) edges. It must remain collaboration-ready (amenable to a future CRDT/OT/sync layer without a rewrite) and informed by the performance findings/mitigation shape from tickets 01-02 (e.g., does it need spatial indexing or a viewport-aware structure to support virtualization?). This is the central ADR most other tickets depend on.

## Answer

`Board` holds three flat, ID-keyed entity collections — `Components`, `Groups`, `Edges` (reserved empty; content deferred to the connectors/edges ticket) — no ownership tree. Every entity (`ComponentInstance`, `Group`, future `Edge`) gets a GUID at creation; relationships are ID references only. This shape is a direct consequence of staying collaboration-ready: flat, independently-addressable entities merge cleanly under a future CRDT/OT layer, where a nested tree would force structural conflict resolution.

`ComponentInstance` = `ComponentTypeKey` (string, per ADR 0001) + boxed `object Props` (resolved to `TProps` via a registry lookup — captured generically at `RegisterComponent` time, never string-to-`Type` parsing) + `Bounds` (position/size, per ADR 0001) + an explicit `ZIndex` field (reserved now so re-layering is a field write, not a shared-sequence reorder).

Entities are mutable classes, mutated in place — matching today's `ComponentContainer` drag/resize code. Consequence: the deferred undo/redo history model will need explicit change-tracking (command/memento), not free snapshotting.

`Group` is its own entity: `Id` + `MemberIds` (may reference instances or other groups, giving nesting for free); bounds computed on demand from members, not stored.

Viewport query: `Board.GetVisible(viewport, overscan)` — a plain instance method wrapping the O(n) linear bounding-box scan already validated in tickets 01/02, no spatial index. Subset-visibility queries (a group's members, a future edge) are deferred until an actual caller needs one; extracting the scan into a stateless core function is the intended path then, not duplicating it.

Surfaced and addressed as an addendum to ADR 0001 (not a re-open of ticket 05): component registration must validate key uniqueness and throw on collision — `ComponentInstance.ComponentTypeKey` assumes the registry guarantees one type per key.

Full rationale: `docs/adr/0003-board-state-model.md`. Glossary: `CONTEXT.md` (Board, Entity, Group, plus existing Component type/instance/Key/Props/Bounds).

Graduates from map fog: **Undo/redo history model** and **Styling/theming** (both were waiting on this ticket) — filed as new tickets.
