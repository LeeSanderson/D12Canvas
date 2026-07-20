# Persistence/serialization format

Type: grilling
Status: resolved
Blocked by: 06

## Question

Design the local persistence/serialization format for a board (schema shape, versioning strategy for future migrations) built on the state/data model from ticket 06. It must stay import/export-ready — the format shouldn't need to change shape to later support exporting to or importing from other formats — even though building actual import/export is out of scope for now.

## Answer

Scope is format only: D12Canvas exposes serialize/deserialize; the host app owns storage medium and save/load timing (mirrors the host-owns-placement pattern from ticket 05).

Serialization is System.Text.Json. The persisted document is a versioned envelope — `{ SchemaVersion, Components: [], Groups: [], Edges: [] }` — using plain arrays (not ID-keyed objects) since every entity already carries its own `Id`, and arrays are the more import/export-portable shape.

`Props` round-trips via a two-phase deserialize: phase one parses the envelope generically leaving each instance's `Props` as a raw `JsonElement`; phase two resolves `TProps` per `ComponentInstance` via its `ComponentTypeKey` against the (DI-populated) component registry and binds the `JsonElement` into that concrete type. No CLR-type discriminators (`$type`) — would reintroduce the CLR-identity coupling ADR 0001 already rejected for the registration key. Consequence: the registry must be populated before a saved board can be deserialized.

API surface is an injectable `IBoardSerializer`-style service (registered by `AddD12Canvas`, holding the registry as a constructor dependency, not static methods — avoids a service-locator/ambient-state pattern). Two deserialize methods are exposed: a strict one that throws if any `ComponentTypeKey` is unresolvable, and a partial one that returns the board plus a list of warnings for whatever was skipped — the host chooses its own tolerance.

`SchemaVersion` is written and checked on load; no migration-chain mechanism is built yet (reserved for a real V2, mirroring how `ZIndex` was reserved in ticket 06 — nothing to migrate from today).

Import/export-readiness is satisfied by the above (opaque per-type `Props`, flat arrays, no CLR-coupled metadata) — no further constraint needed; actual import/export mapping is future work this ticket only avoids blocking.

Full rationale: `docs/adr/0004-board-persistence-format.md`.
