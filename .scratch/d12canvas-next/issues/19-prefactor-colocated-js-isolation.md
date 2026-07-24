# 19 — Prefactor: colocated JS isolation for library interop

**What to build:** A host developer drops the library into any consuming app and every script-backed behaviour just works, with no path configuration. All library JS interop loads via Blazor's colocated JS-isolation convention instead of hand-rolled host-relative paths — eliminating by construction the silent 404 that currently breaks click-outside behaviour in host apps (tracked in the `component-container-js-import-404` spec). This sets the interop pattern every later ticket follows.

**Blocked by:** None — can start immediately.

**Status:** resolved

- [x] No dynamic-import 404s appear in the browser console when the Demo app loads pages using library components
- [x] Click-outside-exits-edit-mode works in the Demo app (previously silently broken)
- [x] All library scripts are colocated with their owning component per the JS-isolation convention; no host-relative script paths remain
- [x] Existing bUnit tests still pass

## Comments

`ComponentContainer.razor.js` and `DiagramCanvas.razor.js` now sit colocated next to their `.razor`/`.razor.cs` files and are imported via `_content/D12Canvas/{Component}.razor.js` (Blazor's colocated JS-isolation URL convention), replacing the host-relative `./js/componentContainer.js` path and the manually-added `<script>` tag for `DiagramCanvasInterop.js` in the Demo app's `index.html`. `DiagramCanvas` now holds and disposes an `IJSObjectReference` module the same way `ComponentContainer` already did, which required switching it from `IDisposable` to `IAsyncDisposable` — this also fixed a latent bug where `DiagramCanvas` defined a `Dispose()` method but never actually implemented `IDisposable`, so Blazor never called it and its cleanup (event unsubscription, resize/keyboard listener teardown) was dead code.

Verified via new bUnit coverage (`ComponentContainerTests`, `DiagramCanvasTests.DiagramCanvas_ImportsColocatedJsModule`) and a live headless-Chromium run against the Demo app's `/componentcontainer-demo` page: zero console/page errors, and double-click-to-edit → click-outside now correctly exits edit mode.
