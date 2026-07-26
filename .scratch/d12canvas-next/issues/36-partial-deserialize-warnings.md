# 36 — Partial deserialize with warnings

**What to build:** A host developer chooses how to handle imperfect input: alongside the strict path, a partial deserialize path returns the recoverable board plus a list of warnings instead of throwing — e.g. instances whose component-type key isn't registered are reported and omitted rather than failing the whole load. (ADR 0004.)

**Blocked by:** 35 (JSON round-trip (serialize + strict deserialize))

**Status:** resolved

- [x] The partial path never throws on unknown component-type keys or malformed entities — it returns a board plus warnings
- [x] Each warning identifies the affected entity and the reason it was skipped
- [x] Entities unaffected by problems load normally
- [x] The strict path's behaviour is unchanged
- [x] xUnit coverage of mixed good/bad payloads

## Comments

Added `IBoardSerializer.DeserializePartial(string)` returning a new
`PartialBoardDeserializeResult(Board, IReadOnlyList<BoardDeserializeWarning>)`.
Implementation parses the envelope generically (`JsonDocument`), then per
component entry: attempts the same `FromEnvelope` bind the strict path uses,
catching `UnknownComponentKeyException` and any other exception (malformed
JSON shape, props that don't match the registered `PropsType`, etc.) as a
skip-and-warn instead of letting it propagate. `Deserialize` (strict path) is
untouched. Schema-version mismatches still throw `UnsupportedSchemaVersionException`
in both paths — that's a whole-document problem, not a per-entity one.

Entity identification (`DescribeEntity`) reads the raw entry's `Id` straight
off the `JsonElement` before attempting the full envelope bind, so even a
structurally malformed entry (e.g. `Bounds` isn't an object) is still
identified by its real `Id` in the warning — only an entry with no readable
`Id` at all falls back to a positional `Components[N]` label. Schema-version
validation was factored into one `EnsureSupportedSchemaVersion` helper shared
by both paths to avoid duplicating that check.

Coverage in `BoardJsonSerializerPartialDeserializeTests.cs`: all-valid (no
warnings), unknown component type, structurally malformed entry (asserts the
warning still carries the real `Id`), an entry with no readable `Id` at all
(asserts the positional fallback), props type mismatch, a mixed payload (1
good + 3 bad → board has exactly the good one, 3 warnings each identifying
the right entity), schema-version mismatch still throws, and a regression
check that the strict path still throws `UnknownComponentKeyException`
unchanged.

Reviewed via `/code-review` (standards + spec axes in parallel). Findings
addressed: extracted the duplicated schema-version check, and fixed entity
identification to read the real `Id` off the raw JSON even when the full
envelope bind fails, instead of always falling back to a positional index.
