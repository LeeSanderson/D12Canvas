# 74 — MoveSelection, the gesture preview and live geometry

**What to build:** Pressing a shape selects it and dragging moves it in the same motion, and what the drag shows per frame is what its release commits (ADRs 0020, 0022, 0048 move half, 0017 LOD hit target, 0036 author-content). `MoveSelection` becomes the third gesture on the spine and takes the `instance` and `selection-bounds` roles from the old container and bounding-box handlers:

- Pressing an unselected shape selects it at once (Shift appends) and a drag past the threshold moves the whole selection. Pressing a member or the selection box leaves the selection alone until release, where a click collapses to the pressed entity (Shift toggles). A double-press on a member is routed to a click outcome that later tickets fill (enter group, inline edit).
- The active gesture publishes a `Gesture preview` once per frame: bounds overrides keyed by instance id. `Live geometry` is one read surface consulting the preview before committed state, with the live entry points for bounds, endpoint resolution and group bounds beside `Board`'s committed ones. `instance.Bounds` always means committed; windowing and content extent read committed. Attached edges read live geometry, so they follow a drag.
- `Board` is never written mid-gesture. The release writes the preview back verbatim, only entities whose previewed bounds differ produce a command, a drag back to its start or a release below the threshold leaves no history entry, and each gesture is exactly one undo step. Grid snapping runs in the per-frame tick and rounds the top-left of the selection's bounding box, never its size.
- Participants have a sticky mount for the gesture's duration, a participant's LOD state is frozen at press, and a shape dragged in from off-screen mounts and stays until release. A drag that reaches the viewport edge stops there and still commits on release, so Ctrl+Z afterwards undoes the move rather than deleting the shape.
- The LOD placeholder is a full `Hit target` with the `instance` role and is no longer skipped by the marquee, so a board zoomed out past the threshold is still reachable.
- `author-content` inferred from a natively interactive element (`input`, `textarea`, `button`, `select`, `a[href]`, `[contenteditable]`, `[tabindex]`) classifies as `Native`: the browser keeps its own focus and text selection, nothing is prevented and no capture is taken. The author's opt-in marker for a plain region is added, and a press on it prevents and transfers focus like any other. Inside an unentered group `author-content` classifies as `instance` (ticket 81 adds the entered scope).
- The container's own move path (`_isMoving`, `OnMoved`, `OnSelect`, `HandleClick`, the click-driven focus call) and `EffectiveBounds`, `_isGroupMoving` and the group-move anchor on the canvas are deleted.

**Blocked by:** 71 (Pointer arbitration spine)

**Status:** resolved

- [x] Press-to-select-and-drag works on a single shape and on a multi-selection; a member press keeps the selection until release; Shift behaves exactly as today for append and toggle
- [x] Attached edges follow the shape throughout the drag; snapping is visible during the drag and nothing jumps at release
- [x] A drag back to its starting point leaves no history entry; one drag is one undo step
- [x] A release outside the canvas or past the clipped edge commits the move and enters history
- [x] A shape dragged in from off-screen stays mounted until release; a placeholdered participant does not swap mid-gesture
- [x] A click on an LOD placeholder selects it and a marquee over it takes it
- [x] A press inside an inline editor's textarea keeps the browser's focus and text selection
- [x] The release-reliability theory gains `MoveSelection` and `Native`
- [x] Gesture-object tests over a fake context assert what `MoveSelection` publishes and commits, read through the preview; the press-to-kind table gains the `instance`, `selection-bounds` and `author-content` rows
- [x] Drag-move, multi-selection move, selection, focus-follows-selection, undo-redo and LOD tests that dispatched mouse events are rewritten at the new seams
- [x] A mid-drag visual baseline exists; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Gesture preview`, `Live geometry`, `Bounds`, `LOD placeholder` and `Snap-to-grid` terms describe what shipped

## Comments

Built as specified, with one addition. The canvas now takes pointer capture on an instance press, so the browser sends the `dblclick` to the canvas and the built-ins' own `@ondblclick="BeginEdit"` never ran. Double-click editing broke in the browser while bUnit stayed green. With the user's agreement, ticket 101's seam came forward: `IInlineEditable`, `ComponentRegistration.IsInlineEditable`, and a double-press on an addressable, mounted instance calling `BeginEdit()` through a `DynamicComponent` ref. Ticket 101 lists what it still owns.

The textarea probe drives the real sticky-note editor, opened by a double-click, rather than an injected textarea. The probe page gained a sticky note at a fixed id for it.

A press on the `selection-bounds` role carries no entity, so a click inside the box over a member keeps the selection rather than collapsing to the member. Ticket 82's hit stack owns that. Group resize is still on the legacy mouse handlers and publishes to the gesture preview directly, so Escape, blur and a Board swap do not cancel it and history is not locked during it, the same exposure `EffectiveBounds` had. Ticket 75 moves it onto the spine.

A press now focuses the canvas rather than the instance, so a clicked shape shows the `.selected` outline that the container's `:focus` rule used to hide. That moved 12 PNGs.
