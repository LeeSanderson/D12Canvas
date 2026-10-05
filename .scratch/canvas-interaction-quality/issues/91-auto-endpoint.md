# 91 — Auto endpoint

**What to build:** Dropping a connector anywhere on a shape's body attaches it so that it keeps choosing the side facing the other end (ADR 0027). `IEdgeEndpoint` gains a fourth shape, `AutoPortEndpoint(ComponentId)`, carrying the id and nothing else. It resolves to the standard port on the side an aiming line from the component centre to the other end's reference point crosses; the one crossing-free case, the other point inside the rect, takes the nearest side. Resolution is pairwise, so `Board` and `Live geometry` both resolve an edge's two ends together. Three drop zones ordered so the easier gesture gives the more forgiving result: within a port's target pins; anywhere else over a component gives auto; nothing floats. The drop resolves in C#. The same-endpoint guard widens to same-component. Persistence uses the envelope's previously unreachable combination (component named, no port named) with no new field and no schema bump. The keyboard pick (Enter on an instance stop) defaults to auto and cycles through auto, standard and custom ports with Space. An `Auto endpoint` never chooses a custom port.

**Blocked by:** 76 (DragEdgeEnd and SelectEdge)

**Status:** ready-for-agent

- [ ] Dropping a connector on a shape's body attaches it and the edge leaves from the side facing the other end; moving either shape re-chooses the side
- [ ] Dropping precisely on a port pins it; dropping on empty canvas floats it
- [ ] A drop on the source shape's own body creates nothing
- [ ] Enter on a focused shape picks auto first; Space cycles to Top, then the rest, then any custom port, then back to auto; Enter commits
- [ ] A saved edge with an auto end round-trips through both load paths and a board saved under the current schema loads unchanged
- [ ] Pure C# tests for resolution over all four sides and the inside case; serializer tests for the encoding
- [ ] A baseline shows an auto-attached edge; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Auto endpoint` term describes what shipped
