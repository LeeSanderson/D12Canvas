# 97 — Clone drag

**What to build:** Holding Alt while dragging leaves the originals in place and drags copies instead, so duplicating a cluster into position is one gesture (ADR 0042). `Clone drag` is `MoveSelection` with Alt, not a ninth gesture. Alt is read live: the copies are exactly what duplicate would build, held in the `Gesture preview`'s pending fragment from promotion, rendered with the selected look, and nothing is built below the threshold. Switching Alt mid-drag switches between moving and cloning and the release commits whatever was last on screen. At release the copies are added to `Board`, become the selection and start a `Duplicate run`. Originals are snap candidates, copies are not. A clone drag of a partly locked group copies the whole group.

**Blocked by:** 86 (Live modifiers, axis lock and snap-to-grid on by default), 96 (Duplicate and the duplicate run)

**Status:** ready-for-agent

- [ ] Alt+drag on a selection of two connected shapes leaves them where they are and drops copies with their edge at the release point, in one history entry
- [ ] Pressing Alt mid-move shows the originals snap back and copies appear under the pointer; releasing Alt reverses it
- [ ] After release the copies are selected and Ctrl+D continues at the drag's offset
- [ ] Escape mid-clone leaves the board untouched and the originals selected
- [ ] The pending fragment is readable through the preview; gesture-object tests cover the live switch and the commit
- [ ] A mid-clone visual baseline exists; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Clone drag` term describes what shipped
