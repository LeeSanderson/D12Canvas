# 73 — Theme-token layer + light/dark switching

**What to build:** A host developer rethemes the canvas with plain CSS: a shared set of theme tokens (CSS custom properties — surface, border, accent, muted text, …) that library-owned chrome reads for its default visuals, with no C# theming API. Ships default light and dark themes: `prefers-color-scheme` picks the default, and a `data-d12-theme` attribute lets the host's own theme switcher override it. Token defaults are declared on the canvas's own root — not a global `:root`. The grid and selection marquee adopt the tokens as the first consumers. (ADR 0012.)

**Blocked by:** 32 (Marquee + shift-click multi-select), 71 (Adaptive multi-layer grid)

**Status:** ready-for-agent

- [ ] The shared token set is defined and documented for hosts
- [ ] Light and dark defaults ship; `prefers-color-scheme` selects between them
- [ ] `data-d12-theme` on the canvas or an ancestor overrides the OS preference
- [ ] Token defaults live on the canvas's own root, not `:root`
- [ ] Grid and marquee read tokens exclusively — no one-off colour properties remain in them
- [ ] Screenshot cases in both light and dark
