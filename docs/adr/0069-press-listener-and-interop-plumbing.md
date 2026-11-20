# The press listener reads the DOM, C# decides the model, and interop limits shape the plumbing between them

ADR 0018 puts press classification in the JavaScript listener and ownership in C#. ADR 0031, ADR 0038 and ADR 0047 add cancellation, board ownership and the menu verdict. A set of smaller rules sits between those decisions, in `DiagramCanvas.razor.js` and the `DiagramCanvas` partial classes. Several members also look indirect because Blazor's render model and JS interop allow nothing simpler.

**The listener reads what the DOM can tell it at press time. C# has the last word on anything the model decides, such as locks and which edits may land. `DiagramCanvas` keeps pending work by id across renders, raises events for chrome outside its tree, and gets JS objects back instead of functions.**

## The board decides the lock

The listener reads a lock from the markup, and `WithLockRead` checks it again against `Board`. A primary press on a locked entity is a press on empty canvas, so it steps out of the entered group and starts a marquee.

`SelectionTakesPresses` is true while a clone drag's copies are in the hand, so the box drawn around them takes the press whatever is locked.

## Author content and Alt

A primary press on author content belongs to the browser. The listener captures nothing and tracks nothing, so the press's moves and release never reach C#, and C# hears only the press. ADR 0036 covers the one exception, a press on a marked target that cannot take focus, where the listener prevents the default and focuses the canvas.

With `Alt` held, `altPrimaryCell` turns a primary press on author content into a press on its instance. It leaves the press with the browser when the target is editable or the instance holds a live text selection.

## PointerPress is public for interop only

`PointerPress` is what the listener hands the canvas for one press. `X` and `Y` are container-relative screen pixels. The listener converts them once, so no interop round trip stands between a press and its owner. The record is public only because Blazor requires interop entry points and their parameters to be public. The gesture classes stay internal.

## What a press reports

A primary press on a `port` or `edge-endpoint` carries an edge end. Each of its moves and its release report what lies under the pointer, because captured pointer events target the canvas whatever they are over.

Pointer events carry no click count, so `pressCountFor` counts consecutive presses within `MULTI_PRESS_WINDOW_MS` (500ms) and `MULTI_PRESS_RADIUS_PX` (5).

`velocityAt` reports screen pixels per millisecond since the pointer's previous event. Two events stamped in the same millisecond keep the last speed instead of dividing by zero.

`NativeGesture` selects the enclosing instance when the selection does not already hold it. A control inside an edge's label resolves to the edge, which is not an instance, so a native press there selects nothing.

## A release unlocks history before it acts

`OnPointerReleased` unlocks history before the gesture acts on its release. The release is then the one write the board accepts while the gesture still owns the press. The canvas drops the preview the release committed after that write.

## An inline edit that ends during a press waits for the press

ADR 0051 has the editor end every inline edit with one `CommitInlineEdit` call. ADR 0038 lets nothing but the gesture's own release write `Board` while a gesture owns the press.

A press elsewhere on the canvas is what blurs the editor, and the press can reach the canvas before the blur does. The edit then ends while that same press owns the board. Writing it would break ADR 0038, and dropping it would lose what the user typed. So when `CommitInlineEdit` arrives while `PressOwnsBoard` is true, the canvas holds the commit in `_editsEndedDuringPress`. `CommitEditsEndedDuringPress` applies it once the press ends. The edit lands after the gesture that caused the blur, so its history entry follows that gesture's entry.

A `Board` swap clears `_editsEndedDuringPress`, along with `_clicksAwaitingMeasurement`, because the held edits belong to the old board.

## The second snap pass can hand an axis back to the grid

ADR 0024 runs object snapping twice per move, the second pass from the snapped position to collect guides. In `MoveSelectionGesture`, the snap on one axis can carry the selection out of the row where the other axis found an equal-spacing match. The second pass then finds nothing to draw on that other axis. That axis drops its match and snaps to the grid, so the selection is never pulled to a match with no guide showing why.

## Transform writes

`OnZoomPanChanged` ignores a change that leaves the transform as it was, such as the container reporting the size it already had. That change moves no content, so it ends no framing flight and drops no transition.

## Rendering runs ahead of the DOM

A new group's tab stop does not exist in the DOM until the grouping render commits, so `_pendingGroupFocus` defers focus to `OnAfterRenderAsync`. `focusGroupTabStop` needs no id, because grouping leaves exactly one `.group-tab-stop` with `aria-selected="true"`.

`_pendingFocusId` holds an id, not an index, because the target's index among tab stops is unknown until the render that mounts its stop commits.

`ClickToAdd` before the container has been measured queues the click in `_clicksAwaitingMeasurement`. The viewport centre is unknown until then, and the click would otherwise land on the board origin or be lost.

`InvokeFromContextMenu` closes the menu before it runs the command. Every `OnXPressed` calls `StateHasChanged`, and the canvas must never re-render with a stale menu still open.

## Chrome outside the component tree

Blazor's automatic re-render covers only the component's own tree. Sibling chrome such as the property panel re-renders on `SelectionChanged`. Undo and redo raise it too, although history does not track selection, because an undone or redone edit can change what the panel shows for an unchanged selection.

Chrome wired to the canvas by reference, such as the minimap, learns of a new `Board` through the internal `BoardReplaced` event. A swap that leaves the viewport where it was raises no other event.

## Drag and drop

Blazor's `DragEventArgs.DataTransfer` has no `SetData` or `GetData`. ADR 0052 covers files. For a palette drag, `BeginPaletteDrag` stores the dragged type in `_pendingPaletteDragKey`, and `HandleDrop` reads it back.

An edge is not a registered component type, so the palette's Edge button has no registry key. `EdgePaletteKey` is a sentinel that goes through the same palette path, and `PlaceComponent` routes it to `PlaceConnector`.

`HandleDrop` asks for the container's rect on every drop. Scrolling or a sibling's layout change can move the container on the page without firing the resize listener, which tracks only size.

## Returning handles from JavaScript

`addResizeListener` returns a plain object with a `dispose` method, not a bare function. A JS function is not JSON-serializable, so `InvokeAsync<IJSObjectReference>` would marshal it back as null.

## Consequences

A new press role or interruption path says which side owns each of these decisions, and the lock check stays on the C# side.

Every route that ends a press runs `CommitEditsEndedDuringPress`.

Code that needs a DOM element created by the current render waits for `OnAfterRenderAsync`. Chrome outside the canvas's tree subscribes to an event instead of relying on a re-render.
