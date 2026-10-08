# 90 — Menu verdict and keyboard menu requests

**What to build:** Right-clicking a sticky note's body opens the object menu rather than the browser's spellcheck menu, right-clicking inside a text field being edited or on a link opens the browser's own menu, and Shift+F10 or the ContextMenu key opens exactly one menu at the selection and returns focus where it was (ADRs 0047, 0067, 0026 keyboard half). The `Menu verdict` for a secondary press on `author-content` is taken once at `pointerdown` by five ordered rules (not addressable, then canvas; editable target or live text selection, then browser; nearest `data-d12-context-menu` marker's value; `a[href]`, `video`, `audio`, then browser; otherwise canvas), stored, and consumed by that press's `contextmenu`, because on Windows `contextmenu` fires after `pointerup` at the release target. `<img>` is not inferred as browser-owned. A component author sets the marker to say whether a right-click in their content is theirs or the canvas's. From the keyboard, the content set splits on whether anything is selected, the menu draws at the selection's on-screen box or the viewport centre, the paste anchor is the viewport centre, and focus returns to the element focused at the keydown. The keydown writes the verdict from a capture-phase window listener (any other key clears it, repeats do neither), prevents the keydown on a canvas verdict, and the browser's trailing `contextmenu` uses the verdict up without classifying its own target. The ContextMenu key acts on keydown. Outside `author-content` the keydown uses the editable-or-selection rule alone. The hand-run probes under `assets/` for tickets 54 and 64 are the starting material for the automated ones.

**Blocked by:** 89 (Composed context menu with shortcut hints)

**Status:** resolved

- [x] Right-click on a sticky note's text at rest opens the object menu; right-click inside its open editor opens the browser menu
- [x] Right-click on a link inside a component opens the browser menu; a component with the marker set to canvas gets the object menu over its whole body
- [x] Shift+F10 with a shape selected opens one menu at its box; the ContextMenu key does the same; closing returns focus to the shape's stop
- [x] Shift+F10 with nothing selected opens the canvas set at the viewport centre
- [x] Any non-menu key clears a stored verdict so a later request inside an editor that stops propagation is not stranded
- [x] A secondary press on `author-content` with a browser verdict is `Native`; with a canvas verdict it is `Pan`
- [x] Probes prove one menu per keypress for both keys in Chromium and that the verdict survives to the trailing `contextmenu`
- [x] bUnit covers the five rules as a table and the keyboard content split
- [x] `CONTEXT.md`'s `Menu verdict` term describes what shipped
