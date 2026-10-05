# 69 — Property panel theming and raised values

**What to build:** The property panel follows the dark theme instead of rendering as a white box on a dark host (ADR 0064). It joins the token layer by declaring the raised value set on its own root (the same four blocks `Palette` declares plus a text colour) and `color-scheme`, so its native inputs render in the right scheme and inherit the surface's text colour. A host overriding tokens on an ancestor themes the panel along with everything else.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] The panel's root declares the raised token values for light, `prefers-color-scheme: dark`, and both `data-d12-theme` overrides, matching the four-block shape `Palette` uses
- [ ] The panel declares `color-scheme` and its inputs read the surface and inherit text colour
- [ ] A theme-tokens bUnit test for the panel mirrors the existing palette and menu token tests
- [ ] A demo page shows the panel over a dark board so the visual suite covers the dark state; the panel's existing light baseline moves only in input text colour (about four baselines expected)
- [ ] Full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Raised values` term describes what shipped
