# 35 — JSON round-trip (serialize + strict deserialize)

**What to build:** A host developer serializes a `Board` to the versioned JSON envelope and strictly deserializes it back, byte-for-byte semantically identical: `SchemaVersion` plus entity arrays (components only at this point — later entity tickets extend the envelope). Props round-trip via the two-phase deserialize: generic parse leaving props raw, then registry-resolved bind by component-type key — no CLR type names anywhere in the JSON. Exposed as an injectable serializer service; the host owns all storage I/O. A Demo page proves save/load. (ADR 0004.)

**Blocked by:** 20 (Component-type registration contract & registry), 21 (Board model with component instances)

**Status:** ready-for-agent

- [ ] Serializing produces the versioned envelope: `SchemaVersion` + a components array
- [ ] Strict deserialize rebuilds an equivalent board — IDs, bounds, `ZIndex`, and typed props all intact
- [ ] Props bind by component-type key through the registry; no CLR-type discriminators appear in the output
- [ ] Strict deserialize throws on an unknown component-type key
- [ ] The serializer is an injectable service; the library performs no storage I/O
- [ ] Demo page saves and reloads a board; xUnit round-trip coverage
