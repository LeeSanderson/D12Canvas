# 46 — Groups persist

**What to build:** A saved board keeps its groups: the `Groups` array joins the versioned envelope, round-tripping each group's identity and `MemberIds` — including nested membership. The partial deserialize path warns on a group referencing a missing member instead of failing the load.

**Blocked by:** 35 (JSON round-trip (serialize + strict deserialize)), 44 (Group/ungroup lifecycle)

**Status:** resolved

- [x] Groups serialize into the envelope and strict-deserialize back with identity and `MemberIds` intact
- [x] Nested group membership round-trips
- [x] The partial path surfaces a warning for a group referencing a missing member
- [x] Reloaded groups behave as groups (click-member-selects-group works after a round-trip)
- [x] xUnit round-trip coverage

## Comments

`Groups` joins `BoardEnvelope` alongside `Components` as a new `GroupEnvelope(Guid Id, IReadOnlyList<Guid> MemberIds)`
array (`D12Canvas/Persistence/BoardEnvelope.cs`), matching the shape ADR 0004 already reserved. The
`Groups` property defaults to `null` so JSON predating this ticket (no `Groups` key at all — every
existing raw-JSON test literal from tickets 35/36) still deserializes without a missing-property
error; both the strict and partial paths null-coalesce/`TryGetProperty`-guard it to empty.

`BoardJsonSerializer` (`D12Canvas/Persistence/BoardJsonSerializer.cs`):
- `Serialize`/`Deserialize` (strict) gained a mirrored second loop over `Groups`, reusing
  `Group`'s existing `(memberIds, id)` constructor to restore original identity — no referential
  validation, exactly like the existing unvalidated `Components` load.
- `DeserializePartial` gained `DeserializeGroupsPartial`: a structurally malformed group entry is
  skipped-and-warned exactly like a malformed component (same broad `catch` pattern). A group that
  parses fine but references a missing member is **not** dropped — mirroring `Board.GetBounds`'s
  existing tolerance for a dangling member id at read time — instead one warning is recorded per
  missing member id and the group still loads with whatever members do resolve, so the group keeps
  behaving as a group afterwards. Membership existence is checked against components already
  loaded *plus every group parsed in the same batch regardless of array order* (two passes: parse
  all group entries first, then check membership), since a nested group may reference another
  group declared later in the `Groups` array — verified by a dedicated forward-reference test.
- `DescribeEntity` was generalized to take the id/array property names as parameters instead of
  being hardcoded to `Components`, so both entity kinds share one positional-fallback helper.

Test coverage added directly to the existing ticket 35/36 suites (no new test file, consistent with
those tickets covering one concern each within `BoardJsonSerializer`):
- `D12Canvas.Tests/BoardJsonSerializerTests.cs` — strict round-trip: envelope shape, identity +
  `MemberIds` intact, nested membership (`FindContainingGroup` resolves through a restored nested
  group), the "click any member selects the same restored group" behaviour, and (added after
  `/code-review`, see below) a test locking down that strict deserialize loads a group with a
  missing member without throwing, since the ticket's warning requirement is explicitly scoped to
  the partial path and this behaviour was otherwise undocumented.
- `D12Canvas.Tests/BoardJsonSerializerPartialDeserializeTests.cs` — no warnings on a fully valid
  group, a missing-member warning with the group still loaded, a malformed group entry
  skipped-and-warned, the forward-reference (outer group declared before the inner group it points
  at) producing no false-positive warning, and (added after `/code-review`, see below) a duplicate
  group `Id` producing a warning instead of crashing the load.

`/code-review` (Standards + Spec sub-agents) caught one real bug before this was committed: in
`DeserializeGroupsPartial`, `board.AddGroup(...)` sat outside the per-entity `try/catch` (unlike the
pre-existing Components loop, where `board.AddComponent(...)` sits *inside* it) — so two Groups
entries sharing an `Id` would throw `ArgumentException` uncaught out of `DeserializePartial`,
crashing the *entire* load. That's exactly the "failing the load" behaviour this ticket says the
partial path must avoid. Fixed by wrapping the `AddGroup` call in its own try/catch, mirroring the
Components loop, plus the duplicate-Id test above. The Standards pass also flagged the
`ToEnvelope`/`FromEnvelope` vs. `ToGroupEnvelope`/`FromGroupEnvelope` naming asymmetry now that two
entity kinds coexist — renamed to `ToComponentEnvelope`/`FromComponentEnvelope` for symmetry (no
external callers, safe rename). A flagged "duplicated parse-loop shape between Components and
Groups" observation was left as-is: extracting a shared generic helper for only two call sites
(no Edges array yet) would be premature abstraction ahead of a genuine third occurrence.

Full `D12Canvas.Tests` suite (282 tests) passes. No rendering or UI touched, so no Playwright
visual-test case was needed for this ticket.
