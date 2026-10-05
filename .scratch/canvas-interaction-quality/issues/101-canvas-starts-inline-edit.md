# 101 — The canvas starts every inline edit

**What to build:** A double-press on a text or sticky note starts editing it, F2 does the same from the keyboard, and a newly placed text opens ready to type with its text selected (ADR 0051). ADR 0001 reopens for one seam: `IInlineEditable` with a single parameterless `BeginEdit()`, and registration records at composition time whether a component type implements it. The canvas starts every edit: a double-press on an addressable instance, F2 on a focused one, palette placement by click, Enter, Space or drop, and a new edge label. Copies never open for editing. `BeginEdit` selects all text. If the instance is not fully in view the canvas pans the minimum distance in, pan only; below the LOD cutoff the request is dropped. The built-ins' own double-click handlers go. Escape in an editor commits, reversing the old discard, and returns focus to the instance's tab stop or the edge's stop for a label. The editor makes one `CommitInlineEdit(InstanceId, before, after, returnFocus)` call per edit end on every route, replacing the old commit path for inline edits; creation and edit are two history entries. `ParentCanvas` and `InstanceId` cascading values become part of the contract.

**Blocked by:** 78 (Delete edit mode and the legacy container plumbing)

**Status:** ready-for-agent

**Already shipped with ticket 74:** `IInlineEditable` with a parameterless `BeginEdit()` on `StickyNote` and `Text`, `ComponentRegistration.IsInlineEditable` derived from the component type, and a double-press on an addressable, mounted instance calling `BeginEdit()` through the `DynamicComponent` ref. Ticket 74 took pointer capture on instance presses, which retargets the browser's `dblclick` to the canvas, so this had to move forward to keep double-click editing working. `StickyNote`'s own `@ondblclick` is gone. `Text`'s stays only because edge labels are still on the legacy press path and reach it that way; delete it when the edge-label double-press goes through the canvas. Still to do here: select-all on entry, `preventScroll` on the editor's focus, F2, edit-on-create, the pan-into-view rule, Escape commits and `CommitInlineEdit`.

- [ ] Double-pressing a text or sticky note opens its editor with all text selected; F2 on a focused one does the same
- [ ] Placing a text from the palette by click, by Enter and by drop opens it for typing
- [ ] Escape commits the typed text and focus lands on the shape's tab stop; blur commits as before
- [ ] A double-press on a shape half off-screen pans it fully into view before the editor opens; a placeholdered shape does nothing
- [ ] A pasted or duplicated text never opens for editing
- [ ] Inline-text tests that double-clicked the built-in are rewritten through the canvas route; a probe proves the real F2 key path
- [ ] A baseline shows an open editor on a dark board, since none exists today; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Inline edit` term describes what shipped
