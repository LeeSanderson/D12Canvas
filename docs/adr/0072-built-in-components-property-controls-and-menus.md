# Built-in editors, property controls and chrome menus absorb browser behaviour by fixed rules

The built-in `StickyNote` and `Text` edit in a native editor. The `Property bar` and `PropertyPanel` render native controls, and ADR 0040 decides how each shows a `Mixed` row. The context menu, the library flyout, `Minimap` and `PropertyBar` hand focus around. In each place the browser has timing, focus or picker behaviour that the markup does not show, and the code works around it.

**The built-in editors focus only once the editor has rendered and end an edit once. A colour swatch holds the last colour the picker reported and commits it when focus leaves. A mixed row rebuilds its element on every edit. The menu and the flyout register only when connected, swallow a dismissing press once, and hand focus back only when they took it.**

## The editor focuses on the render that has it

`BeginEdit` sets a pending flag, and `OnAfterRenderAsync` focuses the editor only once `_editorRef` is set. `BeginEdit` can arrive while the after-render of an earlier render in the same batch is still to run, before the editor has rendered. Focusing on that after-render would find no editor. `StickyNote` and `Text` both wait.

## An edit ends once

`EndEdit` returns at once when `_isEditing` is false. `Escape` removes the editor from the DOM, and the browser may then fire a blur for it. That blur must not end the same edit a second time. Both built-ins carry the guard. An `IInlineEditable` author who writes their own editor meets the same two timing cases and needs the same pending flag and end guard.

## The note shows the text where the editor had it

`StickyNote` renders a trailing newline doubled. A `pre-wrap` paragraph drops the empty line after a final newline that a textarea shows, so without the extra newline the text jumps up a line when the edit ends.

Vertical alignment uses `safe center` and `safe flex-end`. Safe alignment keeps the first line of a text taller than the note at the note's top edge instead of pushing it above the note, out of sight.

The editor sizes its own height to its text. Firefox has no `field-sizing: content`, so `growWithContent` in `inlineEditor.js` sets the editor's height to its scroll height on every input. That keeps a middle- or bottom-aligned note's text where it was while the user types. Blazor owns the editor's `style` attribute and drops the height whenever it rewrites it, so the script refits after a rewrite too.

Removing the doubled newline or the `safe` keyword changes what a finished note shows, and the visual baselines catch it.

## Option sets are curated

The built-in props offer curated CSS values, not the full CSS vocabulary. `ImageProps.Fit` offers the most useful `object-fit` values. `TextProps.FontWeight` and `TextProps.TextAlign` offer every value already used across the demo app and the test suite. Options are declarative metadata on `[PanelEditable]`, not a CLR enum, so a curated list costs nothing to extend.

`StickyNoteProps` defaults its alignments to centred in the record as well as in the registration. The registration default covers a new note. The record default covers a board saved before the alignment properties existed, which then loads centred.

## A swatch with no single colour holds an unlikely one

A colour input on a mixed panel row, and a bar swatch with no single colour, hold `#010203` instead of black (`PropertyPanel.MixedSwatchValue`, `PropertyBar.UnsetSwatchValue`). The native picker opens on the input's value, and confirming that same value fires no `change`. A mixed swatch hides its value under the hatch. If it held `#000000`, picking black on a mixed row would fire nothing, and black could never be applied. Nobody is likely to pick `#010203`.

## The last pick commits when the swatch is left

`PropertyBar` and `PropertyPanel` each hold the last colour the picker reported through `input` (`HoldPick`), commit it on `change` (`CommitPick`), and also commit the held pick when the swatch loses focus (`CommitHeldPick`). A `change` drops the hold.

The browser fires no `change` when a pick ends on the colour the input already held. On a themed bar row (a `null` colour painted from a board token), the input opens on the colour the swatch already shows. Without the blur commit, picking that exact colour writes nothing and the row stays themed. With it, the themed row becomes an explicit colour, which is what the user picked.

On a mixed panel row, a user who settles on the colour the swatch opened with expects every target to take it. With `change` alone the targets stay mixed. The commit on leave follows ADR 0040's `Mixed` rules: one history entry, and targets that already hold the colour, compared ignoring case, are skipped. A test inputs another colour, inputs the original back, blurs, and checks both targets hold the original.

## A mixed row is rebuilt after every edit

A mixed row's element key includes the edit count, so every edit rebuilds it. A row still mixed after an edit renders the same empty value as before, and Blazor would keep whatever was typed into the reused element.

In the bar, a choice chip holds nothing typed and keeps its plain id. A pick from a mixed row leaves the chip in place for the closing flyout to hand focus back to. In the panel, tile rows also keep their plain id.

## Checkbox values arrive as booleans

`PropertyPanel.ConvertValue` takes a checkbox's value as a `bool` and every other control's as a string. Blazor reads a checkbox's `.checked` DOM property directly, so its `ChangeEventArgs.Value` is a `bool`.

## Tiles are one tab stop

A dropdown drawn as tiles is one tab stop with a roving `tabindex`. The stop is the focused tile while focus is inside the group, otherwise the checked tile, or the first tile when the value is mixed. Arrow keys only move focus. `Space`, `Enter` or a click commits, so browsing the options adds no history entry. The panel's JavaScript prevents the default for these keys, so the page does not also scroll and a button does not also click.

## Custom opens the native picker inside the click

CONTEXT.md says "Custom…" opens the native picker of the hidden colour input in its cell. The bar's JavaScript opens it while the click is still a user gesture, because the browser opens a picker only under user activation. .NET then closes the flyout.

## Presets are #RRGGBB and registered once

A `ColourPreset`'s `Hex` is `#RRGGBB`, the only form the native picker behind "Custom…" accepts. `ColourPresetCatalog` throws when `D12CanvasOptions.ColourPresets` is set to a null list or to a preset whose `Hex` has another form.

`AddD12Canvas` registers one `ColourPresetCatalog` instance. A second `AddD12Canvas` call finds that instance in the service collection and configures it, so it changes the list the first call registered.

## Menu and flyout registration tolerates a closed element

`registerMenu` in `ContextMenu.razor.js` and `registerFlyout` in `flyout.js` return an empty disposable when the element is no longer connected. The menu can close before its first render's registration runs, and the module then finds no element to register.

## Focus when the menu opens

A menu opened from a pointer focuses the menu element itself, not its first row. No row looks chosen before a key is pressed, and the first arrow lands on the first or last row. ADR 0067 covers the keyboard-opened menu, which focuses its first row.

## The press that dismisses the menu is swallowed once

ADR 0023 consumes the press that closes the menu. That press still fires its `click` on release, or its `contextmenu` on the secondary button, and the module swallows each of these once. The next press, the next key, or the press being cancelled clears whichever of the two never came. Without that clearing, a primary dismissal could go on to eat the menu a later `ContextMenu` key opens, or a keyboard click on a host button.

## The flyout keeps its keys

Every key pressed inside the flyout stops there, so neither the toolbar that holds the opener nor the canvas's shortcut listener on `window` also acts on it. An item's activation by `Enter` or `Space` is the button's default action, which stopping propagation leaves alone. A press on the opener is left to the opener, whose click closes the flyout itself.

## Focus returns after the closing press ends

When an outside press closes the flyout, the flyout judges focus on that press's `pointerup`, not at dispose. The press can still move focus after .NET has closed the flyout: its `mousedown` blurs to the body or focuses what it landed on. Judging once the press ends lets focus land where the press left it. Focus goes back to the opener only when the flyout took it along, which means focus is on the body, the document element or inside the flyout. A press that moved focus to another control keeps it there.

## The flyout flips only when that clips less

The owner's placement opens the flyout on one side of its opener, and the owner's `[data-d12-flyout-flipped]` rule opens it on the other. `flyout.js` sets the attribute only when the flyout's own side runs past the box that clips it and the flipped side runs past that box less. The clipping box is the viewport narrowed by every ancestor that clips its overflow. Each owner says what flipped means in its own CSS.

## Grid and column navigation

An element with `data-d12-flyout-columns` holds a grid of items, and `Up` and `Down` cross it a whole row at a time. Leaving the grid's top or bottom row steps to the item before or after the grid.

A column of rows is an element with `role="group"`. Across columns, `Left` and `Right` move to the same row of the next or previous column, or to that column's last row when it is shorter. A flyout without columns moves to the next or previous item instead.

## The minimap lets go before the canvas hears the end

`Minimap.EndPress` clears its held press before it tells the canvas the press ended. A click's framing flight then reframes the map instead of finding it still held.

A listener that goes away ends its press with no release. The minimap then tells the canvas to cancel, instead of leaving the board held for a release that will never arrive.

## Unlock hands focus to the first chip from the keyboard only

Unlock in the `Property bar` replaces itself with the chips, so focus would fall to the page. When Unlock was pressed from the keyboard, focus lands on the first chip instead. A click whose `Detail` is zero came from the keyboard. A pointer press leaves focus where it is, since focus inside the bar keeps the selection chrome hidden.

## Consequences

A new colour control that wraps `<input type="color">` holds the last pick and commits it on leave, and holds `#010203` when it has no single value.

A new flyout owner writes its own `[data-d12-flyout-flipped]` rule.
