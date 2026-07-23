# 24 — `GetVisible(viewport, overscan)` query

**What to build:** The `Board` can answer "what's in view": `GetVisible(viewport, overscan)` returns the component instances whose bounds intersect the viewport expanded by a tunable overscan margin. Implemented as a linear scan — the query *surface* locks now; a spatial index is deferred until profiling demands it (per the performance-ceiling and virtualization findings).

**Blocked by:** 21 (Board model with component instances)

**Status:** ready-for-agent

- [ ] Returns exactly the instances intersecting the expanded viewport, including bounds that touch its edges
- [ ] The overscan margin is tunable with a sensible default
- [ ] Implementation is a linear scan — no spatial index
- [ ] xUnit coverage at the pure C# seam, including empty-board and fully-out-of-view cases
