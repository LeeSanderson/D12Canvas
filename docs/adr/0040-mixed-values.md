# A mixed row stays editable, says it is mixed by one rule per editor kind, and writes to every target

When the targets of a property row disagree, the row is **mixed**. It shows that it is mixed instead of showing whichever target came first, and committing it writes the new value to every target. This replaces ADR 0008's first-target rule, which the property panel states in its own comment ("there's no 'mixed values' indicator").

The first-target rule was tolerable in a docked text field and is not tolerable in the property bar. There, ADR 0021 paints a colour role's value into its glyph, so three differently coloured sticky notes showed one of the three as though it were the truth, at 26 pixels, with no field to read and disbelieve. ADR 0021 also made mixed rows ordinary rather than rare: reading the expanded selection makes a `Group` editable through its members, and grouping three notes of different colours is something users do on purpose.

## Committing writes to every target

A mixed row is editable. A commit writes the chosen value to every target in one history entry and changes only the edited property. The panel's `Commit` already does this, including skipping any target that already holds the new value, so a commit to a partly mixed row is a smaller entry than the selection suggests.

A row that stays read-only until the user resolves it was rejected. Resolving has to choose a value and write it to every target, which is this rule with an extra step in front of it, and it blocks the ordinary case of recolouring a group.

The reference tools agree. tldraw writes a style to every selected shape that declares it, and any click on a mixed picker writes. Excalidraw writes to every element that can take the property. FigJam replaces every selected object's colour. None of them has a read-only mixed state. (Branch `research/mixed-value-presentation`, `.scratch/canvas-interaction-quality/research/mixed-value-presentation.md`.)

## Mixed is decided per row, and `null` is a value

A row is mixed when its targets do not all hold an equal value. The row is the unit that displays and commits, so it is also the unit of the test. A row only ever contains targets that have the property, through the same-type schema or the role intersection, so "some selected items cannot take this" never makes a row mixed. It removes the row instead, as ADR 0021 already does.

**`null` is its own value.** Since ADR 0034, `null` on a colour means *no author opinion* and resolves from a theme token, and ADR 0016 gives an edge's colour the same meaning. A rectangle with `FillColor = null` and one with `"#FFFFFF"` look the same on the light theme and different on the dark one. A row that showed `#FFFFFF` for both would be believed until the user switched themes. `Commit`'s skip already treats `null` as different from every literal, and Excalidraw counts unset as "no common value", so this matches both the code and the nearest reference.

**Colour values compare case-insensitively**, in both the mixed test and `Commit`'s skip. The built-in defaults are uppercase (`"#FFEB3B"`), and `<input type="color">` always reports lowercase, so a note recoloured to the same yellow read as different from a fresh one. Stored data is not rewritten, for ADR 0034's reason: the library does not second-guess author values. Shorthand hex such as `#fff` is not normalised; it can only arrive from hand-written data, and parsing colours is more than this needs.

## One display rule per editor kind, on both surfaces

ADR 0021 let the bar and the panel differ on membership. They do not differ on how a mixed value looks, because the same selection on two surfaces side by side must not tell two stories. The only difference is the one ADR 0021 already set: a colour is a glyph in the bar and a swatch control in the panel.

| `EditorKind` | Mixed shows as |
|---|---|
| `Color` | A hatched swatch. In the bar, the role keeps its own glyph shape so `Fill` stays distinguishable from `Stroke`, drawn in a fixed hatch in a muted token instead of any selected colour, with the accessible name "Fill: mixed". In the panel, a swatch button with the same hatch opens the native picker, because `<input type="color">` cannot show the absence of a value. |
| `Number`, `Text` | An empty field with the placeholder "Mixed". Focusing and leaving without typing fires no `change`, so nothing is written by accident. A typed value is absolute. |
| `Dropdown` | No option active. The panel's `<select>` shows a disabled "Mixed" option as selected. |
| `Checkbox` | The native indeterminate state. `indeterminate` is a DOM property with no attribute, so it is set through the library's existing JS module. No role is a checkbox, so this reaches the panel only. |

**The inked, themed and mixed states of a colour glyph must be distinct from each other in both themes.** ADR 0034 requires a themed state for a `null` colour and does not draw it. This is a constraint on that design, not a design of it.

Showing the actual colours was rejected. FigJam's picker shows at most two of the selected colours. A 26-pixel glyph split three ways is unreadable, two segments claim there are only two colours when there may be five, and a split would need a themed segment inside it for any `null` target. Excalidraw's slashed swatch was rejected because it means both "mixed" and "transparent" there, and this library already needs a separate themed state, so a shared glyph would bring back the confusion the `null` rule above exists to prevent.

## `Custom` editors learn it from the context

`CustomEditorContext` gains a trailing `bool IsMixed = false`. When it is true, `Value` is `null`. `Commit` is unchanged and writes to every target.

An author who never reads `IsMixed` sees `null`, which their editor already has to handle, so a mixed row looks empty rather than looking like an arbitrary first value. That is wrong in the safer direction. `null` now also means ADR 0034's themed value, and `IsMixed` is what separates the two for an author who needs to.

Passing every value as a list was rejected: every author would have to collapse it into one display, and most would take the first item, which is the defect being fixed. Hiding a mixed `Custom` row was rejected because it removes bulk-setting, which the commit rule above makes the ordinary case.

## The row carries the flag

ADR 0021's row, an id, a role, an `EditorKind`, options, a current value and a commit callback, gains an `IsMixed` flag that its producer computes. Both producers compute it by the rule above. The edge producer needs it now that ADR 0037 lets several edges be selected together, and an edge's colour follows the same `null` and case rules.

## Not decided here

Figma's relative arithmetic, where typing `Mixed+100` adds 100 to each layer's own value, is a feature rather than a way of showing a value, and is left to the fog.

## Amends ADR 0008

The panel's first-target rule is replaced by this ADR. The addendum's `CustomEditorContext` gains `IsMixed`.

## Amends ADR 0021

A colour role's glyph gains a mixed state alongside the inked and themed ones. The row gains `IsMixed`. The section "What this does not solve is mixed values" is answered here.

## Considered and rejected

- **First-target display (status quo).** Presents an arbitrary target's value as the truth, and in the bar the glyph is that value.
- **Read-only until resolved.** The resolve step is a write to every target with an extra click in front of it.
- **Ignoring `null` when deciding mixed.** Shows a literal for a row that changes appearance under a theme switch.
- **Lowercasing colours on write.** Leaves older data and hand-written JSON in either case, so the comparison still has to ignore case, and it rewrites author data.
- **Lowercasing only the built-in default literals.** Fixes the built-ins and nothing an author or a loaded board supplies.
- **Showing the actual colours in a split glyph.** Unreadable at 26 pixels past two segments, claims a count it cannot show, and needs a themed segment inside the mixed state.
- **Excalidraw's slashed swatch.** One glyph for "mixed" and "transparent", where this library already needs a distinct themed state.
- **Letting the bar and panel display mixed differently.** Two stories about the same selection on screen together.
- **`Values` list on `CustomEditorContext`.** Every author collapses it, and most collapse it to the first value.
- **Suppressing a mixed `Custom` row.** Removes bulk-setting for exactly the properties that have no built-in control.
