# Library icons, Palette and Property bar

Status: resolved
Map: [Library icons, Palette and Property bar](map.md)

## Problem Statement

The library's icons look different on every machine and look dated. The `Palette` draws its built-in entries with emoji and Unicode (`▭` `🗒️` `🔤` `🖼️`), so Windows, macOS and Linux each show a different picture. The Edge entry has no icon at all and is called "Connector". Each row pairs an icon with a name, and the result reads like an old toolbar. The `Property bar` draws its glyphs by hand, with stroke widths from 1 to 3.2, so the set doesn't hold together. The context menu has its own hand-built strip glyphs and a Unicode `✓`. The image placeholder and the zoomed-out LOD placeholder repeat the emoji.

A host that registers its own component types can only give them a string icon, which brings the emoji problem back. A host with a custom property can't get icon controls, and can't reuse the library's drawings in its own editors.

Two smaller gaps sit next to the looks. The `Property bar` shows colour values only as a painted glyph and gives no quick way to pick a common colour. A partly locked selection looks fully editable in the `PropertyPanel`, so an edit there changes two of three shapes and gives no reason.

## Solution

The library draws its own glyph set: about 59 SVG glyphs on a 16 grid at stroke 1.5, in one consistent style, with no CSS framework, icon font or third-party paths. One public component, `D12Icon`, renders any glyph. Hosts use the same `Glyph` type for their component icons and their option glyphs, and can render the library's glyphs in their own components.

- The `Palette` becomes a vertical rail of icon-only buttons. Edge comes first with its own glyph, then the built-ins, then host types. The rail holds 8 types, and the rest go behind a "…" flyout grouped by category. A library tooltip shows each name.
- The `Property bar` becomes a row of value chips. Each chip shows a glyph, the current value as text and a caret, and opens a flyout of options or colour presets. A host can replace the colour presets. A partly locked selection ends with a lock count. A fully locked selection keeps the bar as a lone Unlock button.
- The `PropertyPanel` draws a Dropdown whose every option has a glyph as a group of labelled tiles. A partly locked selection gets a line of text saying how many targets are locked.
- The context menu draws its strip, every row and the check mark from the shared set. The Image editor's two buttons take the matching row glyphs.
- The image placeholder and the LOD placeholder draw glyphs, not emoji.

## User Stories

### Glyph set and rendering

1. As a diagram user, I want every icon in the library to look the same on Windows, macOS and Linux, so that the editor doesn't change appearance with the OS.
2. As a diagram user, I want all icons drawn in one style, with the same grid, stroke width and round caps, so that the editor looks like one product.
3. As a diagram user, I want icons to stay sharp at 16px on a standard-density screen, so that thin lines don't blur to grey.
4. As a diagram user on a high-density screen, I want icons to stay sharp at 2x, so that they don't look soft next to text.
5. As a diagram user in dark mode, I want icons to follow the text colour of the control they sit in, so that they stay readable in both themes.
6. As a host developer, I want the library to ship no icon font, CSS framework or third-party icon paths, so that my app takes on no extra dependency or licence notice.
7. As a host developer, I want to render any library glyph with `<D12Icon Glyph="Glyphs.AlignLeft" />`, so that my own components match the library.
8. As a host developer, I want to change icon size for a whole area by setting `--d12-icon-size` on an ancestor, so that I can make the Palette or bar icons larger without forking the library.
9. As a host developer, I want to change stroke width through `--d12-icon-stroke`, so that I can tune weight for my design.
10. As a host developer, I want an icon to be decorative unless I give it a `Label`, so that screen readers read the control's name, not the picture.
11. As a host developer, I want a labelled icon to be `role="img"` with an `aria-label` and no `<title>`, so that no native tooltip clashes with the control's own tooltip.
12. As a host developer, I want accent parts of a glyph to take `--d12-icon-accent`, so that I can paint a value into a glyph in my own colour editor.
13. As a host developer, I want the glyph catalogue `Glyphs` to be public, so that I can reuse Fill, Lock or AlignLeft in my own editors.
14. As a host developer, I want to build my own `Glyph` from path data, so that my custom component types get a drawing that fits the set.
15. As a library maintainer, I want a Demo glyph gallery that shows every glyph at 1x and 2x in light and dark, so that I can judge the drawings and catch a changed path in a diff.
16. As a library maintainer, I want a test that fails when a glyph point falls outside the padding box or a glyph is missing from the gallery, so that the set stays consistent as it grows.

### Consumer icon contract

17. As a host developer, I want a component registration's `Icon` to be a `Glyph`, so that I can't bring back emoji that look different per OS.
18. As a host developer who gives no icon, I want my type to show the generic Component glyph, so that every entry still has a picture.
19. As a host developer, I want my type's glyph to show both on the Palette and on the zoomed-out LOD placeholder, so that the type is recognisable at any zoom.
20. As a host developer, I want to give a Dropdown property `OptionGlyphs` through the builder's `EditableProperties` override, so that my property gets icon controls with no new `EditorKind`.
21. As a host developer, I want my glyphed property to get tiles in the `PropertyPanel` and a chip with a flyout in the `Property bar`, so that it behaves like the built-in properties.
22. As a host developer, I want a Dropdown without `OptionGlyphs` to stay a plain `<select>`, so that existing properties keep working.
23. As a host developer, I want an option with no glyph to show its name alone in the bar, so that adding an option doesn't break the chip.
24. As a library maintainer, I want the built-in Text align, Vertical align, Routing and arrowhead properties to use `OptionGlyphs` like any host property, so that the library has no private shortcut.

### Palette

25. As a diagram user, I want the Palette as a single column of icon buttons, so that it takes little room on the board.
26. As a diagram user, I want Edge at the top of the Palette, so that drawing a connection is always one click away.
27. As a diagram user, I want the Edge entry to have its own glyph, a diagonal arrow with a dot at its start, so that it doesn't look like a text label next to icon buttons.
28. As a diagram user, I want the entry called "Edge", so that the Palette uses the same word as the rest of the editor.
29. As a diagram user, I want the built-in types before the host's types, so that the common shapes stay in the same place in every app.
30. As a diagram user, I want dividers between categories on the rail, so that related types group together without headings.
31. As a diagram user, I want to see a type's name in a tooltip to the right of its button when I hover it, so that I can tell similar glyphs apart.
32. As a keyboard user, I want the same tooltip when a Palette button gets focus, so that I learn the name without a mouse.
33. As a diagram user with many host types, I want the rail to show the first 8 and a "…" button for the rest, so that the rail never scrolls or overflows.
34. As a diagram user, I want the "…" button's tooltip to show how many more types there are, for example "More components (14)", so that I know what's behind it.
35. As a diagram user, I want the "…" flyout to show the remaining types in columns by category, each with a heading and glyph-and-name rows, so that I can scan a long list.
36. As a diagram user, I want to drag a type out of the flyout or click it to add it, just as from the rail, so that overflow types aren't second class.
37. As a diagram user, I want the flyout to close when I pick a type or press Esc, so that it doesn't stay in the way.
38. As a diagram user with 8 or fewer types, I want no "…" button, so that the rail has nothing that leads nowhere.
39. As a screen-reader user, I want each Palette button named by the type's `AccessibleName`, so that I hear "Sticky note", not a picture description.
40. As a screen-reader user, I want the rail announced as a vertical toolbar named "Component palette", so that I know where I am.
41. As a screen-reader user, I want the "…" button named "More components" with `aria-expanded`, and the flyout as a menu, so that I know it opens a list.
42. As a keyboard user, I want arrow keys to move through the rail per the WAI-ARIA toolbar pattern, so that Tab doesn't stop on every button.

### Property bar

43. As a diagram user, I want each colour role shown as a chip with its glyph painted in the value and the hex in upper case, so that I can read the exact colour.
44. As a diagram user, I want each choice role shown as a chip with the current option's glyph and name, such as "Center" or "Orthogonal", so that I can read the setting without opening anything.
45. As a diagram user, I want "Theme" and "Mixed" shown in a muted colour on a chip, so that they read as states, not values.
46. As a diagram user, I want a mixed colour drawn with a hatch and a theme colour drawn with a dashed outline in the glyph, so that both states are visible at a glance.
47. As a diagram user, I want clicking a choice chip to open a flyout under it with the role's label as a heading and one glyph-and-name row per option, so that I see every choice at once.
48. As a diagram user, I want the current option marked in the flyout, and nothing marked when the selection is mixed, so that I know what I'm changing from.
49. As a diagram user, I want picking an option to commit it and close the flyout, so that one choice takes one click.
50. As a diagram user, I want options without a glyph to line up with the rest, so that the flyout reads as a clean list.
51. As a diagram user, I want clicking a colour chip to open a grid of preset swatches, five per row, so that I can pick a common colour quickly.
52. As a diagram user, I want a "Theme colour" row in the colour flyout when every target can take the theme default, so that I can go back to the themed colour.
53. As a diagram user, I want a "Custom…" row that opens the native colour picker, so that I can pick any colour.
54. As a diagram user, I want the swatch matching the current colour checked, ignoring case, so that I can see if the value is a preset.
55. As a diagram user, I want each swatch's tooltip to show its name and hex, such as "Blue #2F80ED", so that I know what I'm picking.
56. As a diagram user, I want Esc or a click outside a flyout to close it and return focus to its chip, so that I don't lose my place.
57. As a keyboard user, I want opening a chip to move focus into the flyout, so that I can choose with arrow keys and Enter.
58. As a diagram user, I want number roles (stroke width, font size) to stay a glyph beside a small field, so that typing a number stays fast.
59. As a diagram user, I want a mixed number to show "–" in its field, so that the narrow field doesn't clip "Mixed".
60. As a screen-reader user, I want a chip named "Label: value", such as "Fill: #4F8CFF", so that I hear the role and its value.
61. As a screen-reader user, I want a mixed number field still announced as Mixed, so that "–" doesn't hide the state.
62. As a screen-reader user, I want chips announced as menu buttons and options as `menuitemradio` with checked state, so that I know how to operate them.
63. As a diagram user, I want number fields and the lock count to show a library tooltip above the bar on hover and focus, so that the tooltip doesn't cover the shape.
64. As a diagram user with a partly locked selection, I want the bar to end with a lock glyph and a count such as "1/3", with the tooltip "1 of 3 locked; changes skip them", so that I know some shapes won't change.
65. As a diagram user with a fully locked selection, I want the bar to show a single Unlock button where it usually stands, so that I can unlock without opening the panel or the menu.
66. As a diagram user, I want the bar's Unlock to unlock the whole selection in one history entry, so that one undo restores the lock.
67. As a host developer, I want to replace the colour presets through `D12CanvasOptions.ColourPresets` in `AddD12Canvas`, so that my brand colours appear in the bar.
68. As a host developer, I want each preset to carry a `Name` and a `Hex`, so that a screen reader says "Blue", not "#2F80ED".
69. As a host developer, I want the library to throw when a preset's hex isn't `#RRGGBB` or the list is null, so that a mistake fails at startup, not when a user opens a flyout.
70. As a host developer, I want an empty preset list to remove the grid, so that I can switch presets off without a separate flag.
71. As a host developer, I want one preset list for every colour role, including my own colour properties, so that my colours apply everywhere.
72. As a diagram user of an app that sets no presets, I want ten sensible defaults led by Black, Grey and White, with Yellow matching the sticky note and Blue matching the accent, so that common colours are one click away.

### PropertyPanel

73. As a diagram user, I want a glyphed Dropdown in the panel drawn as tiles with the glyph above the name, so that I see every option at once.
74. As a diagram user, I want the current option's tile pressed, so that I can read the value.
75. As a diagram user, I want tiles to wrap into a grid with columns as wide as the longest name, so that no name gets cut short.
76. As a diagram user, I want a mixed value to press no tile and tag the label with an italic "Mixed", so that I know the selection disagrees.
77. As a diagram user, I want picking a tile on a mixed selection to set every target to that value, so that the result is predictable.
78. As a diagram user with a fully locked selection, I want the tiles disabled but still showing the pressed value, so that I can read the value without editing it.
79. As a keyboard user, I want Tab to land once on the tile group, on the checked tile or the first tile when mixed, so that the group is one tab stop.
80. As a keyboard user, I want arrow keys to move focus between tiles and wrap, including onto a second row, without committing, so that browsing options doesn't fill my undo history.
81. As a keyboard user, I want Space or Enter to commit the focused tile as one edit, so that one choice is one undo entry.
82. As a screen-reader user, I want the tiles announced as a radio group named by the field's label, each tile a radio with its name, so that I know the choice and the current value.
83. As a diagram user with a partly locked selection, I want a line above the fields with the lock glyph and "1 of 3 locked; changes skip them", so that I know why an edit skips some shapes.
84. As a diagram user with a fully locked selection, I want today's Unlock button and no lock-count line, so that the panel shows one thing at a time.
85. As a screen-reader user, I want the partial-lock line read as the first text in the panel, with no live region, so that it doesn't interrupt me on every selection change.
86. As a diagram user, I want the Image editor's "Choose file…" and "Remove image" buttons to show the same glyphs as the menu's image rows, so that one action has one picture.

### Context menu

87. As a diagram user, I want the align and distribute strip drawn with the shared glyphs, so that it matches the bar and the Palette.
88. As a diagram user, I want every menu row to lead with a glyph, so that I find Cut, Group or Zoom to Fit faster.
89. As a diagram user, I want row glyphs to follow the row's text colour, including on hover, so that the menu stays calm.
90. As a diagram user, I want toggle rows such as Snap to Grid to keep their own glyph and show a check glyph in a separate column when on, so that state and action don't compete.
91. As a screen-reader user, I want menu glyphs to be decorative and the label to stay the accessible name, so that I hear "Cut", not a picture.
92. As a diagram user, I want Lock and Unlock to look the same in the menu, the bar and the panel, so that I learn each glyph once.

### Placeholders

93. As a diagram user zoomed far out, I want each LOD placeholder to show its type's glyph, so that I can still tell shapes apart.
94. As a diagram user, I want an empty or broken image to show the Image glyph above its text, so that the placeholder matches the Palette entry.

## Implementation Decisions

### Glyph model and rendering

- A **Glyph** is an immutable record holding a list of `GlyphPart(string Data, GlyphPaint Paint)`. Every part is SVG path data, with no circle, rect or line elements. `GlyphPaint` is `Stroke` (the default), `Fill`, `AccentStroke` or `AccentFill`. **Glyph** and **Glyph set** are already in the glossary. "Icon" stays the word for the slot a glyph fills.
- `Glyphs` is a public static catalogue, maintained by hand with no generator. Glyph names are public API from now on, so renaming or removing one is a breaking change.
- `D12Icon` is a public component. It takes `Glyph`, an optional `Label`, a `Class` and splatted attributes, with no `Size` parameter. It renders one `<svg viewBox="0 0 16 16" class="d12-icon" fill="none" stroke="currentColor">` with round caps and joins.
- Size defaults to 16px. Any ancestor can override it with `--d12-icon-size`. Stroke defaults to 1.5 and can be overridden with `--d12-icon-stroke`.
- Colour follows the host's `color`, which already comes from the `--d12-*` tokens. Light and dark need no new tokens, and nothing trips the colour-literal guard test. Accent parts render with class `d12-icon-accent` and take `--d12-icon-accent`, falling back to `currentColor`.
- Without a `Label` the svg is `aria-hidden="true" focusable="false"`. With a `Label` it is `role="img" aria-label="..."`. There is no `<title>`. The XML doc says to name the control and leave its icon decorative.
- Option states use one glyph per state, picked by the caller from the value. No glyph redraws itself from a value.

### Glyph list and drawing rules

The spec carries no path data. The drawing slice draws every glyph, starting from the rough paths on the throwaway branches `prototype/palette-look` (`ProtoGlyphs`) and `prototype/property-bar-look` (`ProtoBarData`), which already use the part shape. The names below are working names. Match them to the existing enum members where one exists.

Drawing rules:

- 16 grid, stroke 1.5, round caps and joins.
- At least 1 unit of padding: every coordinate sits in the 1 to 15 box.
- Place coordinates so a 1.5 stroke lands on whole pixels at 16px.
- Text vertical align must not reuse the object-align glyphs. The bar's "align text to top" and the menu's "align shapes to top" look different.

| Glyph | Shows | Sites | Paint |
|---|---|---|---|
| Rectangle | a rectangle | Palette, LOD placeholder | stroke |
| StickyNote | a note with a folded corner | Palette, LOD placeholder | stroke |
| Text | a letter or text mark | Palette, LOD placeholder | stroke |
| Image | a picture frame with a mountain and sun | Palette, LOD placeholder, image placeholder | stroke |
| Edge | a diagonal arrow with a dot at its start | Palette | stroke, fill for the dot |
| Component | a plain rounded square with a small corner mark | Palette and LOD placeholder when `Icon` is null | stroke |
| Fill | a shape whose inside carries the value | Property bar | stroke + accent fill |
| Stroke | a shape whose outline carries the value | Property bar | accent stroke |
| TextColour | a letter over a swatch bar | Property bar | stroke + accent fill |
| EdgeColour | a line whose stroke carries the value | Property bar | accent stroke |
| StrokeWidth | lines of growing weight | Property bar | stroke, fill |
| FontSize | a large and small letter | Property bar | stroke |
| WeightNormal | a plain letter | Property bar option, panel tile | stroke |
| WeightBold | a heavy letter | Property bar option, panel tile | stroke, fill |
| TextAlignLeft, TextAlignCentre, TextAlignRight | text lines aligned in the box | Property bar option, panel tile | stroke |
| TextVerticalTop, TextVerticalMiddle, TextVerticalBottom | text lines placed vertically in a box | Property bar option, panel tile | stroke |
| RoutingStraight, RoutingOrthogonal, RoutingCurved | an edge path of each kind | Property bar option | stroke |
| SourceArrowNone, SourceArrowArrow | an edge start, bare or with a head | Property bar option | stroke |
| TargetArrowNone, TargetArrowArrow | an edge end, bare or with a head | Property bar option | stroke |
| Caret | a small down caret | Property bar chips | stroke |
| AlignLeft, AlignCentre, AlignRight, AlignTop, AlignMiddle, AlignBottom | shapes against a guide line | context menu strip | stroke, fill |
| DistributeHorizontally, DistributeVertically | three shapes evenly spaced | context menu strip | stroke, fill |
| Check | a check mark | context menu toggle column | stroke |
| Cut, Copy, Paste, Duplicate, Delete | the usual edit actions | context menu rows | stroke |
| SelectAll | a dashed box around shapes | context menu row | stroke |
| Group, Ungroup | shapes inside and outside a group frame | context menu rows | stroke |
| BringToFront, BringForward, SendBackward, SendToBack | stacked shapes with one moving | context menu rows | stroke, fill |
| Lock, Unlock | a closed and an open padlock | context menu rows (Unlock All reuses Unlock), Property bar lock count and Unlock button, panel partial-lock line | stroke |
| ZoomToSelection, ZoomToFit, ZoomTo100 | a magnifier or frame for each zoom | context menu rows | stroke |
| SnapToGrid, ObjectSnapping | a grid with a point, two shapes with a guide | context menu toggle rows | stroke |
| AddPort, RemovePort | a port dot with plus or minus | context menu rows ("Add port here" and "Add port…" share AddPort) | stroke, fill |
| ChooseImage, RemoveImage | an image with an add or remove mark | context menu rows, Image editor buttons | stroke |

That is 59 glyphs: 6 component, 25 Property bar, 8 strip, 24 menu-row and editor. The panel tiles reuse the bar's option glyphs.

### Consumer icon contract

- `ComponentRegistrationBuilder.Icon` and `ComponentRegistration.Icon` change from `string?` to `Glyph?`. There is no string or emoji fallback. This is a breaking change, accepted because the library is not published. The built-ins move to `Glyphs.Rectangle`, `Glyphs.StickyNote`, `Glyphs.Text` and `Glyphs.Image`. The Demo's two custom types and the options tests move to glyphs too.
- A null `Icon` draws `Glyphs.Component` on the Palette and on the LOD placeholder. Neither site branches on a missing icon.
- `EditableProperty` gets an optional `OptionGlyphs` (`IReadOnlyDictionary<string, Glyph>?`), keyed by option value. It is set only through the builder's `EditableProperties` override, because an attribute can't carry a `Glyph`. There is no new `EditorKind`.
- A Dropdown whose every option has an entry in `OptionGlyphs` draws tiles in the `PropertyPanel` and a chip with a flyout in the `Property bar`. Without glyphs it stays a `<select>` in the panel (Image `Fit` stays one).
- The built-in Text align, Vertical align, Routing and arrowhead properties declare `OptionGlyphs` through the same route. The bar reads glyphs from the property that declares the role (the representative in its row list), and the edge rows carry `OptionGlyphs` on their `EdgeRole` entries the same way. The library keeps no glyph table per role. In the bar, a value with no glyph shows its option text.

### Palette

- One column of 36px square buttons with 18px icons, on the Palette's raised surface. The host still places it, and the App keeps its top-left overlay.
- Order: Edge, a divider, then registered types with built-ins before host types. This reverses today's order, where `AddD12Canvas` appends the built-ins after the host's types. A divider separates categories. Uncategorised types stay in the `Uncategorized` group.
- The rail shows the first 8 registered types, a fixed number with no parameter. Edge doesn't count. Past 8, a divider and a "…" button sit at the bottom. With 8 or fewer, the button isn't drawn.
- The "…" flyout opens to the right of the rail, bottom-aligned with the button. It holds only the types not on the rail, one column per category, each with a small uppercase heading and glyph-and-name rows. Picking a row adds the type and closes the flyout. Esc closes it. Rows drag like rail buttons.
- Each button's accessible name is the registration's `AccessibleName`, and its glyph is decorative. The visible name is a library tooltip with `DisplayName`, 12px to the right of the rail, centred on the button. It shows on hover and keyboard focus and never uses the native `title`. The "…" tooltip gives the count ("More components (14)"); its accessible name is "More components" with `aria-expanded`.
- The Edge entry is named "Edge" everywhere, which replaces "Connector", and draws `Glyphs.Edge`.
- The rail is a vertical `toolbar` named "Component palette" and follows the WAI-ARIA toolbar keyboard pattern. The flyout is a `menu`.

### Library tooltip and flyout

- One library tooltip serves the Palette rail, the bar's number fields, the bar's lock count and the colour swatches. It shows on hover and keyboard focus and never uses the native `title`. The caller sets its side: right of the Palette rail, above the Property bar.
- One library flyout serves the Palette overflow and the bar's chips. Opening it moves focus into it. Esc or an outside click closes it and returns focus to the button that opened it. Picking an entry acts and closes.

### Property bar

- Each colour and choice role is a chip button: glyph, value text, caret. Colour chips paint the value into the glyph's accent and show the hex in upper case. The chip shows the hex even when the value matches a preset. Choice chips show the current option's glyph and name. "Theme" and "Mixed" use the muted token. The chip's accessible name is "Label: value". It is a menu button with `aria-haspopup="menu"` and `aria-expanded`. Chips need no tooltip.
- The bar sets `--d12-icon-accent` inline per cell to the selection's colour. Its own CSS restyles `.d12-icon-accent` as a hatch for mixed and a dashed muted outline for theme colour.
- Choice flyout: the role's label as a small uppercase heading, then one `menuitemradio` row per option, glyph and name, with the current one checked. A mixed value checks none. An option with no glyph keeps an empty glyph column.
- Colour flyout: the preset grid, five per row in list order, wrapping. Then "Theme colour" when every target can hold `null`. Then "Custom…", which opens the native picker. Swatches are `menuitemradio`, named by the preset's `Name`, with a tooltip of name and hex. A swatch is checked when the current value matches its hex, ignoring case. Mixed checks none. "Theme colour" and "Custom…" have no glyph.
- Number roles stay a glyph beside a 40px field, no flyout. A mixed number shows the placeholder "–", and its accessible name still says Mixed. Number fields use the library tooltip.
- A partly locked selection ends the bar with a divider, `Glyphs.Lock` and a count such as "1/3". It isn't a button. Its tooltip is "1 of 3 locked; changes skip them".
- A fully locked selection no longer hides the bar. The bar stands where it would and holds only an Unlock button with `Glyphs.Unlock` and the word. It unlocks the selection in one history entry, as the panel's Unlock does.
- Chips make the bar wider (about 440px for a sticky note against about 190px today). The existing clamp rule stands.

### Colour presets option

- `D12CanvasOptions.ColourPresets` holds a list of `ColourPreset(string Name, string Hex)`, set in `AddD12Canvas`. Both fields are required. `Hex` must be `#RRGGBB`, checked when the list is configured, throwing as a registration with no `DisplayName` does.
- One list for every colour role: Fill, Stroke, Text colour, Edge colour and any host colour property. No per-role override.
- The default list, in order: Black `#000000`, Grey `#868E96`, White `#FFFFFF`, Red `#E03131`, Orange `#F08C00`, Yellow `#FFEB3B`, Green `#2F9E44`, Blue `#2F80ED`, Purple `#7048E8`, Pink `#D6336C`. Presets are fixed hex values and don't change with the theme.
- An empty list removes the grid. Setting the property to null throws.
- The presets appear in the `Property bar` only. The `PropertyPanel` keeps its native picker.

### PropertyPanel

- A glyphed Dropdown draws tiles: glyph above the name in 10px text, in a grid under the label that wraps. The grid's minimum column width comes from the longest option name. A name wider than half the panel ends in an ellipsis as a last resort. No tooltip, and no fallback to a chip or flyout at any option count. The current option's tile is pressed.
- Mixed: no tile pressed, the label carries an italic "Mixed" tag. Picking a tile sets every target.
- Fully locked: today's Unlock above the fields, every tile disabled, the pressed tile still drawn as pressed. Disabled tiles leave the tab order.
- The tiles are a `radiogroup` labelled by the field's label through `aria-labelledby`. Each tile is a `radio` with `aria-checked`, named by its visible name. Tab lands once on the group, on the checked tile or the first tile when mixed (roving `tabindex`). Arrow keys move focus and wrap but don't commit. Space or Enter commits one edit and one history entry. This departs from the ARIA radio pattern on purpose, to keep history clean.
- Partly locked: `Glyphs.Lock` and "1 of 3 locked; changes skip them" in the muted token, above the fields in the Unlock button's slot. Plain text, no action, no tooltip, no live region, no `aria-describedby` on fields. It shows only when some targets are locked and some aren't.
- The Image editor's "Choose file…" and "Remove image" buttons show `Glyphs.ChooseImage` and `Glyphs.RemoveImage` before their text, decorative.

### Context menu

- The 8 align/distribute glyphs move onto `Glyphs` and render through `D12Icon`. The hand-built glyph markup goes. The strip buttons keep their `aria-label`.
- Every row gets a leading decorative glyph. A row reads: check column, glyph, label, hint. Glyphs draw in `currentColor`, including on hover. Delete gets no danger colour.
- `✓` becomes `Glyphs.Check` in its own column. The column appears only when the menu holds a toggle row, as today. A toggle row keeps its own glyph. `aria-checked` still carries the state.
- The menu stays internal: rows have no public route, so none of this reaches consumers.

### Placeholders

- The LOD placeholder draws the registration's glyph, or `Glyphs.Component` when it is null.
- The image placeholder draws `Glyphs.Image` above "No image" or "Image unavailable". There is no broken-image variant.

### Slice order

1. **Drawing.** Add `Glyph`, `GlyphPart`, `GlyphPaint`, `Glyphs` and `D12Icon`. Draw all 59 glyphs. Build the Demo glyph gallery with its baselines and the padding-box test. Publish a gallery screenshot and get the user's sign-off before committing. No production site uses the glyphs yet, so no existing baseline moves.
2. **Site slices**, each switching its site to the final glyphs and moving its own baselines once in its commit: Palette (with the `Icon` type change and the null fallback), Property bar (with `OptionGlyphs`, chips, flyouts, colour presets and lock states), PropertyPanel (tiles, partial-lock line, Image editor buttons), context menu, and the LOD and image placeholders. A slice may combine sites where the `Icon` type change forces it.

## Testing Decisions

A good test asserts what a host or a user sees: rendered markup, roles, names, state, what a click or key does to the board and history. It doesn't assert private fields, CSS class internals beyond the public `d12-icon` and `d12-icon-accent` hooks, or how a component stores its open state.

Four seams, all existing except the gallery page:

1. **bUnit through the public components** a host renders: `Palette`, `PropertyBar`, `PropertyPanel`, and the context menu through `DiagramCanvas`, with services set up through `AddD12Canvas` and `D12CanvasOptions`. Prior art: the existing `PropertyPanel`, `PropertyBar`, `Palette`, context-menu and `D12CanvasOptionsTests` classes in `D12Canvas.Tests`. bUnit covers:
   - tiles carry `radiogroup` and `radio` with `aria-checked`; mixed checks none and tags the label; a locked selection disables them
   - Space or Enter on a tile commits one edit; arrow keys move the roving `tabindex` and don't commit
   - `OptionGlyphs` on a host-registered property gives tiles in the panel and a chip with a flyout in the bar; without it the property stays a plain Dropdown
   - a null `Icon` draws `Glyphs.Component`, and a host type's glyph shows on the rail and in the flyout
   - the rail order (Edge, built-ins, host types), the 8-type limit and when the "…" button appears
   - chip accessible names, "–" for a mixed number with Mixed in its accessible name, the checked option and swatch, and none checked when mixed
   - the panel's partial-lock line shows only for a partial lock; the bar's lock count follows the same rule; a fully locked selection gives the lone Unlock button, which unlocks in one history entry
   - `ColourPresets` throws on a bad `#RRGGBB` or a null list, an empty list removes the grid, each swatch is named by its `Name`
   - a checked context-menu row draws `Glyphs.Check` in its own column; every row draws a glyph
   - `D12Icon` is `aria-hidden` without a `Label` and `role="img"` with one
2. **A unit test over `Glyphs`**: every coordinate lies in the 1 to 15 box, and every public member appears in the gallery. Whole-pixel stroke placement is left to the user's sign-off.
3. **Playwright screenshots** of Demo pages, in the pinned container with `-parallel none`, per the commit-gating rule. Nine new cases, each in light and dark (18 baselines):
   1. Palette "…" flyout open, columns by category
   2. Property bar option flyout open
   3. Property bar colour flyout open, preset grid
   4. Property bar partly locked, lock count at the end
   5. Property bar fully locked, the lone Unlock button
   6. PropertyPanel tiles, single value
   7. PropertyPanel tiles, mixed value
   8. PropertyPanel partly locked line
   9. Demo glyph gallery

   Every existing baseline at the changed sites moves with its slice: Palette, Property bar, PropertyPanel, context menu, LOD placeholder, image routes, the locked-shape case and the connector palette entry. An existing case without a dark twin gets one only when its slice changes that site anyway, which adds dark twins for the hatched-fill bar and the Image editor buttons. The plain Dropdown baseline moves to a host property with no `OptionGlyphs`; if the Demo has none, add one to its custom component type. The gallery page lives in `D12Canvas.Demo`, isn't linked from the App and isn't a throwaway prototype route.
4. **Playwright interaction probes** (derived from `InteractionProbe`, prior art in the existing probe classes), for what needs a real browser:
   - a bar chip opens its flyout and moves focus into it; Esc and an outside click close it and return focus to the chip; choosing an option commits and closes
   - the Palette "…" flyout follows the same rules, and an entry in it still places on the board
   - arrow keys move real focus across the tiles, including onto a wrapped second row
   - a rail button has an accessible name and shows its tooltip on hover and on focus (hover states flake in screenshots, so the tooltip has no baseline)

## Out of Scope

- The App's icons and Bootstrap: the App keeps its own icon paths.
- Colour tokens and the colour palette.
- Palette search, filtering, collapsing categories and placement previews.
- A full layout redesign of the `PropertyPanel` (grouped sections, W/H pairs, a custom colour field). The panel keeps its native colour picker.
- Edge fields in the `PropertyPanel`, so routing and arrowheads as panel controls. The panel edits component instances only.
- A shape stock for hosts (Ellipse, Diamond, Hexagon and the other Palette prototype stand-ins). Hosts supply their own `Glyph`.
- A broken-image glyph, and glyphs on the colour flyout's "Theme colour" and "Custom…" rows.
- A per-canvas or per-role colour preset list.
- A public route for context menu rows.
- Third-party icon paths and a third-party notice.

## Further Notes

- The decisions come from the wayfinder map at [map.md](map.md) and its twelve resolved tickets under `issues/`. Each ticket's Answer has the reasoning.
- The Lucide research in `research/` was reversed: the library draws every glyph itself. The file stays as a record.
- The three prototypes live on throwaway branches `prototype/palette-look`, `prototype/property-bar-look` and `prototype/panel-icon-controls`. Don't merge them. The drawing slice may copy their rough paths as a starting point.
- The Palette and the bar's chips make the chrome wider than today. Check the bar's clamp inside narrow containers while doing the bar slice, but no new clamp rule is decided here.
- `CONTEXT.md` already has **Glyph** and **Glyph set**. Update the **Palette** and **Property bar** entries if their descriptions mention the old layouts, and rename any "Connector" palette wording to Edge.
