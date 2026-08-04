# 74 — Token adoption across all remaining chrome

**What to build:** Every remaining piece of library-owned chrome — the palette, LOD placeholder, connector drag-preview, and context menu — reads the shared theme tokens, each declaring its own token defaults on its own root so it works standalone. Two canvas instances on one page can carry different themes, and a host override on a shared ancestor themes everything consistently via ordinary CSS inheritance. Component instances' own visuals stay untouched — they're props, not tokens. (ADR 0012.)

**Blocked by:** 26 (Palette lists registered types), 48 (Drag port-to-port creates an edge), 62 (Selection context menu), 70 (LOD placeholder), 73 (Theme-token layer + light/dark switching)

**Status:** resolved

- [x] Palette, LOD placeholder, connector drag-preview, and context menu read tokens exclusively
- [x] Each chrome component declares its token defaults on its own root and renders correctly standalone
- [x] Two canvases on one page can carry different `data-d12-theme` values simultaneously
- [x] A token override on a shared ancestor themes all chrome consistently
- [x] Component-instance visuals (props-driven) are unaffected by theme switching
- [x] Screenshot cases: each chrome element in light and dark, plus the two-themes-one-page scenario

## Comments

`Palette.razor` and `SelectionContextMenu.razor` each gained their own copy of the four
shared tokens (`--d12-surface`/`--d12-border`/`--d12-accent`/`--d12-muted-text`) plus light/dark
media-query and `data-d12-theme` override blocks, mirroring `DiagramCanvas`'s existing pattern
exactly (ticket 73). Light defaults reproduce each component's pre-existing hard-coded colors
byte-for-byte (`#fff`/`#ccc`/`#666` for the palette and context menu); dark defaults are new
(`#2a2a2a`/`rgba(255,255,255,0.12)`/`#a0a0a0`). Hover states (`:hover` background/border on both
the palette entry button and the context-menu item) now use
`color-mix(in srgb, var(--d12-border) 25%, var(--d12-surface))`, the same technique ticket 73 used
for the marquee fill, rather than a third one-off hover color.

The LOD placeholder and connector drag-preview live inside `DiagramCanvas`'s own
`.diagram-container` subtree (not separate components), so they just needed their hard-coded
colors swapped for `var(--d12-*)` references — they already inherit the canvas's token
declarations, same as the grid/marquee did in ticket 73. The connector drag-preview's green is a
deliberate departure from `--d12-accent` (which already means "selected" elsewhere on the canvas),
so it's routed through its own escape-hatch custom property, `--d12-connector-preview`, declared
once on `.diagram-container` (unconditionally — it doesn't participate in light/dark switching)
rather than reusing accent or leaving a bare literal in the consuming rule.

**Real bug found while adopting tokens, not part of the original checklist:** both
`.d12-palette-entry-button` and `.d12-context-menu-item` set `color: inherit` (needed to override
the `<button>` element's own UA default), but neither `.d12-palette` nor `.d12-context-menu` had
ever set an explicit `color` of their own — entry names and menu-item labels silently fell through
to whatever ambient body text color the host page happened to have (Bootstrap's near-black
`#212529` in the Demo app). That was invisible as a bug while the palette/menu were always white,
but the moment they could render with a dark background (this ticket), it became a real,
reproducible accessibility failure: near-black text on a near-black `#2a2a2a` panel, confirmed via
`getComputedStyle` in a real dark-mode Playwright page before the fix (`color: rgb(33, 37, 41)`
against `backgroundColor: rgb(42, 42, 42)`) and visually confirmed illegible in the first round of
screenshot baselines. Fixed by adding a fifth shared token, `--d12-text` (light `#212529`,
dark `#e8e8e8`), declared on all three independently-mounted roots (`DiagramCanvas`, `Palette`,
`SelectionContextMenu` — added to `DiagramCanvas` too for token-set symmetry across all three,
even though no `DiagramCanvas`-owned chrome currently needs a primary-text color, same as how
`--d12-muted-text` shipped in ticket 73 before this ticket became its first real consumer) and set
as `color: var(--d12-text)` on each component's own root, so it flows down to entry
names/menu-item labels through ordinary CSS inheritance without needing every leaf selector
touched individually.

A new `/two-themes-demo` Demo page (deliberately left out of `NavMenu.razor`, matching the
existing unlinked-page precedent set by `EdgeStylesDemo`/`PropertyPanelDemo` — adding a NavMenu
entry would have widened this ticket's Playwright diff to essentially every baseline in the suite,
since the nav sidebar is part of every page's full-page screenshot) mounts two independent
`DiagramCanvas`+`Palette` pairs, each under its own `data-d12-theme` wrapper, proving the
two-simultaneous-themes requirement end to end rather than just asserting on CSS text. New
`TwoThemesVisualTests.cs` asserts `getComputedStyle(...).backgroundColor` differs correctly between
the two panes and adds a screenshot baseline.

New bUnit coverage: `PaletteThemeTokensTests.cs` and `SelectionContextMenuThemeTokensTests.cs`
(own-root defaults, dark media query, `data-d12-theme` overrides, exclusive `var()` usage — same
shape as ticket 73's `DiagramCanvasThemeTokensTests.cs`, so the token-extraction helpers moved onto
`ComponentTestBase` as shared `protected static` members rather than being copied a third time),
plus new `DiagramCanvasThemeTokensTests` cases for the LOD placeholder and the connector-preview
escape hatch. New Playwright dark-mode screenshot cases added to `PaletteVisualTests`,
`LodPlaceholderVisualTests`, `PortDragVisualTests`, and `SelectionContextMenuVisualTests`.

Every HTML baseline in the suite needed regenerating (the same class of mass, mechanical churn
ticket 73 already documented — any edit to a shared `<style>` block changes every page's full HTML
snapshot regardless of visual impact). Verified each PNG diff before promoting rather than
blanket-accepting: most measured an exact 0.0000% pixel difference under the ticket-80 fuzzy
comparer (including the LOD placeholder's light-mode recolor — `#e0e0e0`/`#b0b0b0`/`#555` versus
the token defaults `#f0f0f0`/`rgba(0,0,0,0.1)`/`#6b6b6b` register as imperceptible under
`PixelMatch`'s default threshold); the two `ThemeVisualTests` dark-mode baselines measured a real
~11% difference, visually confirmed as the intended palette dark-theming (previously always
white, regardless of `prefers-color-scheme`) rather than noise. All brand-new baselines
(palette/context-menu/LOD-placeholder/connector-preview dark, two-themes) were inspected by eye
before promoting.

Full suite verified in the pinned container from a clean `obj`/`bin` state: 718 bUnit tests passed
(1 pre-existing skip), 71/71 Playwright tests passed. `dotnet csharpier --check .` clean.

`/code-review`'s Spec pass flagged one real gap against the checklist's literal "reads tokens
exclusively": `SelectionContextMenu`'s drop shadow was still a bare `rgba(0, 0, 0, 0.15)` literal
(shadow isn't one of ADR 0012's four enumerated generic categories, so it had been left alone).
Fixed the same way as the connector-preview escape hatch — a one-off `--d12-shadow` custom
property declared once on `.d12-context-menu`'s root (theme-invariant, not redeclared per
light/dark) — rather than leaving a bare literal or forcing it into one of the four shared tokens.
The Spec pass's other observation (`--d12-accent` declared on `Palette`/`SelectionContextMenu` but
not yet consumed by either) was left as-is: it matches the exact precedent ticket 73 itself set
(`--d12-muted-text` shipped with zero consumers until this ticket), and inventing a new visual use
for accent in either component would be scope creep this ticket's checklist doesn't ask for.
