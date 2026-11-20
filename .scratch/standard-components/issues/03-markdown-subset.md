# Markdown subset

Type: grilling
Status: open
Blocked by:
Map: [Better standard components](../map.md)

## Question

Which markdown does the library support, and how does each component limit it?

Text (and connector labels) get the full subset. Sticky notes and rectangles get bold, italic and underline only. The library writes its own parser and renderer, so every construct costs code.

Decide:

- the inline constructs for Text: bold, italic, underline, strikethrough, inline code, links, hard line breaks, others
- the block constructs for Text: paragraphs, headings, bullet and numbered lists, nesting depth, blockquotes, code blocks, tables, horizontal rules. Which earn a place in a fixed-size canvas box
- the underline syntax, since markdown has none: inline `<u>…</u>`, `++text++`, or something else. It must survive paste in from and copy out to other tools
- how a component declares its allowed subset, and what happens to disallowed markup: pasted into a sticky note, or loaded from a board
- how literal characters such as `*` and `_` typed by a user are escaped so they don't turn into formatting
- whether span colour and span font size are wanted at all, since markdown can't carry them without inline HTML
- how existing boards' plain `Text` strings load: as markdown, which may misread stray `*`, or escaped on migration (feeds Persistence and migration)
