# 101 — The canvas starts every inline edit

**What to build:** A double-press on a text or sticky note starts editing it, F2 does the same from the keyboard, and a newly placed text opens ready to type with its text selected (ADR 0051). ADR 0001 reopens for one seam: `IInlineEditable` with a single parameterless `BeginEdit()`, and registration records at composition time whether a component type implements it. The canvas starts every edit: a double-press on an addressable instance, F2 on a focused one, palette placement by click, Enter, Space or drop, and a new edge label. Copies never open for editing. `BeginEdit` selects all text. If the instance is not fully in view the canvas pans the minimum distance in, pan only; below the LOD cutoff the request is dropped. The built-ins' own double-click handlers go. Escape in an editor commits, reversing the old discard, and returns focus to the instance's tab stop or the edge's stop for a label. The editor makes one `CommitInlineEdit(InstanceId, before, after, returnFocus)` call per edit end on every route, replacing the old commit path for inline edits; creation and edit are two history entries. `ParentCanvas` and `InstanceId` cascading values become part of the contract.

**Blocked by:** 78 (Delete edit mode and the legacy container plumbing)

**Status:** resolved

**Already shipped with ticket 74:** `IInlineEditable` with a parameterless `BeginEdit()` on `StickyNote` and `Text`, `ComponentRegistration.IsInlineEditable` derived from the component type, and a double-press on an addressable, mounted instance calling `BeginEdit()` through the `DynamicComponent` ref. Ticket 74 took pointer capture on instance presses, which retargets the browser's `dblclick` to the canvas, so this had to move forward to keep double-click editing working. `StickyNote`'s own `@ondblclick` is gone. `Text`'s stays only because edge labels are still on the legacy press path and reach it that way; delete it when the edge-label double-press goes through the canvas. Still to do here: select-all on entry, `preventScroll` on the editor's focus, F2, edit-on-create, the pan-into-view rule, Escape commits and `CommitInlineEdit`.

- [x] Double-pressing a text or sticky note opens its editor with all text selected; F2 on a focused one does the same
- [x] Placing a text from the palette by click, by Enter and by drop opens it for typing
- [x] Escape commits the typed text and focus lands on the shape's tab stop; blur commits as before
- [x] A double-press on a shape half off-screen pans it fully into view before the editor opens; a placeholdered shape does nothing
- [x] A pasted or duplicated text never opens for editing
- [x] Inline-text tests that double-clicked the built-in are rewritten through the canvas route; a probe proves the real F2 key path
- [x] A baseline shows an open editor on a dark board, since none exists today; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Inline edit` term describes what shipped

Shipped with three choices the ADR did not spell out. A new edge label pans into view and is dropped below the LOD cutoff, as an instance is, since a long edge can put its midpoint off screen. `focusTabStopAt` now focuses with `preventScroll` for every command handoff, not only the return from an edit, because the container is `overflow: hidden` and any scroll there moves the board behind `ZoomPanTracker`'s back. The keyboard listener's `Escape` row now skips every editable target inside the container, which is what keeps the first `Escape` out of the canvas's own row; an author's own input inside board content gets the same treatment.

Still open: the pan into view is a plain pan write with no transition, because no framing transition exists until ticket 106. `Quick create` (103) does not exist yet, so its edit-on-create waits for it. What an edit that ends empty does is ticket 102.

`Quick create` shipped with ticket 103 and opens an editable copy through `RequestInlineEdit`.
