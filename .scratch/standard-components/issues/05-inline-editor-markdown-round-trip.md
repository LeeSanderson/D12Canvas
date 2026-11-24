# Inline editor markdown round trip

Type: grilling
Status: resolved
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

## Answer

The editor edits the browser's `contenteditable` DOM, as the Formatting UI prototype did, and C# owns the format. The reasoning is in [ADR 0079](../../../docs/adr/0079-formatted-text-edits-on-contenteditable.md). A document model in JS was considered first and dropped: hand-writing composition, Android input, caret mapping and undo cost too much for what it gave.

**Editing.** While editing, the DOM is the live document. Typing, IME, spellcheck and the caret are the browser's own. Every formatting change goes through `execCommand`, with `styleWithCSS` false and `defaultParagraphSeparator` set to `p`. In Full, Enter starts a new paragraph and Shift+Enter inserts a line break. In Basic, Enter inserts a line break and the Full-only shortcuts do nothing. Drag-and-drop inside the editor is cancelled.

**One parser, in C#.** C# turns markdown into a document tree, serialises the tree to markdown with Markdown subset's escaping rules, narrows a tree to a formatting level, and renders a tree to an HTML string. The tree goes between C# and JS as JSON, and that JSON is the whole contract between them. JS has one piece of format logic: a walk from DOM to tree. The walk keeps the whitelisted elements, maps `<b>`, `<i>`, `<strike>` and the like to the canonical marks, and drops anything else to its text. No second markdown parser exists, so nothing has to be kept in step.

**Static render.** The C# HTML writer escapes all text and only writes an `href` that passed the http/https/mailto check. The static render outputs the string as a `MarkupString`, which bUnit can test. When an edit starts, Blazor renders an empty `contenteditable` container, since Blazor can't diff DOM the browser has changed, and JS sets the same string as its `innerHTML`. Nothing in JS turns a tree into HTML, so there's no parity test.

**Commit.** Blur or Escape runs the walk, sends the tree to C#, serialises it to markdown, and commits one board command, as `CommitInlineEdit` does today.

**Paste.**

- When `text/html` carries formatting we recognise, it's used. That means semantic elements (`strong`/`b`, `em`/`i`, `u`, `s`/`del`, `code`, `a`, `h1`–`h6`, `ul`/`ol`/`li`, `blockquote`, `pre`, `hr`, `table`) or the inline styles `font-weight` 600 and up, `font-style: italic` and `text-decoration` underline or line-through. JS parses it with `DOMParser` and walks it to a tree.
- Otherwise `text/plain` goes to C# and is parsed as markdown with Full. Colour-only HTML from VS Code then still pastes as markdown.
- Either way, C# narrows the result to the component's level and returns HTML that JS inserts with `insertHTML`. The paste handler calls `preventDefault` synchronously and awaits that call.
- Ctrl/Cmd+Shift+V pastes plain text literally, with nothing parsed.
- Copy and cut write the markdown source as `text/plain` and the rendered HTML as `text/html`.

**Undo.** Undo inside the editor is the browser's own, so nothing may edit the editor's DOM directly. Operations with no native command select the whole enclosing element, rebuild its HTML from the DOM, `insertHTML` it in place, and put the caret back. That covers table rows, columns, alignment, deleting the table and Tab adding a row (caret by cell row, column and text offset), inline code on and off, and link edits (caret by text offset). Each is one undo step. The board never sees Ctrl+Z while editing. Once the edit commits, the board's undo reverts the whole edit.

**What the editor reports.** On `selectionchange`, JS reports the caret's active formatting to .NET for the Property bar's chips. Bold, italic, underline and strike come from `queryCommandState`. Code, link, block kind, list and table come from walking up from the caret. It also reports the caret's rect, which places the Ctrl+K link field and the table bar.

**Testing.**

- C# unit tests cover markdown ↔ tree round trips, tree → HTML with an XSS fixture set (`<script>`, `javascript:` hrefs, quotes that break attributes), and narrowing.
- Interaction probes cover the DOM walk against the markup Chrome and Firefox actually produce for each command, and each table, code and link operation followed by Ctrl+Z.
- Real IME and Android typing can't be automated here, so they go on the manual check list.

## Comments

- From [Markdown subset](03-markdown-subset.md): typed characters are always literal and the serialiser escapes them with CommonMark backslashes (the rule list is in that ticket's answer). Only pasted markdown is read as markup. A paste parses with Full, then flattens anything outside the component's level to plain text before storing. Stored markup outside the level renders as raw text; nothing rewrites it on load. Link targets outside http, https and mailto must never reach an `href`.
- From [Formatting UI](04-formatting-ui.md): the editor is WYSIWYG, with formatting applied from the Property bar and shortcuts while editing and no markdown conversion as you type. The editor has to report the caret's active formatting so the bar can show chips as on, place the Ctrl+K URL field at the caret, and run table row, column and alignment operations. The prototype used `contenteditable` with `execCommand`, but that was for speed, not a decision.
