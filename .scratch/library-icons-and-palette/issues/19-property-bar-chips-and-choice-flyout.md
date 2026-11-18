# 19. Property bar value chips and choice flyout

**What to build:** the `Property bar` becomes a row of value chips. A choice role such as text align or routing shows its current option's glyph and name, and clicking it opens a flyout listing every option with its glyph, the current one marked. A colour role shows its glyph painted in the current colour and the hex in upper case, with a hatch when mixed and a dashed outline for the theme colour. Number roles stay a glyph beside a small field, showing "–" when mixed. A host property with `OptionGlyphs` gets a chip and flyout like the built-ins. Colour chips get their flyout in the next ticket.

See the spec's "Property bar", "Consumer icon contract" and "Library tooltip and flyout" sections.

**Blocked by:** 16 (Palette "…" overflow flyout), 17 (OptionGlyphs and PropertyPanel tiles)

**Status:** resolved

- [x] The bar reads glyphs from the declaring property's `OptionGlyphs` (and from `EdgeRole` entries for edge rows); the library keeps no glyph table per role; the hand-drawn path helpers are gone
- [x] A choice chip shows the option's glyph, its name and `Glyphs.Caret`; a value with no glyph shows its name alone; "Mixed" shows in the muted token
- [x] A colour chip shows its role glyph with `--d12-icon-accent` set inline to the value, plus the hex in upper case; mixed draws the hatch and theme colour the dashed muted outline; "Theme" and "Mixed" show in the muted token
- [x] Every chip is a menu button (`aria-haspopup="menu"`, `aria-expanded`) named "Label: value"; chips have no tooltip
- [x] Clicking a choice chip opens the library flyout under it: the role's label as a small uppercase heading, one `menuitemradio` row per option with glyph and name, the current one checked and none when mixed; an option without a glyph keeps an empty glyph column
- [x] Picking a row commits one edit and closes; Esc and an outside click close and return focus to the chip; opening moves focus into the flyout
- [x] Number roles stay a glyph beside a 40px field; a mixed number shows the placeholder "–" and its accessible name still says Mixed; number fields use the library tooltip, placed above the bar
- [x] The existing clamp rule still keeps the wider bar inside the container
- [x] bUnit: chip names and values, mixed and theme states, flyout rows and checked state, a host property with `OptionGlyphs` gets a chip and flyout, "–" for a mixed number
- [x] Probe: a chip opens its flyout and moves focus into it; Esc and an outside click close it and return focus to the chip; choosing an option commits and closes
- [x] New screenshot case: the option flyout open, in light and dark; the hatched-fill bar case gains a dark twin
- [x] `CONTEXT.md`'s **Property bar** entry describes the chips
- [x] The full visual suite passes with Property bar baselines updated in this commit

## Comments

- Colour chips draw the chip (role glyph painted through `--d12-icon-accent`, upper-case hex, caret, hatch for mixed, dashed muted outline for theme) but stay a native colour input over the chip until ticket 20 gives them the flyout. So they are not menu buttons yet; only choice chips carry `aria-haspopup="menu"` and `aria-expanded`. Their accessible name is already "Label: value".
- The native `title` is gone from the bar, so the old "(Delete to use the theme colour)" hint no longer shows anywhere. Delete and Backspace still work.
- `PropertyBarRow` gains `OptionGlyphs`, taken from the representative property for instance rows and from the `EdgeRole` entry for edge rows. The role glyphs for colour and number roles (Fill, Stroke, Text colour, Colour, Stroke width, Font size) come from a switch in `PropertyBar`.
- The bar measures itself again whenever a shown value changes, not only when its rows change, because a chip's width follows its text.
- Choice cells keep their plain row id as the key, so a pick from a mixed row leaves the chip in place for the flyout to hand focus back to.
- `DiagramCanvas` cascades `D12IconStyles.InScope` around the bar. The bar carries `Tooltip.Rules` and `Flyout.Rules`.
- Clamp check: the existing rule still holds a bar that fits. A narrowed 400px pane holds the 365px edge bar, which a probe checks. A container narrower than the bar plus 16px can't hold it: the bar pins to the left margin and overflows the right edge. The sticky note's 5-role bar, about 440px, is clipped this way in the 420px inline-edit demo pane (`InlineTextEditingVisualTests.StickyNoteInEditingState`). No new rule was added.
- The flyout opens under the chip and can be clipped by the container's `overflow: hidden` when the selection is near the bottom. The flyout screenshot cases frame the edge first (Shift+2) for this reason. The first full run showed it clipped.
- A probe pressed outside the flyout before its JS had registered, so the press was missed. The probes now wait for focus to land in the flyout before pressing.
- Baselines: 27 cases move and 3 are new: the option flyout in light and dark, and the hatched-fill dark twin. All 27 moved cases show a selected bar, across the Property bar, Property panel, mixed values, ports, edges, inline edit and quick create pages. In each PNG only the bar's strip changes. Outside the bar, each HTML diff is a single `<!--!-->` marker from the new `InScope` cascade. The `MultiSelectionMoveResize` `aria-selected` noise did not appear in this run.
