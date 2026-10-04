# An abandoned edit on new empty content

Type: grilling
Status: resolved
Blocked by:

## Question

Decide what happens when an `Inline edit` opened on a newly created instance ends with its content empty.

ADR 0051 makes palette placement and adding an edge label open the new instance for editing. `Text` registers `""` as its default text, and an edge label is an empty `Text`. So placing a `Text` and pressing `Escape` without typing leaves an invisible, empty instance on the board. This was possible before ADR 0051, but edit-on-create makes it the normal result of changing your mind. Miro, FigJam and tldraw all delete a text object left empty when its edit ends.

Decide:

- **Whether an empty result removes the instance at all**, and for which types. An empty `Text` is invisible; an empty sticky note is still a visible, useful object. Is the rule per type, declared by the author through `IInlineEditable`, or decided by the canvas from something it can see?
- **Whether it applies only to an edit opened by creation**, or also to an existing instance whose text the user deletes. The second reads as "delete by clearing", which no other content does.
- **What history records.** ADR 0051 made creation and edit two entries and declined to build a coalesce or a held-open creation. The clean outcome for create-then-abandon is no entry at all, which needs exactly that retraction. The alternatives are a create entry followed by a delete entry, which leaves two undo steps for nothing, or a creation left in history with an invisible result. Say which, and whether it amends ADR 0007 or ADR 0020.
- **What an edge label does**, since removing it is `ChangeEdgeLabelCommand` back to null rather than a removal from `Board`.

**Update from ADR 0053 ([A Group left referencing deleted members](48-group-referencing-deleted-members.md) resolved):** any removal decided here is a write that can break a group's membership if the instance is grouped, which the "existing instance" branch makes reachable. It must make the same membership edits a delete does, in the same history entry: remove an emptied group, and dissolve a group of one.

## Answer

Recorded as ADR 0062, worked as a grilling.

Decided:

1. **Removal is per type.** Only a type whose empty content is invisible is removed. Among the built-ins that is `Text`, and so every edge label. An empty `StickyNote` stays.
2. **The author declares it** with an optional `IsEmpty` predicate on the registration, a `Func<TProps, bool>` evaluated against committed `Props`. `Text` registers `string.IsNullOrWhiteSpace(props.Text)`, so whitespace counts as empty. No predicate means never removed.
3. **Every edit end**, not only one opened by creation: double-press, `F2`, and clearing an existing edge label all qualify. An empty `Text` that no one edits is left alone.
4. **History.** `CommandHistory.Retract(command)` undoes and drops the top entry when it is, by reference, this instance's creation, pushing nothing onto redo. Create-then-abandon leaves no entry, and a `Quick create`'s edge is retracted with its node. Otherwise the removal is one `CompositeCommand`: the text change, the removal or `ChangeEdgeLabelCommand` to null, and ADR 0053's group repairs.
5. **One call per edit end.** `CommitInlineEdit(InstanceId, before, after, returnFocus)` replaces the editor's use of `CommitPropsChange` and ADR 0051's `EndInlineEdit`, on every route including blur. The canvas decides from a four-row table.
6. **Focus after an `Escape` removal** goes to the `Quick create` source, the edge's stop for a label, or the canvas container with the selection cleared. Blur adds nothing.

Checked against each other before sign-off; no conflicts.

Found on the way: ADR 0051 rejected one entry for create-plus-edit because it needed "a coalesce-into-previous operation … outside ADR 0007's closed command set". Arrow-key nudge and resize already extend the top entry in place behind a `ReferenceEquals(_history.PeekUndo, …)` guard, so the pattern was in the code. `Retract` reuses that guard.

Amends ADR 0001 (third addendum), ADR 0007 (`Retract`) and ADR 0051 (history and `Escape` sections). Confirms ADRs 0006, 0020, 0053 and 0054. Updates `CONTEXT.md`'s `Inline edit` and `History` entries. No new term.
