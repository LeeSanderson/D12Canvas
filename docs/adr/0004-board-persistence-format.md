# Board persistence is a versioned JSON envelope, deserialized in two phases so component-authored Props round-trip without CLR-type coupling

D12Canvas owns only the wire **format** and the pure functions to move a `Board` in and out of it — not storage medium or save/load timing. An injectable `IBoardSerializer`-style service (registered by `AddD12Canvas`, holding the component registry as a constructor dependency) exposes `Serialize(Board)` and two deserialize paths; the host app decides where the resulting bytes live (file, browser storage, wherever) and when to call in. This mirrors the existing pattern of the host owning placement/layout concerns outside the library's core (ADR 0002's canvas chrome).

Serialization uses System.Text.Json — already in the BCL, no new dependency, and its `JsonElement` support is what makes the two-phase `Props` deserialize below workable without a custom converter.

The persisted document is a versioned envelope, not bare board content:

```json
{
  "SchemaVersion": 1,
  "Components": [ { "Id": "...", "ComponentTypeKey": "...", "Props": { }, "Bounds": {}, "ZIndex": 0 } ],
  "Groups": [ { "Id": "...", "MemberIds": [] } ],
  "Edges": []
}
```

Collections are plain arrays, not ID-keyed objects — no information is lost since `Id` is already a field on every entity (ADR 0003), `Board` rebuilds its in-memory index from the array in one pass, and arrays are the shape essentially every external interchange format uses, which keeps this format import/export-ready without designing any actual import/export now.

**`Props` round-trip (the crux of this ADR):** `ComponentInstance.Props` is a boxed `object`, resolved to its real `TProps` only via a registry lookup keyed by `ComponentTypeKey` (ADR 0001) — there is no CLR type name to deserialize against. Deserialization therefore happens in two phases: phase one parses the envelope generically, leaving each instance's `Props` as a raw `JsonElement`; phase two looks up `TProps` for that instance's `ComponentTypeKey` in the (DI-populated) component registry and binds the `JsonElement` into that concrete type. Consequence: the registry must already be populated — host components registered — before a saved board can be deserialized; this is inherent (an unregistered component type can't be rendered either), not a workaround.

Two deserialize methods are exposed, not one, so the host picks its own tolerance for a stale or renamed `ComponentTypeKey`: a strict form that throws if any instance's type can't be resolved, and a partial form that returns the board plus a list of warnings for whatever was skipped.

`SchemaVersion` is written and checked on load, but no migration-chain mechanism is built yet — there is exactly one version and nothing to migrate from. This mirrors `ZIndex` in ADR 0003: the field is reserved so the format's *shape* doesn't need to change later, without speculating on a migration mechanism whose real needs aren't known until a second version actually exists.

**Considered and rejected:**
- **A `$type` discriminator using CLR assembly-qualified names** (Newtonsoft-style polymorphic serialization) — would reintroduce exactly the CLR-type-as-identity problem ADR 0001 already ruled out for the registration key, breaking the moment a `TProps` class is renamed or moved.
- **ID-keyed dictionary/object collections** instead of arrays — more convenient for O(1) in-memory lookup, but that's `Board`'s own indexing concern, not a serialization concern; arrays are the more portable wire shape and lose nothing since `Id` is already per-entity.
- **Building a migration pipeline now** (e.g. an `IBoardMigration` chain) — rejected as speculative abstraction designed against a hypothetical V2 whose actual shape can't be known yet; deferred until a real second schema version exists.
- **Static methods on `Board`** for serialize/deserialize — would need a service-locator or ambient singleton to reach the DI-registered component registry from a static context; an injectable service keeps the registry an explicit, testable constructor dependency instead.
