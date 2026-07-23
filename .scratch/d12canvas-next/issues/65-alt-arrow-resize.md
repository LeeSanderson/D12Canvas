# 65 — Alt+Arrow resize

**What to build:** A keyboard user resizes without a pointer: Alt+Arrow grows or shrinks the focused/selected instance along one axis per press, with a stable anchor so the instance doesn't drift. Steps are zoom-relative and undoable. (ADR 0010.)

**Blocked by:** 37 (History core: undo/redo move & resize), 63 (Tab stops + focus-follows-selection)

**Status:** ready-for-agent

- [ ] Alt+Right/Left grows/shrinks horizontally; Alt+Down/Up grows/shrinks vertically
- [ ] The anchor stays stable — resizing never moves the anchored edge
- [ ] Steps are zoom-relative; bounds can never invert or go negative
- [ ] Resize steps record undoable history entries
- [ ] bUnit coverage of anchoring and step behaviour
