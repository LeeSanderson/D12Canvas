# 81 — Entered group

**What to build:** An end user double-presses a grouped shape to step inside its group and then selects, moves, aligns and edits the members directly, without ungrouping (ADR 0044). `Entered group` is transient view state beside `Selection`, one level at a time, entered by a double-press on a member or Enter on the group's tab stop. While entered, the selection holds only direct members, and content outside the scope does not respond to a primary press: an author control inside an unentered group goes quiet, and JavaScript reads a rendered non-addressable marker rather than any copy of state. Escape steps out one level and selects the group left. A press outside pops as far as needed; empty canvas inside the group's bounds keeps the scope, so a marquee inside a group works. A dashed outline in the muted-text token marks the innermost entered group. `GroupCommand` and `UngroupCommand` gain a parent-membership edit so grouping inside an entered group nests correctly, and Ctrl+G is unavailable when every direct member of the entered group is selected.

**Blocked by:** 66 (A group's members always resolve), 80 (Edges in the selection)

**Status:** resolved

- [x] Double-pressing a member enters its group and selects that member; a second double-press on a nested member enters one level deeper
- [x] Inside a group, a plain press on a member selects only that member and a drag moves only it; a marquee inside the group's bounds selects members
- [x] Escape steps out one level and the group just left is selected; a press on canvas outside the group steps all the way out
- [x] A button inside a component that is in an unentered group does nothing until the group is entered
- [x] The dashed outline renders around the innermost entered group only
- [x] Enter on a group's tab stop enters it from the keyboard; no edge has a tab stop while a group is entered
- [x] Grouping two members inside an entered group produces a nested group whose parent membership is updated in the same entry, and undo restores it
- [x] bUnit covers entry, exit and the addressability rule; a probe proves the non-addressable marker stops the author control
- [x] A demo page shows an entered group for the visual suite; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Entered group` term describes what shipped
