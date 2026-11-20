# Sticky note default size

Type: grilling
Status: resolved
Blocked by:
Map: [Better standard components](../map.md)

## Question

What size is a new sticky note when it's dropped on the board?

Today's default is 200×200 (`BuiltInComponents.cs`) with a 14px font. The ask is "about a quarter of the size".

Decide:

- a quarter of the area (100×100) or something else
- whether the default font size changes with it, given shrink-to-fit (see Sticky note shrink-to-fit text)
- whether a sticky note has a minimum size when resized
- whether existing boards keep their notes' sizes (they should, since size is stored per instance, but confirm nothing reads the default at load)

## Answer

- A new sticky note is 100×100, a quarter of today's 200×200 area. Set in the sticky-note registration's `DefaultSize` in `BuiltInComponents.cs`.
- A new sticky note's font defaults to Auto. How Auto is modelled, its min and max sizes and its overflow behaviour belong to Sticky note shrink-to-fit text.
- Sticky notes get no minimum size of their own. The shared resize floor in `ResizeMath` (`DefaultMinWidth`/`DefaultMinHeight`) drops from 50×50 to 16×16 for every component. 16px keeps the resize handles grabbable. This also fixes Text, whose 40px default height sits under today's 50px floor, so any resize that touches a Text instance's height clamps it to 50. A multi-selection's scaled floor (`MinimumBoxSizeFor`) follows the new value without changes.
- Existing boards keep their notes' sizes. `DefaultSize` is only read when an instance is created (Palette drop, image fallback, pasted-text fallback). Loading a board uses each instance's stored bounds.
- The registration API gains no per-type minimum.
