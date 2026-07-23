# 70 — LOD placeholder

**What to build:** An end user zooms far out on a dense board and it stays fast and legible: any component instance whose on-screen size (bounds × zoom) drops below a host-configurable threshold renders the generic, non-interactive LOD placeholder — a plain box built from the type's registered `DisplayName`/`Icon` — instead of mounting its full Razor component tree. Zooming back in swaps the real component back. (ADR 0011 — windowing alone can't bound a dense, zoomed-out board.)

**Blocked by:** 69 (Unbounded extent & zoom, configurable limits)

**Status:** ready-for-agent

- [ ] Instances below the on-screen size threshold render the generic placeholder from `DisplayName`/`Icon`
- [ ] Placeholders are non-interactive; zooming in restores the full component seamlessly
- [ ] The threshold is host-configurable with a sensible default
- [ ] The stress-test page shows the frame-rate benefit on a dense, zoomed-out board
- [ ] Screenshot case for a board of placeholders
