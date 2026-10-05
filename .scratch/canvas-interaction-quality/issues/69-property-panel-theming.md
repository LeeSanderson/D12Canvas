# 69 — Property panel theming and raised values

**What to build:** The property panel follows the dark theme instead of rendering as a white box on a dark host (ADR 0064). It joins the token layer by declaring the raised value set on its own root (the same four blocks `Palette` declares plus a text colour) and `color-scheme`, so its native inputs render in the right scheme and inherit the surface's text colour. A host overriding tokens on an ancestor themes the panel along with everything else.

**Blocked by:** None — can start immediately

**Status:** resolved

- [x] The panel's root declares the raised token values for light, `prefers-color-scheme: dark`, and both `data-d12-theme` overrides, matching the four-block shape `Palette` uses
- [x] The panel declares `color-scheme` and its inputs read the surface and inherit text colour
- [x] A theme-tokens bUnit test for the panel mirrors the existing palette and menu token tests
- [x] A demo page shows the panel over a dark board so the visual suite covers the dark state; the panel's existing light baseline moves only in input text colour (about four baselines expected)
- [x] Full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Raised values` term describes what shipped

## Comments

`.d12-property-panel` now declares `Palette`'s raised values in the same four blocks (light default, `prefers-color-scheme: dark`, and the two `data-d12-theme` overrides), with `color-scheme` set in each, `color: var(--d12-text)` on the root, and every former literal replaced by a token: the surface and border on the root and on `.d12-property-panel-input`, muted text on labels and the empty message. Inputs set `background: var(--d12-surface)` and `color: inherit`, so dark Chrome stops filling them with its own field grey and the text follows the surface. The light values equal the old literals byte for byte.

The decision also had `Palette` and the context menu adopt `color-scheme` in the same change, and this ticket's commit did that. The next ticket's commit took it back off those two, first on a wrong diagnosis and then on a right one. The wrong one: the run after this ticket showed five pixels at the palette's rounded corner differing by one unit on nine screenshots, which looked like `color-scheme` had made the corner render nondeterministically. The right one: the repo's own PNG comparer reports zero differing pixels for those files, so they were never failures. Verify writes a received copy of every target when a test's HTML target fails, and this one-unit anti-aliasing noise at that corner has always been there and always been absorbed; it only became visible once HTML baselines started moving. The declaration stays off the palette and the menu because neither has a form control and it does nothing for them; putting it back is one line per block if a later surface wants it. The panel keeps it, because its inputs are the reason it exists. `PropertyPanelThemeTokensTests` mirrors `PaletteThemeTokensTests` and asserts the scheme in every panel block.

The dark state is covered by the existing property-panel demo under dark colour-scheme emulation, which is what gives a dark board under a dark panel without a new page: `PropertyPanelVisualTests` takes `PaletteVisualTests`' `NewPageAsync(ColorScheme)` shape and gains `PopulatedPanel_DarkColorScheme` (Rectangle: number and colour inputs) and `PopulatedPanelWithDropdownControl_DarkColorScheme` (Text: the closed select). No dark empty or custom-control baseline, for the reasons the decision gives.

Two small corrections to the decision's wording, noted rather than silently absorbed. The decision describes the Rectangle dark baseline as covering "text, number and colour inputs"; the Rectangle has no text-kind property (no built-in does, since every text field is edited inline), so that baseline shows number and colour inputs only. And the ticket asks for "a demo page" showing the panel over a dark board; the decision's own verification uses the existing demo under dark colour-scheme emulation instead, which gives a dark board and a dark panel without a page nobody else needs. That demo's own `#ccc` container border is host markup outside the library and stays light under emulation.

The glossary's `Raised values` entry already names the panel among the raised surfaces and describes the two value sets; no change was needed.

Baseline movement from the run: 35 of 73 cases, all planned or explained. Every `.verified.html` that carries a palette, menu or panel style block moved by the new `color-scheme` lines. The four light panel PNGs moved as the decision predicted, and by more than input text colour: the `<select>` controls now paint the surface token (white) where Chromium's default field grey showed before, which is the "inputs read the surface" rule doing its job in light as well as dark. Nine PNGs on pages that only carry a palette moved by the same five pixels at the palette's bottom-right corner, where Chromium anti-aliases the rounded border differently once the element declares a `color-scheme`; nothing else in those images changed. The two dark cases are new baselines, inspected: dark surface, dark inputs with light text, muted labels, a dark closed select.
