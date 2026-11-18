# 15. Palette icon rail

**What to build:** the `Palette` becomes a single column of icon-only buttons. Edge comes first with its own glyph and is called "Edge". The built-in types follow, then the host's types, with a divider between categories. Hovering or focusing a button shows its name in a library tooltip to the right of the rail. Screen readers hear a vertical toolbar named "Component palette", and arrow keys move through it. The rail shows at most 8 registered types; the overflow button for the rest comes in the next ticket, so this ticket simply stops at 8.

See the spec's "Palette" and "Library tooltip and flyout" sections.

**Blocked by:** 14 (Component types carry a Glyph)

**Status:** resolved

- [x] The rail is one column of 36px square buttons with 18px glyphs on the Palette's raised surface; the host still places it and the App keeps its top-left overlay
- [x] Order is Edge, a divider, then registered types with built-ins before host types; a divider separates categories; uncategorised types stay in `Uncategorized`
- [x] The Edge entry draws `Glyphs.Edge` and is named "Edge" in its tooltip and accessible name; "Connector" no longer appears
- [x] The rail shows the first 8 registered types; Edge doesn't count towards the 8
- [x] Each button's accessible name is the registration's `AccessibleName` and its glyph is decorative
- [x] A library tooltip shows `DisplayName` 12px to the right of the rail, centred on the button, on hover and on keyboard focus, and never through the native `title`; the tooltip is reusable with a caller-set side, since the Property bar will place it above
- [x] The rail is a vertical `toolbar` named "Component palette" and follows the WAI-ARIA toolbar keyboard pattern
- [x] Clicking and dragging a rail button add or place the type as today
- [x] bUnit: rail order, the 8-type limit, accessible names, toolbar role and keyboard movement
- [x] Probe: a rail button has an accessible name and shows its tooltip on hover and on focus
- [x] `CONTEXT.md`'s **Palette** entry describes the rail
- [x] The full visual suite passes with Palette baselines (including the old connector entry case) moved in this commit

## Comments

- `AddD12Canvas` now registers the built-ins before running the host's configure callback, so the registry order (and the rail) is built-ins first.
- The 8-type limit counts in the rail's displayed order, which groups by category: a type registered late into an earlier category can take a slot from an earlier-registered type in a later category. Ticket 16's overflow builds on this; revisit there if the plain registration order is wanted.
- The library tooltip is `Tooltip.Around(text, TooltipSide, control)`, an internal static helper that returns a `RenderFragment`. Razor components are always public and Razor does not discover an internal C# component, so a fragment is the way to keep it internal. The root that uses it carries `Tooltip.Rules` once and can set `--d12-tooltip-offset`.
- `D12Icon` no longer brings its own `<style>` inside a library root. `Palette` and `DiagramCanvas` carry `D12IconStyles.Rules` once and cascade `D12IconStyles.InScope`; an icon outside any root (a host page, the glyph gallery) still emits its own copy. `ComponentTestBase.StyleBlockText` is back to the first `<style>` block.
- The arrow keys move focus with `ElementReference.FocusAsync` and no JS module, so they are not `preventDefault`ed: on a scrollable page Up and Down also scroll it.
- The App's palette overlay drops its fixed 180px width and its scroll clipping so the tooltip can sit outside it.
- Baselines: 40 screenshot cases move their PNG and HTML. 39 are Demo pages with a Palette, where only the Palette region changes. The 40th is the themed-defaults page, which has no Palette: its dark pane picks up the dark canvas frame `#444` instead of `#ccc`. HEAD renders `#ccc` there, so putting the icon rules in the canvas's style block changed which frame value Chromium applies. `#444` is the dark frame token the canvas declares, and ticket 14 saw the same move on three other dark-board pages. 56 more cases move their HTML only, by whitespace and Blazor `<!--!-->` markers around `D12Icon`'s style block and the new cascade; their PNGs are unchanged.
