# 77 — Click-to-add can place at (-width/2, -height/2) on a cold Blazor WASM boot

**What's wrong:** `DiagramCanvas.ClickToAdd` (`D12Canvas/DiagramCanvas.razor.cs`) centers a new instance on `_zoomPanTracker.Viewport`, which derives from `ContainerWidth`/`ContainerHeight` - only populated once `OnAfterRenderAsync`'s `getContainerDimensions` JS-interop call resolves. If a palette click fires before that call has resolved (both default to `0`), the instance centers on `(0, 0)` and lands at `(-Width/2, -Height/2)` - e.g. a 200x150 default note at `(-100, -75)`, half off-canvas to the top-left.

**Status:** needs-triage

**How it was found:** Regenerating Playwright visual-test baselines for ticket 39 (Rectangle built-in). `ClickToAddPlacementVisualTests.ClickToAddedInstance_MatchesBaseline` intermittently captured `left: -100px; top: -75px` instead of the correct centered position, specifically when it happened to be the first test to hit a freshly-started `dotnet run` Demo app process (cold JIT of the request pipeline / slow first Blazor circuit boot - the same class of slowness `D12Canvas.VisualTests/ModuleInitializer.cs` already extends its `Expect` timeout for). A same-page retry, or running the test anywhere but first, reliably produced the correct `(108, 224)` position. The existing `Expect(...).ToHaveCountAsync(...)` guard in each test's `InitializeAsync` only waits for palette entries to render - a DOM condition independent of whether `DiagramCanvas`'s own async container-size JS-interop round trip has completed.

**Why not fixed as part of ticket 39:** Out of scope - ticket 39 only adds the Rectangle built-in and had no reason to touch placement timing. A correct fix likely needs `ClickToAdd`/`PlaceComponent` to no-op (or queue) until `ContainerWidth`/`ContainerHeight` are known non-zero, rather than trusting whatever `_zoomPanTracker.Viewport` currently reports.

**Workaround used in ticket 39:** none in product code; the flaky test run was simply retried until it captured the correct, centered position before accepting the new baseline.
