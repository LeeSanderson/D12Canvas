# Glyph drawing handoff

Type: grilling
Status: resolved
Blocked by: 08
Map: [Library icons, Palette and Property bar](../map.md)

## Question

Who draws the final glyphs, and what does the spec hand over?

The Palette, Property bar and panel prototypes drew rough glyphs. Decide whether the spec carries final path data, or lists each glyph with the drawing rules from [How the library defines and renders an icon](03-icon-definition-and-rendering.md) and leaves the drawing to implementation. Also decide how a drawing is accepted (a review page, a visual baseline, the user's sign-off) and the final glyph list, once Context menu glyphs fixes the menu's share.

## Comments

From [Context menu glyphs](08-context-menu-glyphs.md): the menu claims the 8 strip glyphs, about 22 row glyphs and `Glyphs.Check`, and the Image editor's two buttons reuse row glyphs. The set grows to about 65. The 30 to 50 figure was an estimate, not a limit, so the handoff need not trim it.

## Answer

The spec lists the glyphs and the rules. Implementation draws them, and the user signs off on a gallery.

- **Handoff.** The spec carries no path data. It has a glyph table with each glyph's name, what it shows, the sites that use it and its paint parts (stroke, fill, accent). It also restates the drawing rules from [How the library defines and renders an icon](03-icon-definition-and-rendering.md): 16 grid, stroke 1.5, round caps and joins, at least 1 unit of padding, and coordinates placed so a 1.5 stroke lands on whole pixels at 16px. Drawing is its own implementation slice. It starts from the rough paths in `ProtoGlyphs.cs` (`prototype/palette-look`) and `ProtoBarData.cs` (`prototype/property-bar-look`), which already use the part shape. Path data in markdown can't be judged by reading it, and drawing 59 glyphs is execution work, not a decision.
- **Acceptance.** A glyph gallery page in `D12Canvas.Demo` shows every member of `Glyphs` at 16px, at 1x and 2x, in light and dark, with its name. Accent glyphs show in colour, hatch and dashed states. The page stays in the Demo: it's a test page with a baseline, it isn't a throwaway prototype route, and the App doesn't link to it. A glyph is accepted in three steps:
  1. The drawing slice publishes a gallery screenshot, and the user signs it off before the slice commits.
  2. The gallery gets a Playwright baseline in light and dark, so a later path change shows as a diff.
  3. A unit test over `Glyphs` checks every coordinate is inside the 1 to 15 padding box and every member appears in the gallery. Whole-pixel stroke placement is left to the sign-off, since it's hard to test reliably.
- **Final list, 59 glyphs.** Names are working names; the spec matches them to the existing enums.
  - Components (6): Rectangle, StickyNote, Text, Image, Edge, Component (the null fallback).
  - Property bar, accent (4): Fill, Stroke, TextColour, EdgeColour.
  - Property bar, plain (21): StrokeWidth, FontSize, WeightNormal, WeightBold, text align Left/Centre/Right, text vertical align Top/Middle/Bottom, routing Straight/Orthogonal/Curved, source arrow None/Arrow, target arrow None/Arrow, Caret.
  - Context menu strip (8): align Left/Centre/Right/Top/Middle/Bottom, distribute Horizontally/Vertically.
  - Context menu rows and Image editor (24): Check, Cut, Copy, Paste, Duplicate, Delete, SelectAll, Group, Ungroup, BringToFront, BringForward, SendBackward, SendToBack, Lock, Unlock, ZoomToSelection, ZoomToFit, ZoomTo100, SnapToGrid, ObjectSnapping, AddPort, RemovePort, ChooseImage, RemoveImage.
- **Reuse.** The panel tiles reuse the bar's option glyphs. Unlock All reuses Unlock. "Add port here" and "Add port…" share AddPort. The panel's partial-lock line uses Lock. The Image editor's buttons use ChooseImage and RemoveImage.
- **Left out.**
  - The Palette prototype's extra shapes (Ellipse, Diamond, Triangle, Hexagon, Cylinder, Person, Document, RoundedRectangle, EdgeElbow) were demo stand-ins for host types. Consumers supply their own `Glyph`, so the library doesn't ship a shape stock.
  - Variant D's TextGroup and Colours are dropped, because the bar follows variant C.
  - "Image unavailable" gets no broken-image variant; it keeps the Image glyph and its text.
  - The colour flyout's "Theme colour" and "Custom…" rows get no glyph.
