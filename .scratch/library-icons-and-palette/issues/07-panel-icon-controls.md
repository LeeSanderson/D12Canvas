# Panel icon controls

Type: grilling
Status: resolved
Blocked by: 06
Map: [Library icons, Palette and Property bar](../map.md)

## Question

Which `PropertyPanel` controls become icon controls, and how do they behave?

The layout stays a label-and-control list. Decide:

- which `EditorKind` and `PropertyRole` combinations become segmented buttons, toggles or icon lists, following the choices made in Property bar look
- how a mixed value and a locked selection show on an icon control
- the keyboard and screen-reader behaviour of each control (radio group, toggle button)

## Comments

From [Icon inventory](01-icon-inventory.md): the `PropertyPanel` edits component instances only. It has no edge fields, so edge routing and edge arrows aren't panel controls today. Making them icon controls would first mean adding edges to the panel, which may sit past the destination.

From [The consumer icon contract](04-consumer-icon-contract.md): the trigger for an icon control is data, not a new `EditorKind`. A Dropdown whose every option has an entry in `EditableProperty.OptionGlyphs` gets icon buttons in place of a `<select>`. Built-in and consumer properties take the same route, so whatever this ticket decides for the buttons applies to consumer properties too.

From [Property bar look](06-property-bar-look.md): the bar chose value chips. A choice role is a menu button showing its glyph and option name, and it opens a flyout of glyph-and-name `menuitemradio` rows. It is not a segmented control. Decide whether the panel's icon buttons follow that or stay segmented, since the panel has room for a whole row. A mixed number shows "–" in the bar but keeps "Mixed" in the panel. The bar now stays for a fully locked selection as a lone Unlock button.

## Answer

The panel draws a glyphed Dropdown as labelled tiles. I prototyped four controls over six selections, in light and dark: an icon-only segmented row, the bar's chip and flyout, labelled tiles, and an inline label beside a segmented row. The tiles won. The panel and the bar now differ on purpose: the bar has no room and uses chips; the panel has a full row and shows every option at once.

- **Trigger.** Unchanged from The consumer icon contract: a Dropdown whose every option has an entry in `OptionGlyphs` draws tiles in place of the `<select>`. Built-in and consumer properties take the same route. Without glyphs it stays a `<select>`, as Image `Fit` does.
- **Tile.** Each option is one tile: its glyph above its name in 10px text. The tiles sit in a grid under the label and wrap onto more rows when there are many. There is no tooltip and no fallback to a chip or flyout, whatever the option count. The current option's tile is pressed.
- **Width.** The panel sets the grid's minimum column width from the longest option name, so long names get fewer, wider columns and no name is cut. A name wider than half the panel still ends in an ellipsis as a last resort. The 10-option Shape example becomes two columns and five rows; that height is the consumer's choice.
- **Mixed.** No tile is pressed and the label carries an italic "Mixed" tag. The panel does not mark which values the selection holds. Picking a tile sets every target to that value.
- **Locked.** Today's rule stands. A fully locked selection shows Unlock above the fields and disables every tile; the pressed tile stays drawn as pressed so the value can still be read. The panel shows no partial-lock count; that question is in the map's fog.
- **Keyboard and screen reader.** The tiles are a `radiogroup` named by the field's label (`aria-labelledby`). Each tile is a `radio` with `aria-checked`, and its visible name is its accessible name. Tab lands once on the group: on the checked tile, or on the first tile when the value is mixed. Arrow keys move focus between tiles and wrap, but do not commit. Space or Enter commits, so one choice is one edit and one undo entry. This departs from the ARIA radio pattern on purpose, to keep history clean, and matches the bar's flyout, where moving through rows doesn't commit. Disabled tiles leave the tab order.
- **Edges.** The panel has no edge fields, so edge routing and arrowheads stay out of the panel. That is out of scope for this map.

The prototype is on the throwaway branch `prototype/panel-icon-controls` (`/prototype/panel-icon-controls` in the Demo, `?variant=C`). Do not merge it.
