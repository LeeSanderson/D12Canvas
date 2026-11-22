# Sticky note shrink-to-fit text

Type: grilling
Status: resolved
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

## Answer

Model:

- `StickyNoteProps.FontSize` becomes `double?`. Null means Auto, and a number pins the size. A new note gets null.
- A new `PropertyRole.AutoFontSize`, declared as `double?`, edits it. Its control offers "Auto" plus a number box. How the Property bar chip looks waits for the Property bar pass in the map's fog.
- `Text` keeps its plain `double FontSize`, and the shared `PropertyRole.FontSize` stays declared as `double`.
- Old boards store `"FontSize": 14` under the same property name, so they load as a size pinned at 14 with no conversion. Persistence and migration has a note about this.

Fit:

- A new `fitText.js` in `wwwroot` binary-searches the font size from 6px up to the largest size where the wrapped text fits the text area. There is no fixed max. It writes the result straight onto the element's `font-size`. The fitted size is never stored, so a resize makes no props change and no undo entry.
- A `ResizeObserver` and a `MutationObserver` on the text element run the fit again before paint. It runs on resize, on text changes, and live while typing in the inline editor's textarea. The size grows back as text gets shorter or the note gets bigger.
- Bold, italic and underline spans need no rules of their own, because the browser's measurement includes them.
- Text that doesn't fit at 6px clips. A pinned size that doesn't fit clips too, so the text box gets `overflow: hidden` in both modes and a note never draws past its bounds. Clipped text stays stored and editable.

CSS:

- The note gets `container-type: size` and `padding: min(10px, 10cqmin)`. That's 10px on a 100×100 note, as today, and 1.6px on the 16×16 floor.
- An empty note's starting size is `font-size: 50cqh`, half the note's height: 50px on a 100×100 note. C# renders that or the 6px floor as the starting `font-size`, and the fit takes over in the browser.

Tests:

- bUnit asserts the starting style and that the note wires up `fitText.js`. bUnit has no layout, so it can't check the fit itself.
- A Playwright probe in the pinned Docker image asserts the real fit: text fills the note, shrinks as text is added, grows back when text is deleted, and clips at 6px.
