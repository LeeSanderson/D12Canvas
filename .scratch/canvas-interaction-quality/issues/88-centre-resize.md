# 88 — Centre resize

**What to build:** Holding Alt while resizing keeps the selection's centre fixed and mirrors the opposite edge, so a shape grows in place (ADR 0057). `Centre resize` is `ResizeSelection` with Alt, not a ninth gesture. The centre of the instances-only box read at press stays fixed, the opposite edge mirrors the handle, a toggle mid-gesture recomputes from the start box so the opposite edge jumps while the handle stays under the pointer, the minimum size clamps symmetrically, and on each driven axis both edges are snap anchors with the smallest correction winning. Members scale inside the box as on any resize.

**Blocked by:** 86 (Live modifiers, axis lock and snap-to-grid on by default)

**Status:** ready-for-agent

- [ ] Alt+drag on a right handle grows both left and right edges equally about the centre; a corner handle drives both axes
- [ ] Pressing or releasing Alt mid-resize switches modes with the handle staying under the pointer
- [ ] The floor clamps symmetrically so the centre never moves
- [ ] Under grid snap, whichever of the two moving edges needs the smaller correction wins
- [ ] A mixed selection's centre is the instances-only box's centre
- [ ] Gesture-object tests over a fake context; the release-reliability theory is unchanged because this is a mode, not a member
- [ ] `CONTEXT.md`'s `Centre resize` term describes what shipped
