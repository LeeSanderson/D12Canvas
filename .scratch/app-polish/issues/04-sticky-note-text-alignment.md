# Sticky note text alignment

Type: grilling
Status: open
Blocked by:

## Question

What is the model for sticky-note text alignment in the library?

Backlog source: `.scratch/backlog/001-skicky-notes.md`. A new note should centre its text both ways, and the property panel should let the user change horizontal and vertical alignment.

Settle:

- Prop names and values on the StickyNote props (enums or strings, and what the panel shows).
- The default for new notes, and how a saved board whose notes have no alignment value renders: centred like new notes, or top-left as today.
- Whether other text-bearing types (`Text`, edge labels) get the same props now or later.
- New domain terms for `CONTEXT.md`, if any.
