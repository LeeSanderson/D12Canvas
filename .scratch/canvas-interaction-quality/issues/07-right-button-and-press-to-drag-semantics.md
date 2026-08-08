# Right-button semantics and press-to-drag

Type: grilling
Status: open
Blocked by: 01

## Question

Decide what each mouse button means on press, and specifically how right-button drag-to-pan coexists with right-button click-to-open-menu.

Two seed notes, both about presses being interpreted too narrowly today:

**Press should begin a drag.** `ComponentContainer.HandleMouseDown` arms `_isMoving` only when `IsSelected` is already true, so moving an unselected instance requires a click to select followed by a separate press to drag. The reported experience is that shapes are hard to drag; this is why.

**Right-drag should pan the board.** `DiagramCanvas.HandleMouseDown` returns immediately on `e.Button != 0`, so the right button reaches only `HandleContextMenu`, which opens the selection menu when something is selected and otherwise lets the browser's native menu through.

Decide:

- Press-to-select-and-drag semantics. Does a press on an unselected instance select it *and* arm the drag in one gesture? What happens to an existing multi-selection when the press lands on a non-member — does it collapse immediately on press, or only if no drag follows? Shift-press must keep toggling membership without clobbering the result (`HandleClick`'s existing comment documents why plain click and shift-click already differ here).
- How a press that becomes a drag is distinguished from a press that becomes a click. A movement threshold, a time threshold, or both — and the same question for the right button, where the answer decides between panning and opening the menu. `DiagramCanvas` already has a `_dragMoved` flag serving a related purpose; decide whether that generalises.
- What happens on right-press over empty canvas versus over a selection versus over an unselected instance. Today empty canvas yields the browser's own menu, which is almost certainly wrong for a canvas app but is a deliberate current behaviour, not an oversight.
- Whether left-drag on empty canvas keeps panning. It does today, with shift-drag reserved for marquee. If right-drag pans, left-drag on empty space is free to marquee instead — which is what the reference tools generally do (ticket 03 should confirm). This is a behaviour change beyond the seed notes; decide it deliberately.
- Middle button, and whether space-drag-to-pan should exist as a third route.
- How each of these degrades under the touch non-foreclosure constraint, where there is exactly one button.

Supersedes or amends ADR 0009's interaction table.
