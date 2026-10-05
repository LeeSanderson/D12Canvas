# 100 — Locking

**What to build:** An end user locks a shape or edge so that no command changes it and no primary press or marquee catches it, while it stays reachable by Tab and visible in the panel (ADRs 0017 locked half, 0058, 0065). `Locked` is a persisted, undoable bool on `ComponentInstance` and `Edge`, absent by default, one optional envelope field with no schema bump. A group is locked when every resolving member is. A new `ChangeLockedCommand` widens the command set; every command skips a locked entity rather than failing. A locked entity takes no part in primary-press hit-testing (JavaScript reads the rendered `Locked` marker), marquee or any command; a secondary press reaches and selects it and offers Unlock; Unlock All is a composite on the canvas menu; Ctrl+Shift+L locks or unlocks; the Lock row reads Unlock when every top-level selected entity is locked. The panel shows a locked entity with its fields disabled and an unlock control live. An operation on a partly locked group acts on its unlocked members: move and nudge apply one delta to unlocked leaves; resize keeps handles on the group's real bounds and scales unlocked members inside; align and distribute measure the unlocked box and a fully locked group does not count toward the thresholds; delete removes only unlocked members and the group repair follows. Copying is not modifying: a copy, duplicate or paste of a locked entity is locked, and Cut carries only what its delete removes.

**Blocked by:** 95 (System clipboard), 99 (Align and distribute)

**Status:** ready-for-agent

- [ ] Lock from the menu or Ctrl+Shift+L; a locked shape cannot be pressed, marqueed, nudged, deleted or resized, and Ctrl+Z undoes the lock
- [ ] Right-click on a locked shape selects it and the menu shows Unlock; Unlock All on the canvas menu unlocks everything in one entry
- [ ] Tab reaches a locked shape; the panel shows its fields disabled with an unlock control
- [ ] Dragging a group with one locked member moves the others and leaves the locked one in place; resizing scales the unlocked members inside the real bounds
- [ ] Aligning a selection with a partly locked group measures its unlocked members' box
- [ ] Deleting a partly locked group removes the unlocked members and the group repairs; a fully locked group is untouched
- [ ] Copy, duplicate and paste of a locked shape produce a locked copy; Cut with only locked entities selected is ineligible
- [ ] A locked edge cannot be repositioned and is not a drop target
- [ ] A board saved without the field loads unchanged and serialises byte-identically
- [ ] Command, serializer and hit-test tests; the press-to-kind table gains locked rows
- [ ] Full visual suite run in the pinned image with `-parallel none` if any markup changes; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Locked` term describes what shipped
