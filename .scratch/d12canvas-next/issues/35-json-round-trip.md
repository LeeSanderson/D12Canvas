# 35 — JSON round-trip (serialize + strict deserialize)

**What to build:** A host developer serializes a `Board` to the versioned JSON envelope and strictly deserializes it back, byte-for-byte semantically identical: `SchemaVersion` plus entity arrays (components only at this point — later entity tickets extend the envelope). Props round-trip via the two-phase deserialize: generic parse leaving props raw, then registry-resolved bind by component-type key — no CLR type names anywhere in the JSON. Exposed as an injectable serializer service; the host owns all storage I/O. A Demo page proves save/load. (ADR 0004.)

**Blocked by:** 20 (Component-type registration contract & registry), 21 (Board model with component instances)

**Status:** resolved

- [x] Serializing produces the versioned envelope: `SchemaVersion` + a components array
- [x] Strict deserialize rebuilds an equivalent board — IDs, bounds, `ZIndex`, and typed props all intact
- [x] Props bind by component-type key through the registry; no CLR-type discriminators appear in the output
- [x] Strict deserialize throws on an unknown component-type key
- [x] The serializer is an injectable service; the library performs no storage I/O
- [x] Demo page saves and reloads a board; xUnit round-trip coverage

## Comments

New `D12Canvas/Persistence/` folder: `IBoardSerializer` (`Serialize(Board)` / `Deserialize(string)`),
`BoardJsonSerializer` (constructor-injected `IComponentRegistry`), internal envelope DTOs
(`BoardEnvelope`, `ComponentInstanceEnvelope`, `BoundsEnvelope` — kept separate from the domain
`Bounds`/`ComponentInstance` types so the wire format doesn't leak `Bounds`'s computed
`Right`/`Bottom` properties), and `UnsupportedSchemaVersionException` (ADR 0004's "`SchemaVersion`
is written and checked on load"). Two-phase deserialize relies on two System.Text.Json defaults:
serializing an `object`-typed property uses the value's runtime type (no `$type` needed), and
deserializing into one yields a boxed `JsonElement` — confirmed empirically via the test suite, not
just assumed. Strict deserialize's "unknown key" throw is just `IComponentRegistry.Resolve` doing
what it already does — no new exception type needed there.

`ComponentInstance`'s constructor gained an optional trailing `Guid? id = null` (defaults to
`Guid.NewGuid()`, unchanged for every existing call site) so deserialize can restore the original
`Id` instead of minting a new one - required for "IDs... intact" in the round-trip.

`AddD12Canvas` now also registers `IBoardSerializer` → `BoardJsonSerializer` as a singleton,
alongside the existing `IComponentRegistry` registration and behind the same
call-more-than-once idempotency guard.

Demo: `D12Canvas.Demo/Pages/SaveLoadDemo.razor` (nav: "Save/Load Demo") — Save serializes the
seeded `Board` into a visible textarea; Load deserializes the (editable) textarea contents back
into a fresh `Board` passed to the same long-lived `DiagramCanvas`. Verified in a real browser
(Playwright, throwaway driver, not committed) by editing both a note's text and its `X` in the
saved JSON before clicking Load and confirming the note visibly moved and re-labelled with no
console errors.

xUnit: `BoardJsonSerializerTests` (envelope shape, round-trip of Id/Bounds/ZIndex/Props, Props
binding to their real CLR type rather than staying a `JsonElement`, no CLR-type discriminator in
the output, multiple component types each binding to their own Props type, strict deserialize
throwing `UnknownComponentKeyException`) plus a DI resolution test added to
`ServiceCollectionExtensionsTests`.

**Two pre-existing bugs surfaced while building the demo page, both out of scope for this ticket
and filed separately** ([75](75-componentcontainer-shouldrender-ignores-props.md),
[76](76-diagramcanvas-dispose-null-cleanup-delegates.md)): `ComponentContainer.ShouldRender()`
doesn't notice a Props swap at unchanged Bounds (only geometry/selection are compared), and
`DiagramCanvas.DisposeAsync()` throws on every real disposal because its JS-interop cleanup
delegates are `Action`-typed but a JS function can't actually marshal into one (bUnit's mocked
module setup masks this). The demo page avoids both by never remounting `DiagramCanvas` — `Load`
reassigns `Board` on the same instance — and the verification edited a Bounds coordinate (which
*is* compared) alongside the Props change.
