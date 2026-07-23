# 25 — Windowed mounting + render discipline

**What to build:** An end user gets smooth pan/zoom on large boards: the canvas mounts only the instances `GetVisible` returns, recomputing the window on pan/zoom/resize (never per frame). Mounted instance containers skip re-rendering when nothing about them changed (skip-if-unchanged as the standard contract), and pan-driven re-renders are throttled. Verified against the virtualization stress-test page, which stays as a permanent dev tool.

**Blocked by:** 22 (Canvas renders a Board), 24 (`GetVisible(viewport, overscan)` query)

**Status:** ready-for-agent

- [ ] Only instances returned by the viewport query are mounted; instances mount/unmount as the window moves
- [ ] The window recomputes on pan, zoom, and resize — not per animation frame
- [ ] Instance containers skip re-render when their own state is unchanged
- [ ] Pan-driven re-render is throttled
- [ ] The stress-test Demo page demonstrates the improvement at scale
- [ ] bUnit coverage of mount/unmount behaviour as the viewport moves
