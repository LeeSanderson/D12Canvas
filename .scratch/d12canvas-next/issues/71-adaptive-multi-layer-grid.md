# 71 — Adaptive multi-layer grid

**What to build:** An end user always has a position/scale reference: the grid runs concurrent layers stepping by 10x spacing, each crossfading in and out as zoom crosses its legibility threshold — simulating infinite depth in either zoom direction. Replaces the old fixed grid. Purely a rendering concern of the canvas: not board content, never persisted. (ADR 0011.)

**Blocked by:** 69 (Unbounded extent & zoom, configurable limits)

**Status:** ready-for-agent

- [ ] Grid layers step by 10x spacing and render concurrently
- [ ] Layers crossfade smoothly as zoom crosses each legibility threshold — no popping
- [ ] The grid reads correctly at extreme zoom in both directions
- [ ] The grid is not part of the `Board` and never persists
- [ ] Screenshot cases at several zoom levels spanning at least two layer transitions
