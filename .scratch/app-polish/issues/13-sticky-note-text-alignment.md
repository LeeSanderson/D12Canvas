# Sticky-note text alignment

Status: ready-for-agent
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
