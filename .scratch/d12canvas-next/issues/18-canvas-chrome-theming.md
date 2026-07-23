# Canvas-chrome-level visual theming

Type: grilling
Status: resolved
Blocked by: 05, 12, 17

## Question

Ticket 12 (styling/theming) decided per-component-instance visual props stay ordinary `TProps` fields with no separate theming model. That left open the visual appearance of chrome/rendering the library itself owns — surfaced while resolving ticket 17 (canvas scale and size limits), which introduced the adaptive multi-layer grid and the LOD placeholder.

Decide the mechanism governing the visual appearance of all library-owned chrome — the grid, the LOD placeholder, the palette (ticket 05), the selection marquee (ticket 09), the connector drag-preview (tickets 08/14), and the selection context menu (ticket 14). Specifically: CSS vs. a C# API surface; which chrome elements are in scope; whether D12Canvas ships a default theme (or themes); how light/dark switching works; where token definitions are scoped given `Palette` and `DiagramCanvas` mount as independent components; and whether this warrants its own ADR.

## Answer

**Mechanism:** pure CSS — no C# theming API surface. A host restyles chrome the same way it restyles any DOM node it doesn't own: CSS custom properties.

**Scope:** all library-owned chrome, unified under one contract — grid, LOD placeholder, palette, selection marquee, connector drag-preview, and selection context menu. Not just the three items the fog note originally named; once the mechanism is "plain CSS custom properties," extending it to every chrome surface costs nothing extra and avoids rediscovering "oh, this is chrome too" piecemeal in future tickets (exactly how this fog note itself got surfaced).

**Structure:** a small shared semantic token layer (`--d12-surface`, `--d12-border`, `--d12-accent`, `--d12-muted-text`, and similar) reused across all chrome elements, rather than one-off properties per element per visual detail. Per-element custom properties remain available as an escape hatch for the rare element that genuinely needs to diverge from the shared token.

**Defaults:** D12Canvas ships two built-in value sets for the token layer — a default light theme and a default dark theme — so a host that overrides nothing still gets a fully, consistently styled canvas, consistent with the "usable out of the box" precedent (ticket 05 / ADR 0002).

**Switching:** dual mechanism. Defaults to the `prefers-color-scheme: dark` media query; a host with its own light/dark toggle can force either mode explicitly via a `data-d12-theme="light"|"dark"` attribute (or equivalent), which takes precedence over the OS preference.

**Scoping:** `DiagramCanvas` and `Palette` mount as independent components (ADR 0002) rather than a shared DOM subtree, so each declares its own copy of the token defaults on its own root element — not a single global `:root`. Every component is correctly themed standalone, and two independent canvas instances on one page can carry different themes. A host wanting one consistent theme across both simply overrides the tokens on a shared ancestor and lets ordinary CSS inheritance carry it down into both — no bespoke wrapper contract required.

**ADR:** new ADR 0012 — this is an independent mechanism, not a narrowing of ADR 0008 (which decided per-instance props get *no* separate theming model; this decision is the mirror image — chrome *does* get a dedicated, CSS-only theming model).

**Considered and rejected:**
- A C# `CanvasTheme` cascading-parameter object — rejected; duplicates what plain CSS custom properties already do natively, adding surface area for something purely cosmetic.
- Global `:root`-scoped tokens only — rejected; prevents two independently-themed canvas instances coexisting on one page, and would make the standalone `Palette` depend on document-global state.
- No built-in default theme (contract-only, host supplies all values) — rejected; leaves an unstyled canvas as the out-of-box experience.
- Automatic (`prefers-color-scheme`) dark mode only, with no host override — rejected; most embedding hosts already have their own theme toggle, and tying dark mode solely to the OS preference would fight it rather than cooperate with it.
- Folding this into ADR 0008 — rejected; 0008 decided the opposite shape of this question, so keeping them separate keeps each ADR's rationale unambiguous.

Seeded ADR 0012 and a new `CONTEXT.md` term (Theme token).
