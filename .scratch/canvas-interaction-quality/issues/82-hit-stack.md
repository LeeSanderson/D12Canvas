# 82 — Hit stack

**What to build:** A click inside the multi-selection box selects the shape beneath it, and Alt+click cycles down through shapes stacked under the pointer, wrapping at the bottom (ADR 0046). The `Hit stack` is every entity at the press point, topmost first in paint order, read from the DOM at press with `elementsFromPoint`, resolved through the effective selection id (a grouped member appears as its group unless that group is entered), with locked entities and the selection box left out and edges kept. It is read at press for these two click outcomes only and never decides the `Hit target`. The multi-selection box is already a real hit element; its click outcome becomes "select the top entry beneath it". Alt+click cycles from the entity selected before the press, read from the `Selection snapshot`.

**Blocked by:** 81 (Entered group)

**Status:** resolved

- [x] A click on empty space inside the selection box, above a shape, selects that shape rather than keeping the selection
- [x] Alt+click on a stack of three selects the next one down on each press and wraps to the top after the bottom
- [x] Alt+click on a grouped member outside its entered group selects the next entity beneath, resolved as its group
- [x] Alt is never read at press for anything else and Ctrl+click is untouched
- [x] bUnit covers the two outcomes through the entry points given a supplied stack; a probe proves the stack arrives in paint order from a real page
- [x] `CONTEXT.md`'s `Hit stack` term describes what shipped
