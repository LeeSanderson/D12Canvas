# 19 — Prefactor: colocated JS isolation for library interop

**What to build:** A host developer drops the library into any consuming app and every script-backed behaviour just works, with no path configuration. All library JS interop loads via Blazor's colocated JS-isolation convention instead of hand-rolled host-relative paths — eliminating by construction the silent 404 that currently breaks click-outside behaviour in host apps (tracked in the `component-container-js-import-404` spec). This sets the interop pattern every later ticket follows.

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] No dynamic-import 404s appear in the browser console when the Demo app loads pages using library components
- [ ] Click-outside-exits-edit-mode works in the Demo app (previously silently broken)
- [ ] All library scripts are colocated with their owning component per the JS-isolation convention; no host-relative script paths remain
- [ ] Existing bUnit tests still pass
