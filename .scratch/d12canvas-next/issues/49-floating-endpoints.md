# 49 — Floating endpoints

**What to build:** An end user drags from a port and releases over empty canvas: the edge is still created, its far endpoint left floating at that board point. A floating endpoint can later be dragged onto a port to attach it. (ADR 0005 — endpoints can float.)

**Blocked by:** 48 (Drag port-to-port creates an edge)

**Status:** ready-for-agent

- [ ] Releasing over empty canvas creates the edge with a floating endpoint at the release point
- [ ] A floating endpoint renders visibly and stays at its board point through pan/zoom
- [ ] Dragging a floating endpoint onto a port attaches it
- [ ] Dragging an attached endpoint off its port can detach it back to floating
- [ ] Screenshot case for an edge with a floating endpoint
