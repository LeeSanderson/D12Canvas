# 51 — Edges persist

**What to build:** A saved board keeps its connections: the `Edges` array joins the versioned envelope. Attached endpoints round-trip as instance ID + port reference; floating endpoints round-trip as board points. The partial deserialize path warns on an edge referencing a missing instance instead of failing the load.

**Blocked by:** 35 (JSON round-trip (serialize + strict deserialize)), 48 (Drag port-to-port creates an edge)

**Status:** ready-for-agent

- [ ] Edges serialize into the envelope and strict-deserialize back with both endpoints intact
- [ ] Attached endpoints round-trip as instance + port references; floating endpoints as board points
- [ ] Reloaded edges stay attached — moving an endpoint instance after a round-trip still drags the edge along
- [ ] The partial path surfaces a warning for an edge referencing a missing instance
- [ ] xUnit round-trip coverage
