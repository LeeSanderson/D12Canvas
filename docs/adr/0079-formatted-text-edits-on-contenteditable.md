# Formatted text is edited on contenteditable with execCommand, and C# owns the format

The inline editor for formatted text edits the browser's `contenteditable` DOM directly, and every formatting change goes through `document.execCommand`. That API is deprecated, but no browser has replaced it, and it is the only way to keep the browser's native undo, IME composition, spellcheck and caret behaviour. The alternative, a document model in JS where every `beforeinput` is cancelled and re-applied, behaves the same in every browser. It would mean hand-writing composition, Android input, caret mapping and an undo stack with no third-party editor, and that cost decided it.

**C# owns the format.** C# parses markdown into a JSON document tree, serialises the tree back to markdown, narrows it to a formatting level, and renders it to an escaped HTML string. The static render outputs that string as a `MarkupString`, and the editor is seeded with the same string. JS holds one piece of format logic: a walk from DOM to tree, run on commit and on HTML paste. It keeps whitelisted elements and drops everything else to its text.

## Consequences

- Nothing may change the editor's DOM directly. Operations with no native command (table rows, columns and alignment, inline code, link edits) select the enclosing element and `insertHTML` its rebuilt HTML, so each is one native undo step. A direct DOM edit breaks the browser's undo stack.
- Chrome and Firefox produce different markup and behave differently for the same command. The DOM walk absorbs differences in markup. Behaviour differences are found by interaction probes and the manual check list, not by design.
- Text safety rests on the C# HTML writer, not Blazor's automatic escaping, so the writer has its own XSS fixture set.
- If browsers ever remove `execCommand`, the formatting actions need rewriting. The C# parser, serialiser, renderer and the DOM walk do not.
