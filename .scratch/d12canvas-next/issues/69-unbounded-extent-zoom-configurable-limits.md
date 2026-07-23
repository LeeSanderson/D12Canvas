# 69 — Unbounded extent & zoom, configurable limits

**What to build:** An end user never hits an artificial wall: board extent and zoom range are unbounded by default, replacing the prototype's fixed 3000×3000 extent and 0.6x–6x zoom constants. A host developer who needs bounds sets optional min/max zoom parameters, which the canvas honours. (ADR 0011.)

**Blocked by:** 22 (Canvas renders a Board), 25 (Windowed mounting + render discipline)

**Status:** ready-for-agent

- [ ] No fixed board extent remains — content can be placed and panned to arbitrarily far coordinates
- [ ] Zoom has no built-in floor or ceiling by default
- [ ] Host-configurable min/max zoom parameters are honoured when set
- [ ] Pan/zoom remain numerically stable at extreme (but realistic) coordinates and scales
- [ ] xUnit/bUnit coverage of limit enforcement; screenshot sanity case at an extreme zoom
