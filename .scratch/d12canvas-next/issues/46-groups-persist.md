# 46 — Groups persist

**What to build:** A saved board keeps its groups: the `Groups` array joins the versioned envelope, round-tripping each group's identity and `MemberIds` — including nested membership. The partial deserialize path warns on a group referencing a missing member instead of failing the load.

**Blocked by:** 35 (JSON round-trip (serialize + strict deserialize)), 44 (Group/ungroup lifecycle)

**Status:** ready-for-agent

- [ ] Groups serialize into the envelope and strict-deserialize back with identity and `MemberIds` intact
- [ ] Nested group membership round-trips
- [ ] The partial path surfaces a warning for a group referencing a missing member
- [ ] Reloaded groups behave as groups (click-member-selects-group works after a round-trip)
- [ ] xUnit round-trip coverage
