# Inline editor markdown round trip

Type: grilling
Status: open
Blocked by: 03
Map: [Better standard components](../map.md)

## Question

How does `wwwroot/inlineEditor.js` edit formatted text and hand markdown back to C#?

Decide:

- whether the editor works on `contenteditable` HTML and serialises to markdown on commit, or keeps its own span model and renders it
- where the one canonical markdown parser lives (C#, JS, or both kept in step) and how the two sides are tested against each other
- how paste works: markdown text, HTML from other apps, plain text. Each is narrowed to the component's allowed subset
- how the editor stops browsers' own `contenteditable` markup (`<div>`, `<b>` vs `<strong>`, `&nbsp;`) leaking into storage
- undo inside the editor versus the board's undo stack once the edit commits
- what the static (not editing) render is: C# markdown to Blazor markup, so bUnit can test it

## Comments

- From [Markdown subset](03-markdown-subset.md): typed characters are always literal and the serialiser escapes them with CommonMark backslashes (the rule list is in that ticket's answer). Only pasted markdown is read as markup. A paste parses with Full, then flattens anything outside the component's level to plain text before storing. Stored markup outside the level renders as raw text; nothing rewrites it on load. Link targets outside http, https and mailto must never reach an `href`.
