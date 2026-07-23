# 55 — Custom ports at runtime

**What to build:** An end user adds extra ports to a specific component instance at runtime, beyond the four standard ones — e.g. to attach several edges along one side. Custom ports are instance-scoped runtime state (nothing is declared at registration), fractionally positioned so they survive move/resize, persisted with the instance, and attachable exactly like standard ports. (ADR 0005.)

**Blocked by:** 48 (Drag port-to-port creates an edge)

**Status:** ready-for-agent

- [ ] An end user can add a custom port to an instance at a chosen position on its border
- [ ] Custom ports are fractional — they stay proportionally placed through move and resize
- [ ] Edges attach to custom ports exactly as to standard ports
- [ ] Custom ports persist with the instance and round-trip, with attached edges intact
- [ ] The registration contract is unchanged — component authors declare nothing
- [ ] Screenshot case for an instance with a custom port and attached edge
