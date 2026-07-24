# 21 — Board model with component instances

**What to build:** The `Board` holds component instances as flat, independently-addressable entities: GUID identity assigned at creation, references by ID only, no ownership tree — keeping the model collaboration-ready. A `ComponentInstance` carries its component-type key, boxed props, bounds, and an explicit `ZIndex`. Entities are mutable. (ADR 0003.)

**Blocked by:** None — can start immediately.

**Status:** resolved

- [x] Component instances can be added to, removed from, and looked up on a `Board` by GUID
- [x] Bounds are tracked uniformly on every instance, structurally separate from props
- [x] Every instance carries an explicit `ZIndex`
- [x] No parent/child ownership exists anywhere in the model — entities reference each other only by ID
- [x] xUnit coverage at the pure C# seam

## Comments

Implemented under `D12Canvas/Model/`: `Bounds` (a `readonly record struct` of `X`/`Y`/`Width`/`Height`, mirroring `ComponentSize`'s style), `ComponentInstance` (`Id` assigned via `Guid.NewGuid()` at construction and read-only thereafter; `ComponentTypeKey` fixed at construction; `Props`/`Bounds`/`ZIndex` are mutable properties, matching ADR 0003's "mutated in place" entities — no separate persisted-vs-runtime split), and `Board` (a `Dictionary<Guid, ComponentInstance>` behind `AddComponent`/`RemoveComponent`/`GetComponent`, plus a `Components` read-only view for enumeration).

Scoped narrowly to what this ticket's checklist asks for: only the `Components` collection exists on `Board` so far. `Groups`/`Edges` (and the `Group` entity itself) are left for the tickets that actually introduce them (44+, 47+) rather than reserved speculatively now — nothing here forecloses adding them as additional flat, ID-keyed collections alongside `Components` later.

`GetComponent`/`RemoveComponent` use nullable-return/no-op semantics for an unknown id (not an exception) since a stale id after a mutation is an expected, routine case for later callers (e.g. undo/redo), unlike the registry's `Resolve`, where an unknown key is a genuine configuration error.
