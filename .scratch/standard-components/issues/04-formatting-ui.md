# Formatting UI

Type: prototype
Status: resolved
Blocked by: 03
Map: [Better standard components](../map.md)

## Question

How does a user apply formatting while editing text inline?

Decide, with a throwaway Demo prototype to react to:

- keyboard shortcuts (Ctrl+B, Ctrl+I, Ctrl+U and any for the block constructs chosen in Markdown subset)
- whether a floating toolbar appears over the selection, or the Property bar switches to formatting controls while editing, or both
- what the controls show for a sticky note or rectangle (bold, italic, underline only) versus Text
- whether markdown shortcuts typed inline (`**`, `- `, `# `) convert as you type, or only paste converts
- how the box-level props (Text's `FontWeight`, `TextAlign`, `FontSize`) relate to span formatting

## Answer

The Property bar becomes the formatting bar while editing. This was variant B of the prototype, which lives on the `prototype/formatting-ui` branch at `/prototype/formatting-ui?variant=B` in the Demo. The other three variants lost: a selection bubble, keyboard and markdown only, and a hybrid of the bar with a bubble.

**Selected, not editing.** The bar shows a whole-text **B** chip ahead of the box props. The chip replaces Text's removed `FontWeight`: it wraps or unwraps the whole text in `**…**`. Ctrl/Cmd+B on a selected box does the same.

**Entering edit.** A double-click starts editing with a collapsed caret at the click point. Today `InlineEditorEntry.FocusAndSelectAllAsync` selects all on every entry, so a double-click then needs another click before the user can type where they meant. Enter on a selected box still selects all, since there's no point to place a caret.

**Editing.** The bar above the box swaps its chips, in this order:

- Inline controls for the component's level. Full has bold, italic, underline, strikethrough, inline code and link. Basic has bold, italic and underline only.
- Full only: block controls for paragraph, H1, H2, H3, bulleted list, numbered list, quote, code block, horizontal rule and table.
- The box props, which stay whole-box: font size (with Auto on a sticky note), alignment and colour.

There is no floating bubble. Each chip shows as on when the caret's formatting has it.

**Shortcuts.** Ctrl/Cmd+B, I and U work at both levels. Full adds:

- Ctrl+Shift+X for strikethrough
- Ctrl+E for inline code
- Ctrl+K for a link
- Ctrl+Alt+0 to 3 for paragraph and H1 to H3
- Ctrl+Shift+8 and Ctrl+Shift+7 for bulleted and numbered lists
- Ctrl+Shift+. for a quote

In Basic, the Full-only shortcuts do nothing and Enter inserts a line break.

**No markdown conversion as you type.** Typing `**`, `# ` or `- ` leaves the characters in place. Only paste reads markdown, as Markdown subset already decided.

**Links.** Ctrl+K or the link chip opens a link field anchored just below the caret, or under the link when the caret is inside one. The field has two inputs, Text and Address, plus Apply and Remove buttons.

- Text starts as the selected text, or the link's text when the caret is inside a link. Focus goes to Address when Text is already filled, otherwise to Text, and Tab moves between the two.
- Changing Text replaces the link's text. Leaving it unchanged keeps any formatting inside the link.
- An empty Text uses the address as the text.
- Address rejects any scheme other than http, https and mailto. Clearing it, or pressing Remove, unlinks the text and keeps it.

**Tables.** While the caret is in a table, a table bar sits under the table with these controls:

- add and remove a row
- add and remove a column
- column alignment (left, centre, right)
- delete the table

Tab and Shift+Tab move between cells, and Tab in the last cell adds a row. Ctrl+Enter also adds a row. Enter inside a cell does nothing, since cells hold no line breaks.

## Comments

- From [Markdown subset](03-markdown-subset.md): Full adds tables, so the UI needs a way to add and remove rows and columns. Text's `FontWeight` prop is gone; consider a bold chip in the Property bar that bolds the whole text when nothing is selected. Basic shows bold, italic and underline only. Links follow on Ctrl/Cmd+click, so the editor needs a way to add and edit a link's URL.
