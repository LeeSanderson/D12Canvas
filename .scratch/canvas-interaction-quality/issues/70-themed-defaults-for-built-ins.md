# 70 — Themed defaults for built-in components

**What to build:** A text or rectangle colour the end user never chose follows the theme (ADR 0034). `TextProps.Color`, `RectangleProps.FillColor` and `RectangleProps.StrokeColor` become nullable and default to null, meaning no author opinion; each built-in resolves null from a board token (`--d12-board-text`, `--d12-board-fill`, `--d12-board-stroke`) in its own CSS, emitting an override custom property only when the value is non-null. Light token values are byte-identical to the old literals, so light boards look the same. The panel's colour editor gains a clear control that writes null, which is the way back to the themed default. Sticky Note's yellow and black stay as literals. Existing boards keep whatever literals they hold; there is no migration. A component author's own component can fall back to the same public board tokens, since nothing in the registration path changes.

**Blocked by:** None — can start immediately

**Status:** resolved

- [x] The three props are nullable with null defaults and their registrations' default props carry null
- [x] Each built-in paints the token when the prop is null and the literal when set, in both themes
- [x] The canvas declares the three board tokens with light values equal to the old literals and dark values that read on a dark surface
- [x] The panel's colour editor shows a clear control that commits null; a null value displays as the themed state rather than an empty swatch
- [x] A board saved with literal colours loads and renders those literals unchanged
- [x] A demo page shows default text and rectangles on a dark board for the visual suite; light baselines are byte-identical
- [x] Full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Themed default` term describes what shipped

## Comments

`TextProps.Color`, `RectangleProps.FillColor` and `RectangleProps.StrokeColor` are `string?` and the built-in registrations default them to null. Each component resolves null in its own CSS: `.d12-text` reads `var(--d12-board-text-override, var(--d12-board-text))` and `.d12-rectangle` reads the fill and stroke pair the same way, with the component emitting the override custom property inline only when the prop is non-null. The canvas declares `--d12-board-text`, `--d12-board-fill` and `--d12-board-stroke` in all four of its token blocks with the light values byte-identical to the literals they replace and the dark values from the decision's table. A `TextProps` or `RectangleProps` positional record still constructs from every existing call, so a board saved with literals loads and paints them unchanged; the demo and the app seed boards keep their literals.

The panel's colour row gains the way back: a nullable colour field (detected through the property's nullability annotation) whose value is null renders its swatch in a themed state (dashed outline on the surface, with a "Theme colour" label) and no clear control; one holding a colour shows a "Use theme colour" button that commits null as one history entry. A non-nullable colour property gets neither, and an empty string committed to a nullable field normalises to null so absence has one representation.

A new demo page (`/themed-defaults-demo`) shows the same seeded board under a light and a dark `data-d12-theme` pane: an unauthored rectangle and text that follow each pane's tokens beside an authored text that stays blue on both. `ThemedDefaultsVisualTests` asserts the computed colours and adds one baseline. The page is deliberately not in the demo navigation, since a nav row would move every existing baseline. The README's token table gains the three rows.

The panel detects a nullable colour from the property's own nullability annotation, recorded on `EditableProperty.CanHoldNull` at discovery time where the decision placed it. A cross-type row offers the way back only when every selected type's property can hold null, since writing null into a non-nullable `string` would break that type's contract; a row whose first target is null and whose others hold colours shows the themed state and no clear control, which is the mixed-values gap ticket 108 owns. The themed swatch is styled for both the WebKit and Gecko colour-swatch parts.

Two things found by this ticket's visual runs and corrected here rather than left. The previous commit had put `color-scheme` on the palette and the context menu as well as the panel; it comes off those two here, because neither has a form control and it does nothing for them (the fuller story, including a diagnosis of pixel noise that turned out to be wrong, is in ticket 69's comments). And this ticket's visual test file had been swept into the previous commit along with that run's baselines, one commit before the page it drives, so the previous commit's visual job would have failed on those three cases alone.

Baseline movement from the final run: every `.verified.html` carrying the canvas style block or a Text or Rectangle style block moved, as the new tokens and the token-reading rules are rendered inline; the four panel PNGs moved because a freshly placed Rectangle now has no authored colours and its two colour rows show the themed state with its label where swatches and no clear control showed before; and the themed-defaults page is one new baseline, inspected: black-on-white and light-on-dark for the unauthored shapes, blue on both panes for the authored text. Received PNGs whose only difference was the one-unit corner noise described in ticket 69 were not folded, since the comparer already treats them as equal. The run's console summary was lost when the process streaming it was stopped for system memory pressure part-way through; the container finished on its own and its received files are what was folded.

Not done here: `--d12-inline-edit-outline` and the image placeholder's tokens, which the decision also names, belong to tickets 110 and 98 respectively.
