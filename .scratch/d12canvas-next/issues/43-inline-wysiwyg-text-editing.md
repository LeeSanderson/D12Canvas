# 43 — Inline WYSIWYG text editing

**What to build:** An end user edits a text-carrying built-in's content directly on the canvas — WYSIWYG, in place, not through a panel. Committing on blur records exactly one undoable gesture (introducing `MutateEntity`, the command for opaque props mutations); Escape cancels the edit without touching history. (ADR 0008 — `Text`-type content is inline-edited, excluded from the property panel.)

**Blocked by:** 37 (History core: undo/redo move & resize), 40 (Sticky Note built-in), 41 (Text built-in)

**Status:** resolved

- [x] Sticky Note and Text content is editable in place on the canvas
- [x] Committing on blur is one history entry (`MutateEntity`); undo restores the prior text
- [x] Escape cancels the edit, reverting content with no history entry
- [x] Edited text round-trips through persistence
- [x] Screenshot case for the in-editing state

## Comments

`MutateEntityCommand` (`D12Canvas/History/MutateEntityCommand.cs`) mirrors `ChangeBoundsCommand`
exactly - `Apply`/`Undo` swap `ComponentInstance.Props` between `before`/`after`, both typed `object`
per ADR 0007 (Props stays opaque; no per-type undo logic needed or possible). `DiagramCanvas` gained
one new public method, `CommitPropsChange(instanceId, before, after)`, doing the usual
`_history.Do(...)` + `StateHasChanged()` - the same shape as `MoveComponent`/`ResizeComponent`.

**Wiring a built-in back to Board/History:** the registration contract (ADR 0001) only guarantees a
fixed `Props` parameter - there's no existing callback contract, and adding one generically via
`DynamicComponent`'s `Parameters` dictionary would throw for every built-in that didn't opt in
(Blazor's parameter binding rejects unmatched dictionary keys). Solved via a second cascading value:
`DiagramCanvas.razor` now cascades `instance.Id` as `"InstanceId"` alongside the existing
`"ParentCanvas"` cascade - both are safe to reach every built-in indiscriminately, since cascading
values never throw for a descendant that doesn't declare a matching `[CascadingParameter]` (Rectangle/
Image don't, and are unaffected). Sticky Note/Text each declare both and call
`ParentCanvas?.CommitPropsChange(InstanceId, Props, Props with { Text = _editBuffer })` on blur.

**Editor UI:** a `<textarea>` swapped in for the `<p>` while editing (not literal `contenteditable`)
- plain two-way `@bind`/`@bind:event="oninput"` gives live buffer updates for free, and it's WYSIWYG
in the sense the ticket cares about (edited in place, at the same position or, in this case, over the
whole rendered element - not through the panel), without needing new JS interop for text capture.
Auto-focus on entering edit via `ElementReference.FocusAsync()` in `OnAfterRenderAsync` (bUnit has a
built-in handler for this, verified via `Bunit.FocusAsyncAssertJSInteropExtensions`/
`FocusAsyncInvocationHandler` - no special test setup needed).

**Escape vs. the global keydown listener:** `DiagramCanvas.razor.js`'s `window`-level keydown
listener unconditionally calls `event.preventDefault()` for every keystroke, which would otherwise
break ordinary typing in the new editable field once contenteditable-adjacent input existed - and
Escape there would also double-fire the global `OnEscapePressed` (clear selection). Both avoided the
same way: `@onkeydown:stopPropagation` on the editor's own `@onkeydown` handler stops the event
before it ever reaches `window`, so no JS changes were needed. Escape sets a `_suppressNextBlurCommit`
flag rather than trying to prevent the native blur that follows removing the textarea from the DOM -
robust regardless of the exact timing of that blur.

**Legacy double-click collision (`ComponentContainer.SwitchToEditMode`):** the outer container
already had its own, unrelated `@ondblclick="SwitchToEditMode"` (a pre-Board-model free drag/resize
toggle). The `<p>`'s own `@ondblclick="BeginEdit" @ondblclick:stopPropagation` keeps the two from
double-firing, and the active editor's `<textarea>` carries a bare `@ondblclick:stopPropagation` too
(no handler - just blocks the bubble) so a double-click made *while already editing* (e.g. to select
a word) can't reach it either. Caught in code review: an earlier draft had the trigger on Sticky
Note's *outer* div (present in both view and edit states), which meant double-clicking inside the
open editor re-fired `BeginEdit` and silently discarded whatever was half-typed - moving the trigger
to the `<p>` (view-mode only) fixes this by construction, not just a guard.

**Found and fixed as a necessary side effect: ticket 75.** `ComponentContainer.ShouldRender()` only
compared `X`/`Y`/`Width`/`Height`/`_editMode`/`IsSelected`/`IsMultiSelected` - exactly the gap ticket
75 already documented, and this ticket's core flow (edit text, same `Bounds`) hits it immediately:
`Board`/`ComponentInstance.Props` updated correctly, but the screen kept showing the old text since
the container skipped re-rendering its `ChildContent` (and therefore the nested `DynamicComponent`)
entirely. Fixed per ticket 75's own suggested approach - `ComponentContainer` now takes a `Props`
parameter purely for change detection (never rendered directly), compared via `Equals` (Props types
are records, so this is a cheap structural check), and `DiagramCanvas.razor` passes
`Props="instance.Props"` alongside the existing `X`/`Y`/etc. bindings. Marked ticket 75 resolved.

TDD-ish throughout: `MutateEntityCommandTests.cs` mirrors `ChangeBoundsCommandTests.cs`;
`StickyNoteTests.cs`/`TextTests.cs` gained standalone editor-mechanics tests (enter/Escape/blur, no
`ParentCanvas` cascaded); `DiagramCanvasInlineTextEditingTests.cs` (new) exercises the full stack -
one `MutateEntityCommand` per commit with working undo/redo, Escape adding zero history entries
(verified via a prior real move + a single Undo, so a wrongly-recorded phantom entry would be the one
reverted instead), a no-op blur recording nothing, the rendered text actually updating post-commit
(the ticket-75 regression), and a real `BoardJsonSerializer` round-trip of the edited text.

**Screenshot case:** `InlineTextEditingVisualTests.cs` (new) - places a Sticky Note, double-clicks
it, asserts the editor is visible and focused, screenshots the mid-edit state. Its baseline could
not be generated in this environment: even the *pre-existing* baselines fail to reproduce
pixel-for-pixel in this sandbox's podman/WSL2 container (confirmed via a control run against a clean
checkout, before any of this ticket's changes - 11-16 of 17-18 existing visual tests failed
regardless), and neither increasing the podman VM's resources (blocked - unsupported for WSL-backed
machines) nor forcing software rendering via Chromium launch flags changed the outcome. This is a
pre-existing environment limitation, not something introduced here. The test needs its `.verified.*`
baseline generated in a properly-resourced environment (real Docker Desktop or CI) per the README's
documented "Updating baselines" flow before it will pass.
