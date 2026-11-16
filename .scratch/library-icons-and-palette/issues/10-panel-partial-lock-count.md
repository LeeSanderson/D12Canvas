# Panel partial-lock count

Type: grilling
Status: resolved
Map: [Library icons, Palette and Property bar](../map.md)

## Question

Does the `PropertyPanel` show a partly locked selection's lock count?

Today the panel edits the unlocked targets and skips the rest without saying so. [Property bar look](06-property-bar-look.md) ended the bar with a lock glyph and "1/3", with the tooltip "1 of 3 locked; changes skip them". [Panel icon controls](07-panel-icon-controls.md) left the panel's side open. Decide whether the panel shows the count, and if so where and in what words.

## Answer

Yes. A partly locked selection gets one line of text in the `PropertyPanel`. Without it, the same selection looks partly locked in the bar and fully editable in the panel, and an edit from the panel changes two of three shapes with no reason given.

- **When.** Only when some targets are locked and some aren't. A fully locked selection keeps today's Unlock button above the fields and gets no line. With nothing locked, the panel shows neither.
- **Where.** Above the fields, in the slot the Unlock button uses.
- **What.** The lock glyph from the shared set, then "1 of 3 locked; changes skip them" in the muted token. It's the bar tooltip's wording, written out in full because the panel has room. The panel has no tooltip and doesn't use the bar's short "1/3".
- **Not a control.** It's plain text with no action, like the bar's lock count. To unlock part of a selection, you use the context menu. An action here would put a second control above the fields, next to the ones that set properties.
- **Screen reader.** Plain text in reading order, the first thing in the panel region. It has no live region, because selection changes too often, and no `aria-describedby` on the fields, because every field read out would repeat it.
