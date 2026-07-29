# 51 — Edges persist

**What to build:** A saved board keeps its connections: the `Edges` array joins the versioned envelope. Attached endpoints round-trip as instance ID + port reference; floating endpoints round-trip as board points. The partial deserialize path warns on an edge referencing a missing instance instead of failing the load.

**Blocked by:** 35 (JSON round-trip (serialize + strict deserialize)), 48 (Drag port-to-port creates an edge)

**Status:** resolved

- [x] Edges serialize into the envelope and strict-deserialize back with both endpoints intact
- [x] Attached endpoints round-trip as instance + port references; floating endpoints as board points
- [x] Reloaded edges stay attached — moving an endpoint instance after a round-trip still drags the edge along
- [x] The partial path surfaces a warning for an edge referencing a missing instance
- [x] xUnit round-trip coverage

## Comments

`Board.Edges` joins the envelope as a third optional array (`BoardEnvelope.Edges`, defaulting to
`null` like `Groups` did in ticket 46), populated via new `EdgeEnvelope`/`EdgeEndpointEnvelope`
records. No CLR-type discriminator (ADR 0004): `EdgeEndpointEnvelope` carries all four of
`ComponentId`/`PortId`/`X`/`Y` as nullable fields, and exactly one pair is ever populated -
`FromEndpointEnvelope` pattern-matches on which pair is present to decide whether to rebuild a
`PortEndpoint` or a `FloatingEndpoint`. An attached endpoint only ever stores `ComponentId` +
`PortId`, never a resolved position, so live tracking after a reload falls out for free from
`Board.ResolveEndpoint` re-deriving the point from the (still-current) instance `Bounds` on every
call - nothing new needed there.

Strict `Deserialize` does no referential-integrity checking for edges, mirroring how `Groups`
already behaves (`StrictDeserializeLoadsAGroupWithAMissingMemberWithoutThrowing`, ticket 46) - a
`PortEndpoint` referencing a missing instance loads as-is. Only the tolerant `DeserializePartial`
path (`DeserializeEdgesPartial`) turns a missing-instance reference into a warning, via the same
tolerance `Board.ResolveEndpoint` already has at read time for a dangling `PortEndpoint`. Edges
never reference `Group`s (ticket 15), so no group-membership-style check was needed here.

`/code-review` (Standards + Spec sub-agents) findings and how they were addressed:
- **Standards**: no hard violations. Flagged that this ticket is the "genuine third occurrence"
  ticket 46's own comment predicted would justify extracting the duplicated
  iterate/describe-entity/try-deserialize-catch-warn loop shape (previously duplicated across the
  Components loop and `DeserializeGroupsPartial`, now a third time in `DeserializeEdgesPartial`).
  Acted on: extracted a shared `ParseEntries<TEnvelope>` helper used by all three call sites -
  Components' inline loop, `DeserializeGroupsPartial`'s first (parse) pass, and
  `DeserializeEdgesPartial` - each supplying only its own `onEntry` callback for what to do with a
  successfully-parsed envelope. Preserves every existing warning message exactly (including
  Components' specific "Unknown component type '{key}'." wording for `UnknownComponentKeyException`,
  which the shared helper still special-cases) - no test changed. Also flagged `EdgeEndpointEnvelope`'s
  four independently-nullable fields as a Primitive-Obsession/Data-Clump smell; left as-is per the
  reviewer's own conclusion that it's the direct, defensible consequence of ADR 0004's
  no-discriminator rule with no existing precedent for a nested discriminated-union envelope shape.
- **Spec**: no gaps, no scope creep, no implementation errors - all five checkboxes verified
  against test coverage. Noted one coverage gap (no duplicate-edge-Id regression test, unlike
  Groups' equivalent) - closed by adding
  `SkipsAnEdgeWithADuplicateIdAndRecordsAWarningInsteadOfFailingTheLoad`.

Test coverage:
- `D12Canvas.Tests/BoardJsonSerializerTests.cs` - envelope shape, strict round-trip for both a
  fully-port-attached edge and a floating-endpoint edge, live tracking after reload (moving an
  endpoint instance still drags the edge), and strict deserialize tolerating a missing endpoint
  instance without throwing.
- `D12Canvas.Tests/BoardJsonSerializerPartialDeserializeTests.cs` - no-warnings round trip, a
  missing-instance reference warns but still loads, a structurally malformed edge entry warns and
  is skipped, and a duplicate edge Id warns and is skipped rather than failing the load.

No rendering or UI touched, so no Playwright visual-test case was needed for this ticket (same as
ticket 46). Full `D12Canvas.Tests` suite (364 tests) passes.
