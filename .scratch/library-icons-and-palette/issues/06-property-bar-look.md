# Property bar look

Type: prototype
Status: resolved
Blocked by: 02
Map: [Library icons, Palette and Property bar](../map.md)

## Question

What should the `Property bar` look like with icons from the chosen set in place of today's hand-drawn glyphs?

Build a throwaway Demo route. Settle:

- the icon for each `PropertyRole`
- how a value shows beside its icon: a colour swatch, a number, the current choice
- which roles become segmented or toggle controls in the bar itself (text align, vertical align, font weight, edge routing, arrowheads)
- how mixed values and locked state look

## Comments

From [The consumer icon contract](04-consumer-icon-contract.md): the bar reads option glyphs from the declaring property's `OptionGlyphs`, not from a table per role. A value with no glyph, such as a consumer's extra option, shows its option text in the cell, so the prototype should show that case.

From [Palette look](05-palette-look.md): the Palette names its icon-only buttons with a library tooltip, not the native `title`. It shows on hover and keyboard focus, beside the control rather than over it. If the bar's icon cells get tooltips, decide whether they use the same one. The prototype's switcher and tooltip CSS are on the `prototype/palette-look` branch.

## Answer

The `Property bar` becomes a row of value chips. I prototyped four layouts across nine selections, in light and dark: glyph cells (today's shape with the new glyphs), inline segments and toggles, value chips with flyouts, and grouped flyouts. The chips won.

- **Chip.** Each colour and choice role is one button: its glyph, its current value as text, and a small caret. Colours show the glyph painted in the value plus the hex in upper case ("#4F8CFF"). Choices show the current option's glyph from `OptionGlyphs` plus the option name ("Center", "Orthogonal"). "Theme" and "Mixed" show in the muted token. A value with no glyph, such as a consumer's `justify`, shows its name alone. The chip's accessible name is "Label: value", for example "Fill: #4F8CFF".
- **Number roles** stay as today: the glyph beside a small field, no flyout. A mixed number shows the placeholder "–", because "Mixed" doesn't fit the 40px field. Its accessible name still says Mixed, and the `PropertyPanel` keeps "Mixed".
- **Choice flyout.** Clicking a chip opens a library flyout under it: a small uppercase heading with the role's label, then one row per option, each its glyph and name, with the current one marked. An option with no glyph keeps an empty glyph column so the names line up. Picking a row commits and closes. Esc closes too. The chip is a menu button (`aria-haspopup="menu"`, `aria-expanded`) and the rows are `menuitemradio`. On a mixed row nothing is checked.
- **Colour flyout.** A grid of preset swatches, then a "Theme colour" row when every target can hold `null`, then "Custom…", which opens the native picker. The library ships a default preset list, and the host can replace it with its own, for example brand colours. The option's name and shape are left to the spec. The swatches are `menuitemradio` too.
- **Glyphs.** The bar's role glyphs (Fill, Stroke, Text colour, Edge colour, Stroke width, Font size) and every option glyph come from the shared set, with the accent part carrying the colour. The prototype drew rough versions; the final drawings follow the drawing rules.
- **Partly locked.** The bar ends with a divider, a lock glyph and the count ("1/3"). It isn't a button. Its library tooltip says "1 of 3 locked; changes skip them".
- **Fully locked.** This changes today's rule, which hides the bar. The bar stays where it would stand but holds only an **Unlock** button, its glyph and the word, and no role chips. Unlock unlocks the selection in one history entry, as the `PropertyPanel`'s Unlock does.
- **Tooltip.** Chips need none, because their value is visible text. The number cells and the lock count use the Palette's library tooltip, which shows on hover and keyboard focus, never as the native `title`. In the bar it sits above the bar, not to the right.
- **Width.** Chips make the bar wider than today. The sticky note's seven roles take about 440px against about 190px now, so clamping inside the container matters more. Nothing new is decided about that here; the existing clamp rule stands.

The prototype is on the throwaway branch `prototype/property-bar-look` (`/prototype/property-bar-look` in the Demo, `?variant=C`). Do not merge it.
