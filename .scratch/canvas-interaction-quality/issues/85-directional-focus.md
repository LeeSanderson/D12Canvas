# 85 — Directional focus

**What to build:** Ctrl+Shift+Arrow moves focus to the nearest shape in that direction, so crossing a dense board is not thirty Tabs (ADR 0060). Candidates are the current ring (viewport plus overscan, or the entered group's members), instance and group stops only, never edges. A stop in the same row or column wins by gap, otherwise the smallest gap plus perpendicular offset, ties broken by reading order, no wrap, no pan. The origin is the focused stop, else the selection's box, else the viewport centre. Like Tab, it selects its target, except inside `Additive traversal` where it moves focus only. It is a no-op during a gesture, placement or picking, and the row calls `preventDefault` even on a no-op because Firefox otherwise scrolls and starts a text selection.

**Blocked by:** 73 (One shortcut table behind one focus guard)

**Status:** ready-for-agent

- [ ] Ctrl+Shift+Right from a shape lands on the nearest shape to its right in the same row before a nearer one off-row; nothing in that direction leaves focus where it is
- [ ] With nothing focused, the move measures from the selection's box; with nothing selected, from the viewport centre
- [ ] Inside additive traversal the move changes focus and not the selection
- [ ] The row prevents default on every press, including a no-op
- [ ] Pure C# tests for the candidate ordering; bUnit for the keydown route
- [ ] `CONTEXT.md`'s `Directional focus` term describes what shipped
