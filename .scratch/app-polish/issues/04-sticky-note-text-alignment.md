# Sticky note text alignment

Type: grilling
Status: resolved
Blocked by:

## Question

What is the model for sticky-note text alignment in the library?

Backlog source: `.scratch/backlog/001-skicky-notes.md`. A new note should centre its text both ways, and the property panel should let the user change horizontal and vertical alignment.

Settle:

- Prop names and values on the StickyNote props (enums or strings, and what the panel shows).
- The default for new notes, and how a saved board whose notes have no alignment value renders: centred like new notes, or top-left as today.
- Whether other text-bearing types (`Text`, edge labels) get the same props now or later.
- New domain terms for `CONTEXT.md`, if any.

## Answer

Resolved by grilling with the user, 2026-11-10.

**Props.** `StickyNoteProps` gains two string dropdowns, mirroring `TextProps`:

- `TextAlign`, options `left`, `center`, `right`, on the existing `PropertyRole.TextAlign`.
- `VerticalAlign`, options `top`, `middle`, `bottom`, on a new `PropertyRole.VerticalAlign`. The role declares `EditorKind.Dropdown`, CLR type `string`, merged-row label "Vertical align". The Property bar needs a glyph for it.

C# enums were rejected because `TextAlign` is already a string role, and an enum would break the cross-type merge with `Text`.

**Defaults.** New notes are `"center"` / `"middle"`, set in the registration's `DefaultProps`. The record's positional parameters also default to `"center"` / `"middle"`. System.Text.Json fills a missing constructor parameter from its C# default, so a board saved before this change loads its notes centred. No value is ever null, and Mixed compares real values.

**Cross-type selection.** A selection holding `Text` and sticky notes merges on `TextAlign`. `VerticalAlign` shows only when sticky notes are the only type selected.

**Scope.** Sticky note only. `Text` gets no vertical alignment in this effort (out of scope on the map). Edge labels don't exist in the library, and `Rectangle` has no text.

**Inline edit.** Text sits in the same place while editing as when rendered. Entering and leaving edit must not move it. A plain `<textarea>` filling the note can't centre vertically, so the editor sizes to its content and the note places it by `VerticalAlign`. The implementer picks the mechanism, for example growing the textarea to its `scrollHeight` or `field-sizing: content`.

**Overflow.** Text taller than the note starts at the top so its first line stays visible (`safe` alignment) instead of clipping at both ends.

**Glossary.** `CONTEXT.md`'s `Property role` entry now lists `VerticalAlign` in the closed set. No new term. No ADR, since the choice is cheap to reverse and not surprising.

**Testing.** This changes the library's rendered markup and a shared `<style>` block, so the implementing issue runs the full visual suite per `docs/agents/testing.md`. The sticky-note baselines will change because the default is now centred.
