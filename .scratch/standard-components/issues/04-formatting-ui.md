# Formatting UI

Type: prototype
Status: open
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

## Comments

- From [Markdown subset](03-markdown-subset.md): Full adds tables, so the UI needs a way to add and remove rows and columns. Text's `FontWeight` prop is gone; consider a bold chip in the Property bar that bolds the whole text when nothing is selected. Basic shows bold, italic and underline only. Links follow on Ctrl/Cmd+click, so the editor needs a way to add and edit a link's URL.
