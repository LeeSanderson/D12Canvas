# DOM-rendering performance ceiling for Blazor components

Type: research
Status: resolved

## Question

How many simultaneously-rendered DOM-based canvas components (in the style of the current `ComponentContainer`) can a Blazor WebAssembly app realistically render and interact with (drag/resize/pan/zoom) before frame rate or interaction responsiveness degrades noticeably? What practical mitigation techniques exist within Blazor WASM's rendering model (virtualization/windowing of off-screen elements, render batching, `ShouldRender`/`@key` optimizations, CSS containment/`content-visibility`, etc.), and which are proven enough to design the state/data model around?

This informs the state/data model ticket — the goal is understanding the ceiling and viable mitigations *before* the data model locks, not building the mitigation itself (that's the follow-on prototype ticket).

## Answer

No primary source gives a definitive "N components" ceiling, but triangulating Microsoft's own per-component render-benchmarks, Chrome's DOM-size/layout-cost thresholds, and Excalidraw's documented degradation point yields a reasoned estimate: an idle/static ceiling of roughly 90–160 mounted `ComponentContainer` instances, and a much lower concurrently-interactive (smooth drag/pan/zoom) ceiling on the order of 200–500 elements without mitigation.

Blazor's built-in `Virtualize<TItem>` doesn't apply — it's a single-axis, uniform-height, linear-list mechanism incompatible with freeform 2D absolute positioning. The load-bearing finding: **Blazor.Diagrams** (a real production Blazor diagram library) ships viewport virtualization today via a plain O(n) linear bounding-box scan with **no spatial index**, recomputed only on pan/zoom/resize events (not per-frame), and has no open performance complaints about it.

**Recommendation adopted:** design the state/data model's viewport-query *surface* in now — e.g. a `GetComponentsIntersecting(bounds)`-style seam, bounds as first-class queryable fields, and a stable per-component identity for `@key` — while deferring the actual spatial-index *implementation* (quadtree/R-tree) until profiling proves a linear scan insufficient. The retrofit risk is architectural (every render/drag/persist call site talking to "the full list" instead of a query seam), not algorithmic — a linear scan over thousands of bounding boxes is sub-millisecond in .NET.

Two directly-actionable, already-present findings for later tickets:
- `DiagramCanvas`'s pan handler and `ComponentContainer`'s drag/resize handlers call `StateHasChanged()`/`NotifyStateChanged` unconditionally on every native `mousemove` (tens–hundreds of times/second) — official Blazor guidance calls this out by name as needing throttling.
- Blazor.Diagrams has an open bug from overloading one `Visible` boolean for both user-controlled and virtualization-driven visibility — the state/data model should keep these as separate fields from the start.

Full findings, citations (22 primary sources), and reasoning: `research/performance-ceiling` branch, `.scratch/d12canvas-next/research/performance-ceiling.md` (commit `fccfb15`).
