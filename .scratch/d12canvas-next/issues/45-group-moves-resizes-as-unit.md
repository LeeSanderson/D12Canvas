# 45 — Group moves/resizes as one unit

**What to build:** An end user drags or resizes a selected `Group` and all its members move/scale together, preserving relative layout — via the group's computed bounds. The whole drag is one gesture, undone atomically.

**Blocked by:** 33 (Multi-selection acts as one unit), 44 (Group/ungroup lifecycle)

**Status:** resolved

- [x] Moving a group moves every member, preserving relative offsets
- [x] Resizing a group scales member bounds proportionally within the computed bounding box
- [x] Nested groups move/scale correctly with their parent
- [x] One history entry per group move/resize gesture; undo restores all members atomically
- [x] Screenshot case for a selected group with computed bounds shown

## Comments

No production code changed. Ticket 44's `DiagramCanvas.ExpandedSelection()` already recursively
flattens a selected `Group` (nested or not) down to its raw leaf `ComponentInstance` ids before the
existing ticket-33 move/resize machinery (`CommitGroupMove`/`CommitGroupResize`/`EffectiveBounds`/
`SelectedInstancesBounds`) ever sees the selection - so a persisted Group already moved/resized as
one unit, atomically, with no separate code path needed. This ticket's remaining work was pinning
that behaviour with dedicated coverage ticket 33/44's own tests never exercised together:

- `D12Canvas.Tests/DiagramCanvasGroupMoveResizeTests.cs` - dragging a member or empty bbox space of
  a *persisted* Group, resizing via its bottom-right and top-left handles, and two nested-group
  cases (move and resize) proving every leaf of a group-within-a-group scales/shifts together. All
  pass unmodified against existing production code.
- `D12Canvas.Tests/DiagramCanvasUndoRedoTests.cs` gains
  `UndoAfterAPersistedGroupMove/ResizeRevertsEveryMemberInOneStep`, alongside ticket 33's own
  `UndoAfterAGroupMove/ResizeRevertsEveryMemberInOneStep` for an ad-hoc selection - undo/history
  coverage lives there by established convention, not in the feature-specific test file above.
- `D12Canvas.VisualTests/MultiSelectionMoveResizeVisualTests.cs` -
  `PersistedGroupBoundingBoxVisible_MatchesBaseline`, driven by a real Ctrl+G keypress (not
  marquee/shift-click) - confirms the bounding-box overlay renders identically for a persisted Group.

Running the Playwright visual suite (pinned `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` via
Docker/Podman, per README) reproduced ticket 78's already-known, unresolved environment drift: most
pre-existing baselines (untouched by this ticket) failed on an HTML-only scoped-CSS class-hash
mismatch, and the `GroupResizeInProgress` PNG baseline (ticket 33, also untouched) failed on a
1-byte, visually-identical PNG re-encoding. Neither is caused by this ticket - no rendering code
changed - so only this ticket's own new baseline was accepted; the rest are ticket 78's to fix.
