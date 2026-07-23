# 74 — Token adoption across all remaining chrome

**What to build:** Every remaining piece of library-owned chrome — the palette, LOD placeholder, connector drag-preview, and context menu — reads the shared theme tokens, each declaring its own token defaults on its own root so it works standalone. Two canvas instances on one page can carry different themes, and a host override on a shared ancestor themes everything consistently via ordinary CSS inheritance. Component instances' own visuals stay untouched — they're props, not tokens. (ADR 0012.)

**Blocked by:** 26 (Palette lists registered types), 48 (Drag port-to-port creates an edge), 62 (Selection context menu), 70 (LOD placeholder), 73 (Theme-token layer + light/dark switching)

**Status:** ready-for-agent

- [ ] Palette, LOD placeholder, connector drag-preview, and context menu read tokens exclusively
- [ ] Each chrome component declares its token defaults on its own root and renders correctly standalone
- [ ] Two canvases on one page can carry different `data-d12-theme` values simultaneously
- [ ] A token override on a shared ancestor themes all chrome consistently
- [ ] Component-instance visuals (props-driven) are unaffected by theme switching
- [ ] Screenshot cases: each chrome element in light and dark, plus the two-themes-one-page scenario
