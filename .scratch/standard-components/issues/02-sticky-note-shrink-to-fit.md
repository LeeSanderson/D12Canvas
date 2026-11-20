# Sticky note shrink-to-fit text

Type: grilling
Status: open
Blocked by:
Map: [Better standard components](../map.md)

## Question

How does a sticky note's text size itself to fit the note by default?

`StickyNoteProps.FontSize` is a fixed number today.

Given by [Sticky note default size](01-sticky-note-default-size.md): Auto is the default for a new note, the note is 100×100 (an 80×80 text area after the 10px padding), and the resize floor is 16×16, so a fixed 10px padding leaves no text area at all on the smallest note.

Decide:

- whether auto replaces `FontSize`, or `FontSize` gains an "Auto" value that is the default and a typed number still pins it
- the maximum and minimum auto sizes, and what happens when text doesn't fit at the minimum (overflow, clip, ellipsis)
- whether the size grows back as text is deleted and as the note is resized
- where the fit is computed: measured in the browser through JS, or estimated in C#. Consider server-side rendering, bUnit tests and visual-test stability
- whether the fit runs live while typing in the inline editor or only on commit
- how mixed bold, italic and underline spans affect the fit (they change glyph widths)
- whether the note's 10px padding stays fixed or shrinks with small notes
