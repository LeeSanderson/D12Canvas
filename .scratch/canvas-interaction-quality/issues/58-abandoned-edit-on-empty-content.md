# An abandoned edit on new empty content

Type: grilling
Status: open
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
