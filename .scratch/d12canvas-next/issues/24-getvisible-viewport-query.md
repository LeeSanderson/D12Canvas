# 24 — `GetVisible(viewport, overscan)` query

**What to build:** The `Board` can answer "what's in view": `GetVisible(viewport, overscan)` returns the component instances whose bounds intersect the viewport expanded by a tunable overscan margin. Implemented as a linear scan — the query *surface* locks now; a spatial index is deferred until profiling demands it (per the performance-ceiling and virtualization findings).

**Blocked by:** 21 (Board model with component instances)

**Status:** resolved

- [x] Returns exactly the instances intersecting the expanded viewport, including bounds that touch its edges
- [x] The overscan margin is tunable with a sensible default
- [x] Implementation is a linear scan — no spatial index
- [x] xUnit coverage at the pure C# seam, including empty-board and fully-out-of-view cases

## Comments

Implemented as `Board.GetVisible(Bounds viewport, double overscan = 0)` in `D12Canvas/Model/Board.cs`, reusing `Bounds` itself as the viewport rect rather than introducing a new type — the viewport is just another X/Y/Width/Height rectangle in canvas space, the same shape `ComponentInstance.Bounds` already uses. `overscan` expands the viewport by a flat margin on all four sides before testing intersection.

`Bounds` gained two behaviors rather than `Board` hand-rolling the geometry inline: `ExpandedBy(margin)` (grows a rect symmetrically on all sides) and `Intersects(other)` (closed-interval AABB overlap test, `<=`/`>=` rather than the stricter `<`/`>` the earlier virtualization stress-test prototype used) — so that an instance whose edge exactly touches the (possibly expanded) viewport boundary counts as visible, per this ticket's checklist. `GetVisible` is then just `_components.Values.Where(instance => viewport.ExpandedBy(overscan).Intersects(instance.Bounds)).ToList()` — a plain O(n) scan, no spatial index, matching tickets 01/02's deferred-until-profiling-demands-it conclusion. Moving the intersection math onto `Bounds` (code review flagged the original inline version as Feature Envy) also means `Bounds` now has direct `BoundsTests` coverage of its own.

Default overscan is `0` (no expansion) — a conservative, unsurprising default now that the *surface* is what's locking; ticket 25 (windowed mounting) is expected to pass a real tuned value once it has an actual mount/unmount cost to balance against.

Covered by `BoardTests`: empty board, fully-inside, fully-outside (no overscan), touching each of the four viewport edges individually, touching the *expanded* viewport's edge under a non-zero overscan, a mixed board (inside/touching/outside together) returning exactly the intersecting subset, default-overscan-is-zero, an instance outside the raw viewport but inside the overscan margin, and an instance outside even the expanded viewport. `BoundsTests` covers `ExpandedBy` and `Intersects` (including edge-touch and symmetry) directly.
