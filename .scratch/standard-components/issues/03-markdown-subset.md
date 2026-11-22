# Markdown subset

Type: grilling
Status: resolved
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

## Answer

Two formatting levels, fixed per component type at registration (terms in `CONTEXT.md`: Formatted text, Formatting level).

**Full** (Text, and so every connector label):

- Inline: bold, italic, underline, strikethrough, inline code, links, line breaks. A plain newline in the source is a line break, not markdown's two-trailing-spaces rule.
- Links follow on Ctrl/Cmd+click only. A plain click selects as it does today, on locked components too. Links open in a new tab with `target="_blank" rel="noopener noreferrer"`, and hovering shows the URL. Allowed schemes are `http:`, `https:` and `mailto:`. Any other target, relative URLs included, renders as the link text with no link, and the source keeps it. Bare `http(s)://` URLs autolink up to the next whitespace, with trailing punctuation trimmed.
- Underline is exactly `<u>…</u>`. No other HTML is recognised. `__x__` is bold, as CommonMark says. The serialiser always writes `**` for bold, `*` for italic and `<u>` for underline.
- Blocks: paragraphs, headings `#` to `###` only, bullet and numbered lists nested up to 3 levels, a one-level blockquote, fenced code blocks with no syntax highlighting, horizontal rules, and GFM pipe tables.
- Tables need a header row and take column alignment from `:` in the separator row. Cells hold inline formatting only (no blocks, no line breaks). A literal `|` is written `\|`. Column widths are automatic and nothing stores them. No merged cells and no row headers.
- No task lists.
- Heading sizes scale with the Text's `FontSize`, through tokens.

**Basic** (sticky notes, rectangles): bold, italic, underline, line breaks.

**Markup outside a component's level** shows as its raw text when loaded. Nothing is rewritten on load. Pasting parses with Full, then flattens anything not allowed down to plain text before storing: a heading becomes its text on its own line, a list item keeps a literal `•` or `1.` prefix, a link becomes its text, code becomes its text, a table becomes one line per row with cells separated by spaces, a quote keeps its text, and a rule is dropped.

**Escaping:** CommonMark backslash escapes. A backslash before any ASCII punctuation is literal on read. The serialiser escapes only where a character would start syntax: `*`, `_`, `[`, `]` and `` ` `` always; `#`, `>`, `-`, `+` and `1.` at line start; `|` in table cells; `<` before `u>` or `/u>`. Typed text is never read as markup. Only pasted markdown is.

**No span colour or span font size.** `Color` and `FontSize` stay whole-box props. Pasted HTML styling is dropped and its text kept.

**No migration.** The project hasn't been released, so existing text loads as markdown and there's no schema version bump for it.

**Text's `FontWeight` prop is removed.** Whole-text bold is `**…**`.
