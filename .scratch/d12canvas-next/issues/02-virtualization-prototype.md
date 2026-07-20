# Virtualization/windowing mitigation stress test

Type: prototype
Status: resolved
Blocked by: 01

## Question

Given the performance ceiling and candidate mitigations surfaced by ticket 01, build a rough, throwaway prototype that stress-tests the most promising mitigation (e.g., viewport-based virtualization/windowing of off-screen components) against a large synthetic set of components on the existing `DiagramCanvas`. Does the mitigation actually hold frame rate/responsiveness at the target scale? What does it demand of the state/data model (e.g., spatial indexing, viewport-aware queries) that the state/data model ticket needs to account for?

## Answer

**Yes, decisively** — measured against the real `ComponentContainer`/`DiagramCanvas`, driven headlessly at 2,000–10,000 synthetic components via a stress-test tool kept at `D12Canvas.Demo/Pages/VirtualizationStressTest.razor` (route `/virtualization-stress-test`, permanent dev tool, same role as `ZoomPanTest`/`ChildComponentTest`).

Three variants were compared: **A — None** (baseline, everything mounted), **B — Windowed (tight)** (only viewport-intersecting nodes mounted), **C — Windowed (buffered)** (viewport + 50% overscan margin). Windowing recomputes on `DiagramCanvas`'s existing `OnZoomOrPanChanged` event (pan/zoom/resize), not per frame — the mechanism ticket 01 validated (Blazor.Diagrams' linear bounding-box scan, no spatial index).

**Original measurements (windowing as the only mitigation):**

| Scenario | Mounted | Pan-tick cost |
|---|---|---|
| Baseline, 2,000 total | 2,000 | 1fps / 717ms |
| Baseline, 5,000 total | 5,000 | 1fps / 1.7s |
| Baseline, 10,000 total | 10,000 | UI unresponsive (>8s to click a dropdown) |
| Windowed tight, 10,000 total | 1,159 | 5fps / 193ms |
| Windowed buffered, 10,000 total | 2,455 | 2fps / 589ms |

Windowing alone already turns "unusable" into "usable" — but panning cost was still directly proportional to mounted count, and the buffered variant's overscan cost 3x tight's.

**Root cause found beyond ticket 01's research:** Blazor re-invokes `SetParametersAsync`/render for every mounted child on any parent `StateHasChanged`, regardless of whether that child's own parameters changed — and `DiagramCanvas`'s pan handler fired `StateHasChanged()` unconditionally on every mousemove tick (the two things ticket 01 had flagged as "directly-actionable findings," confirmed here to be the dominant cost, not a minor one).

Fixed directly in production (commit `c3d8b19`): `ComponentContainer.ShouldRender()` now skips re-rendering when its own X/Y/Width/Height/edit-mode are unchanged (true for *every* child during a pure canvas pan, since panning only moves the parent's CSS transform), and `DiagramCanvas`'s pan-handler `StateHasChanged()` is throttled to ~60fps with a flush on mouse-up.

**Re-measured after the fix:**

| Scenario | Mounted | Pan-tick cost | vs. before |
|---|---|---|---|
| Baseline, 2,000 total | 2,000 | 17fps / 59ms | 12x |
| Baseline, 5,000 total | 5,000 | 4fps / 267ms | 6.5x |
| Baseline, 10,000 total | 10,000 | 2fps / 658ms (sluggish, no longer frozen) | — |
| Windowed tight, 10,000 total | 1,159 | 18fps / 57ms | 3.4x |
| Windowed buffered, 10,000 total | 2,455 | 14fps / 71ms | 8x |

**What this demands of the state/data model (ticket 06):**

1. A `GetComponentsIntersecting(bounds)`-style viewport query surface, as ticket 01 recommended — confirmed necessary, since windowing is what bounds the *mount burst* cost and the total DOM/memory footprint (a first render can't skip via `ShouldRender`; only fewer mounted nodes helps there).
2. The bounds parameter needs a **tunable overscan/margin**, not a fixed viewport rect — tight vs. buffered is a real, measured smoothness-vs-cost trade-off (buffered cost ~3x tight's pan-tick before the fix, still ~1.2x after), and different call sites (drag vs. pan vs. zoom) may want different margins.
3. The component contract should expect implementers to skip re-rendering on unchanged own-state (a `ShouldRender`-style contract expectation, not just a one-off fix on this pair of files) — this did almost all of the heavy lifting for pure panning, independent of windowing.
4. Recompute windowing on viewport-change events (pan/zoom/resize), not per frame — confirmed cheap enough (sub-millisecond linear scan per ticket 01) to do this way.

**Split out, not part of this ticket:** a pre-existing, unrelated bug (`ComponentContainer`'s dynamic JS import 404s on every instance, silently breaking click-outside-to-exit-edit-mode) discovered as a side effect — filed separately at `../../component-container-js-import-404/`.

Full driver scripts and screenshots are session artifacts only (not committed) — the validated fixes and the permanent stress-test tool are the durable output, both already on `main`.
