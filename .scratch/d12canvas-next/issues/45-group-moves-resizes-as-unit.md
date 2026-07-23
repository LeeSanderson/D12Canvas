# 45 — Group moves/resizes as one unit

**What to build:** An end user drags or resizes a selected `Group` and all its members move/scale together, preserving relative layout — via the group's computed bounds. The whole drag is one gesture, undone atomically.

**Blocked by:** 33 (Multi-selection acts as one unit), 44 (Group/ungroup lifecycle)

**Status:** ready-for-agent

- [ ] Moving a group moves every member, preserving relative offsets
- [ ] Resizing a group scales member bounds proportionally within the computed bounding box
- [ ] Nested groups move/scale correctly with their parent
- [ ] One history entry per group move/resize gesture; undo restores all members atomically
- [ ] Screenshot case for a selected group with computed bounds shown
