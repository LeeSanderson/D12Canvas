# 36 — Partial deserialize with warnings

**What to build:** A host developer chooses how to handle imperfect input: alongside the strict path, a partial deserialize path returns the recoverable board plus a list of warnings instead of throwing — e.g. instances whose component-type key isn't registered are reported and omitted rather than failing the whole load. (ADR 0004.)

**Blocked by:** 35 (JSON round-trip (serialize + strict deserialize))

**Status:** ready-for-agent

- [ ] The partial path never throws on unknown component-type keys or malformed entities — it returns a board plus warnings
- [ ] Each warning identifies the affected entity and the reason it was skipped
- [ ] Entities unaffected by problems load normally
- [ ] The strict path's behaviour is unchanged
- [ ] xUnit coverage of mixed good/bad payloads
