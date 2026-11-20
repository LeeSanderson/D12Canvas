# A command fixes its targets before it writes, records one entry, and records nothing when nothing changed

ADR 0007 sets the history model and each command's own ADR says what it does. A set of rules about how the commands reach that result lives only in `DiagramCanvas`, `CommandHistory`, the `History` commands, `Board` and `GroupRepair`: which state a command reads, how a held key coalesces, where an entry ends, and how ties and cycles resolve.

**A command reads its targets from the board as it stood before the command, skips anything locked, and writes them as one history entry. `CommandHistory.Do` applies and records in one call. A held arrow-key burst grows the top entry in place until its anchor changes. A gesture whose result equals the state before it pushes nothing.**

## Do applies and records

`CommandHistory.Do(command)` calls `command.Apply()` and then pushes the command. No method pushes a command without applying it, so a caller cannot record a mutation it forgot to make. `Do` also clears the redo stack. A new gesture abandons whatever was undone, and it can no longer be redone.

`CommandHistory.Changed` fires on every `Do`, `Undo`, `Redo` and successful `Retract`. An `Undo` or `Redo` against an empty stack changes nothing and does not fire it. The event carries no payload. The library keeps no dirty flag, and a host that wants "unsaved changes" builds it on this event.

## Undo targets what the command changed

`AddCustomPortCommand` is its own undoable gesture, beside `AddEntityCommand` and `AddEdgeCommand`. Its undo removes the port it added, matched by the `PortDef` it holds, not the last port in `ComponentInstance.CustomPorts`. More ports may have been added to the same instance since.

`ComponentInstance.CustomPorts` is a plain mutable list with no `Add` or `Remove` of its own. `AddCustomPortCommand` and `RemoveCustomPortCommand` own the undo and redo discipline around it. Code that writes to `CustomPorts` directly bypasses history.

`ParentMembership`, which `GroupCommand` and `UngroupCommand` use inside an entered group, reads the parent group by id when the edit is applied, not when the command is built. Several edits to one parent in a single `CompositeCommand` therefore each see the result of the one before. Its undo puts back the exact parent `Group` the edit replaced.

`ChangeEdgeLabelCommand` covers adding, replacing and removing an edge's label. It swaps the whole `ComponentInstance` reference on `Edge.Label`, or null, not one of its fields. Editing an existing label's own `Props`, its text, is a separate gesture. It goes through the generic `MutateEntityCommand`, which `DiagramCanvas.CommitPropsChange` reaches through `Board.FindEdgeLabel`.

## Targets are fixed before writes

`SetSelection` turns both id lists into lists before it clears either set, because callers often build the new sets from the current ones.

`Remove` works from the copied fragment's ids, so a cut removes exactly the entities it copied and skips any that have gone since. Cut from the menu removes only once the async clipboard write succeeds, and it removes what it wrote even if the selection moved while the write was in flight.

## Layering steps each member to its own next rank

ADR 0008 makes layering arithmetic-only writes to `ComponentInstance.ZIndex`, and makes group layering a bulk write that keeps the members' relative order. Bring to Front and Send to Back go through `RestackSelection`, which gives the selection consecutive distinct values above or below everything else, so their members never collapse onto one value. A selected group's members are reordered the same way as any multi-selection.

Bring Forward and Send Backward go through `ApplyZIndexChange`. It walks `ExpandedSelection()`, computes every new `ZIndex` from committed state, and only then applies the changes as one `CompositeCommand` of `ChangeZIndexCommand`s. No member sees another member's new value, so members do not leapfrog each other. Each selected leaf instance moves to its own next distinct rank, and may land tied with a neighbour's prior rank.

Take two grouped members at 2 and 4 with an untouched instance at 6. Bring Forward puts the first member at 4, tied with the second member's prior rank, and the second at 6, tied with the untouched instance. The first member still sits below the second. Send Backward mirrors this: members at 6 and 8 over an untouched instance at 4 land at 4 and 6. ADR 0055's paint order decides how tied instances draw.

`Board.ZIndexAbove(z)` and `Board.ZIndexBelow(z)` return the next distinct `ZIndex` strictly above or below `z`, skipping every instance that holds `z` itself. A step past a cluster of siblings sharing one value is therefore one step, not one swap per sibling. Both return null when nothing lies on that side, which means the instance is already at that extreme.

A member whose computed rank equals its current one is skipped. When nothing changes, no entry is pushed. Otherwise both paths record one entry, so one undo reverts every member.

## Selection commands

ADR 0013 makes `Ctrl+A` select top-level entities. It also selects every edge on the board. With a group entered it selects that group's direct members instead. It takes nothing locked, a fully locked group included.

ADR 0053 covers `CanGroupSelection` inside an entered group.

## A held key grows the top entry in place

`NudgeCommand` backs the arrow-key nudge and `ResizeStepCommand` backs `Alt+Arrow` resize. Each has an `Extend` method that grows the command already on top of the history, so one press-to-release burst of a held key is one undoable entry. ADR 0020 records this for the nudge.

`DiagramCanvas` keeps a reference to the command it pushed for the current burst. On each repeat keydown it compares `CommandHistory.PeekUndo` with that reference. `PeekUndo` is the most recently done or redone command, or null once it is undone or the stack is empty. When the two are the same object, nothing has been pushed or undone since, and the canvas calls `Extend`. Otherwise it pushes a new command. So an undo, a redo or any other command between two repeats starts a new entry. A caller holding a command reference confirms it is still the top entry before it mutates it.

`NudgeCommand` moves one or more instances, and the floating ends of selected edges, by a board-space delta. Unlike `ChangeBoundsCommand`, which holds a fixed before and after, its `Extend` adds to an accumulated delta. Adding vectors is exact.

`ResizeStepCommand` acts on one instance and grows or shrinks one dimension per press. The edge that `ResizeMath.Apply`'s direction leaves fixed is the anchor. `OnAltArrowKeyPressed` extends the active command on each repeat, and the matching keyup ends the burst, as `OnArrowKeyReleased` does for a nudge. A later press then starts a fresh entry instead of resuming one that ended.

A resize step is not exact, because `ResizeMath.Apply` applies a minimum size. Applying it to its own previous output would compound rounding and drift the anchor edge step by step. So `Extend` adds to the command's total delta and recomputes from the bounds captured when the command was created, once per call. The anchor edge stays fixed however long the burst runs, and undo restores the starting bounds. A test runs a burst against the left edge and checks that the right edge does not move.

`ResizeStepCommand.Matches` checks the instance and the direction. Toggling `Shift` mid-burst flips the anchor under ADR 0010's `Alt+Shift+Arrow` rule, and extending the old command would resize from the wrong anchor. So the canvas starts a new `ResizeStepCommand`, and drops the held one on keyup and on any change of direction or `Shift` state. Each command keeps the anchor it started with. A grow by 1 with the left edge anchored, then a shrink by 1 with the right edge anchored, nets back to the original width, and one undo takes back only the shrink.

`NextGridLine` treats a coordinate already on a grid line as on it, within `GridLineTolerance`, which absorbs floating-point drift. A snapped nudge then moves a whole spacing and does not land back where it started.

## A move that changes nothing records nothing

With `Snap-to-grid` on, a real drag of a few pixels can snap straight back to the instance's current position. That move pushes no entry. If it did, the user would press undo once and see nothing happen. A genuine move followed by a reverting one therefore undoes, in one step, to the bounds before the genuine move. Every path that commits a gesture compares the result with the state before the gesture and skips the push when they are equal.

## Props edits

`CommitPropsChange` and `CommitPropsChangeBatch` trust the caller to have skipped a no-op edit already. The batch writes one entry for the whole bulk edit.

`ResolvePropsEntity` resolves an edge's label from its edge, since a label is not in `Board.Components`. A locked instance, or the label of a locked edge, resolves to nothing, so no props edit reaches it.

## Pasted images

When the clipboard holds a bitmap, the paste handler takes it over any text beside it. For a copied picture that text is usually its address or markup, which is not worth a text shape. ADR 0013 and ADR 0052 cover what the bitmap becomes.

## One click-to-add cascade

ADR 0009 places a click-to-add at the viewport centre, offset by a cascade so repeated clicks do not stack. `ClickToAdd` keeps one counter, `_clickToAddCascadeCount`, for every palette entry, `EdgePaletteKey` included. It is global to the canvas and never resets between kinds. A click-to-add connector right after a click-to-add shape bumps the same counter, and the reverse holds too, so neither lands on top of the other. Each step offsets by `ClickToAddCascadeStep`, 20 board units. A new kind of palette entry joins the same counter.

## Edge ends pinned to one port

`Board.FindEdgeAttachedTo(endpoint)` answers whether any edge end is pinned to a given standard or custom port. `DragEdgeEndGesture` uses it to tell "pull a new edge from this port" from "carry this edge's existing end". When several edges are pinned to the same port, the press carries the first one the search finds, so the choice depends on the board's edge order and on nothing the user can see. An auto end is never found, since it names no port and the side it resolves to changes as the other end moves.

## Group repair settles children first

`GroupRepairPlan` is the repaired shape of a set of groups once some member ids stop resolving: which groups are replaced by a copy with fewer members, which are emptied, and which are dissolved and into which survivor. `GroupRepair.Plan` never touches a `Board`. Delete composes the plan into commands, and the load paths write it straight onto the board being built.

A group with an undersized child group is deferred until that child is settled. A parent settles after its children, so the survivor it reports is the one still on the board once the whole plan has run. Settling the parent first could report as its survivor a child group that the plan then dissolves.

A hand-written file can hold a membership cycle, where groups list each other as members, and deferral would then never end. `Defer` counts how often each group has been deferred. Once a group's count passes the number of groups deferred so far, roughly one pass over every group, it stops deferring and the group is processed as it stands. A board with a membership cycle still loads, with a repair that may not match what its author meant.

## Consequences

A new command that writes the board computes its targets first, skips locked entities, records one entry, and records none when nothing changed.

A host that needs "unsaved changes" listens to `CommandHistory.Changed` and keeps its own state.

Only `AddCustomPortCommand` and `RemoveCustomPortCommand` write `CustomPorts`.
