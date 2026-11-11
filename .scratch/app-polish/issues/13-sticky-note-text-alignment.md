# Sticky-note text alignment

Status: resolved
Blocked by:

Spec: [App polish](../spec.md), "Sticky note text alignment (library)". Decision record: [Sticky note text alignment](04-sticky-note-text-alignment.md). Closes `.scratch/backlog/001-skicky-notes.md`.

## What to build

A new sticky note centres its text both ways. In the property bar, a board author can set a note's horizontal alignment (left, center, right) and vertical alignment (top, middle, bottom). Notes on boards saved before this change also show centred.

The sticky note's props gain `TextAlign` on the existing `TextAlign` property role, and `VerticalAlign` on a new `VerticalAlign` property role. Both are string dropdowns, not enums, so `TextAlign` still merges with the text component across a mixed selection. The new role declares a dropdown editor, CLR type `string`, merged-row label "Vertical align", and has its own glyph in the property bar.

Defaults are `center` / `middle` in the registration's default props and in the record's positional parameter defaults, so a board JSON without the two props deserialises centred. No value is ever null.

Text stays in the same place when inline editing starts and ends: the editor sizes to its content and the note places it by `VerticalAlign` (growing the textarea to its `scrollHeight`, or `field-sizing: content`; implementer's choice). Text taller than the note uses `safe` alignment, so its first line stays visible.

## Acceptance criteria

- [ ] A newly placed sticky note renders its text centred horizontally and vertically.
- [ ] A board JSON whose sticky notes have no alignment props loads them as `center` / `middle`.
- [ ] Selecting only sticky notes shows Text align and Vertical align rows in the property bar, and changing either updates the note and is undoable.
- [ ] Selecting a text box and a sticky note together shows one merged Text align row (Mixed when they differ) and no Vertical align row.
- [ ] Starting and ending an inline edit doesn't move the text, for every combination of the two alignments.
- [ ] Text taller than the note starts at the top and isn't clipped at its first line.
- [ ] `CONTEXT.md`'s Property role entry lists `VerticalAlign` in its closed set.
- [ ] bUnit tests in `D12Canvas.Tests` cover the defaults, loading old JSON, the property bar rows and the cross-type merge, following the existing `TextProps`, property role and Mixed tests.
- [ ] The full Playwright visual suite runs in the pinned Docker image with `-parallel none`. The changed sticky-note baselines are reviewed and folded in, per `docs/agents/testing.md`.

## Comments

Resolved 2026-11-11. `StickyNoteProps` gained `TextAlign` and `VerticalAlign` with `center` / `middle` defaults in both the record and the registration. `PropertyRole.VerticalAlign` was added with its own property bar glyph. The note is now a flex column placed by `justify-content` (`flex-start`, `safe center`, `safe flex-end`). The editor uses `field-sizing: content`, so it is as tall as its text. The `<p>` now has `white-space: pre-wrap` and `overflow-wrap: break-word` to match the textarea, which means line breaks in a note now show outside editing too.

A new demo page, `/sticky-note-alignment-demo`, backs `StickyNoteAlignmentVisualTests`. It has a screenshot of all nine pairings plus an overflowing note, and a check that the text box doesn't move when an edit starts or ends for each pairing. 19 existing baselines moved, all because built-in sticky notes now centre their text. The two editing baselines also show the new Text align and Vertical align cells.

Known gaps:
- `field-sizing: content` is Chromium and recent Safari only. In Firefox the editor falls back to its default height, so a middle or bottom note's text shifts during editing. Growing the textarea to its `scrollHeight` would fix that.
- Text ending in a newline is one line taller in the textarea than in the `<p>`, so it shifts by half a line on a middle note.
