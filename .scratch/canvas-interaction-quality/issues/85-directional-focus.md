# 85 — Directional focus

**What to build:** Ctrl+Shift+Arrow moves focus to the nearest shape in that direction, so crossing a dense board is not thirty Tabs (ADR 0060). Candidates are the current ring (viewport plus overscan, or the entered group's members), instance and group stops only, never edges. A stop in the same row or column wins by gap, otherwise the smallest gap plus perpendicular offset, ties broken by reading order, no wrap, no pan. The origin is the focused stop, else the selection's box, else the viewport centre. Like Tab, it selects its target, except inside `Additive traversal` where it moves focus only. It is a no-op during a gesture, placement or picking, and the row calls `preventDefault` even on a no-op because Firefox otherwise scrolls and starts a text selection.

**Blocked by:** 73 (One shortcut table behind one focus guard)

**Status:** resolved

- [x] Ctrl+Shift+Right from a shape lands on the nearest shape to its right in the same row before a nearer one off-row; nothing in that direction leaves focus where it is
- [x] With nothing focused, the move measures from the selection's box; with nothing selected, from the viewport centre
- [x] Inside additive traversal the move changes focus and not the selection
- [x] The row prevents default on every press, including a no-op
- [x] Pure C# tests for the candidate ordering; bUnit for the keydown route
- [x] `CONTEXT.md`'s `Directional focus` term describes what shipped

Shipped with two choices the ADR did not spell out. A candidate off the origin's row or column scores its gap plus the distance between the two boxes' perpendicular extents, not their centres, so a tall neighbour is not penalised for its height. A zero-size origin, the viewport centre or a floating edge source, counts as in a stop's row when the point lies within the stop's extent. Focus leaving the container now also forgets the focused stop, so a move after tabbing out measures from the selection rather than from a stop that no longer has focus.

Still open: keyboard port placement (ticket 104) does not exist yet, so the placement guard has nothing to hook; it belongs beside the port-picking guard in `OnDirectionalFocusPressed`. Ticket 73's shortcut table has not landed either, so the chord is a branch in the existing arrow row.
