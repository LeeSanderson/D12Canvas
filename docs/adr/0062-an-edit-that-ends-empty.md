# An edit that ends empty removes an instance whose type says empty means absent, and an abandoned creation leaves no history

ADR 0051 opens a new node for editing when it is created. `Text` registers `""` as its default text and an edge label is an empty `Text`, so placing a `Text` and pressing `Escape` without typing leaves an invisible instance on the board. That was possible before, and edit-on-create makes it the normal result of changing your mind. ADR 0051 named the clean answer, no history entry at all, and left it here because it needs a retraction mechanism that ADR 0051 declined to build.

## Which types

**An instance whose edit ends empty is removed only when its type says empty means absent.** Among the built-ins that is `Text`, and so every edge label. An empty `StickyNote` stays: it is a coloured card with a size and a position, and a blank one placed on purpose is a marker. `Rectangle` and `Image` do not implement `IInlineEditable` and never reach this.

Removing any type left empty was rejected because it deletes a visible object the user may have wanted. Never removing was rejected because it makes edit-on-create leave invisible litter every time a user backs out.

## The author declares it, on the registration

The canvas cannot read `Props` (ADR 0001, ADR 0003), so the type's author says what empty means. **Registration gains an optional predicate, `IsEmpty`, a `Func<TProps, bool>`.** `Text` registers `props => string.IsNullOrWhiteSpace(props.Text)`. A type with no predicate is never removed, which is why `StickyNote` registers none.

Whitespace counts as empty. A `Text` holding three spaces is as invisible as one holding nothing.

The predicate reads committed `Props`, which is what the board holds and what a save writes. It is known before any instance mounts, the same reason ADR 0051 put editability on the registration.

## Every edit end, not only creation

**The rule applies whenever an `Inline edit` ends**, whether it was opened by creation, double-press or `F2`, and to an existing edge label cleared to nothing. An existing `Text` cleared by the user is as invisible as a new one abandoned, and the ticket's references (Miro, FigJam, tldraw) delete a text object whenever its edit ends empty, not only when it is new. One rule also reads more easily than one that depends on how the edit was opened.

An instance already empty when its edit opens, a hand-built or loaded empty `Text` opened with `F2` and closed with `Escape`, is removed too. The user has now looked at it and left it empty.

An empty `Text` that no one edits is left alone. The rule runs at an edit end and nowhere else, so load and host code are untouched.

## One call per edit end

The built-in editors return early from `CommitEdit` when the buffer equals `Props.Text`, and ADR 0051 called `EndInlineEdit` only from `Escape`. Typing nothing and clicking away therefore told the canvas nothing. Clearing an existing `Text` also has to be one history entry, which the editor's own `CommitPropsChange` makes impossible once it has pushed a `MutateEntity` on its own.

**The component calls `ParentCanvas.CommitInlineEdit(InstanceId, before, after, returnFocus)` exactly once per edit end, on every route, whether or not anything changed.** `returnFocus` is true for `Escape` and false for blur, which keeps ADR 0051's distinction. It replaces both the editor's use of `CommitPropsChange` and `EndInlineEdit`. `CommitPropsChange` stays for the property panel.

The canvas decides in one place:

| Result | History |
|---|---|
| unchanged, not empty | nothing |
| changed, not empty | `MutateEntity` |
| empty, and the top of the undo stack is this instance's creation | `Retract` that creation |
| empty, otherwise | one `CompositeCommand`: the text change if any, the removal (or `ChangeEdgeLabelCommand` to null for a label), and ADR 0053's group repairs |

The last row is what clearing an existing instance records, so one `Ctrl+Z` restores the text in place and inside its group.

## `Retract`

**`CommandHistory` gains `Retract(ICommand command)`.** It acts only when `command` is still the top of the undo stack, checked by reference, which is the guard arrow-key nudge and resize already use to extend their own entries in place. It undoes the command, drops it from history, pushes nothing onto redo, and fires `Changed`. When the guard fails, because something was pushed or undone since the creation, the canvas falls back to the composite row.

So a create followed by an abandoned edit leaves history exactly as it was before the create. This is narrower than the coalescing ADR 0051 rejected: nothing is merged into anything, and the only entry touched is one still on top.

- **The whole creation goes.** A `Quick create` is one `CompositeCommand` holding the node and its edge, so the edge is retracted with it. An edge with an unresolved end has no line anyway (ADR 0061).
- **Adding an edge label** is one `ChangeEdgeLabelCommand(null → label)`, so abandoning it empty retracts that command. Labels need no separate path.
- **`Changed` fires on a retract**, so a host's autosave may write a board identical to the one before the create. That is harmless, and a `Changed` that skipped retraction would leave a host's dirty flag set after a create it can no longer see.

## Focus and selection after `Escape` removes the instance

The instance's tab stop is gone and selection is not in history (ADR 0006), so the canvas chooses. When `returnFocus` is true:

- **After a `Quick create`, focus and selection go to the source instance** if it still resolves. That is where the user was, and a keyboard chain recovers: `Ctrl`+`Arrow`, change your mind, `Escape`, and `Ctrl`+`Arrow` again in another direction.
- **After an edge label, the edge's tab stop**, which selects the edge (ADR 0054's amendment to ADR 0051, unchanged).
- **Otherwise the canvas container**, with the selection cleared, ADR 0018's focus-transfer target and ADR 0051's existing fallback.

On blur nothing new happens. Focus is already where the user put it, and ADR 0006's addendum drops the removed id from the selection when it is next read.

## How this is verified

Per ADR 0025:

- A table over the four built-ins asserts that `Text` declares `IsEmpty` and the other three do not, and that `Text`'s predicate is true for `""` and whitespace.
- History is asserted as counts: placing a `Text` and pressing `Escape` leaves the history count and `Board` as they were before the placement. The same for a `Quick create` of a `Text` cleared and abandoned, which also removes the edge, and for adding an edge label.
- An empty `StickyNote` placed and abandoned stays on the board, with one history entry.
- Clearing an existing grouped `Text` is one entry; undo restores the text and the group's membership in one step.
- If another command is pushed between the creation and the edit end, the creation stays and the removal is its own entry.
- An `Interaction probe` drives the real blur route: a `Text` placed and abandoned by clicking empty canvas is removed. This is the map's rule that a test supplying the browser's step proves only the C#.
- After `Escape` on an abandoned `Quick create`, the focused element is the source's tab stop and the selection holds the source.

## Amends, confirms

- **Amends ADR 0001's third addendum**: registration gains the optional `IsEmpty` predicate, and `CommitInlineEdit` replaces `EndInlineEdit`.
- **Amends ADR 0007**: `CommandHistory` gains `Retract`, an operation on the stack outside the closed command set.
- **Amends ADR 0051** in three places: its history section's deferral is resolved, its `Escape` section's `EndInlineEdit` becomes `CommitInlineEdit` on every route, and "an `Escape` straight after creation with no change records nothing, so that case is one entry anyway" is zero entries when the type's predicate says empty.
- **Confirms ADR 0020.** A creation still commits at release. It just does not survive an abandoned edit.
- **Confirms ADR 0053.** A removal decided here makes the same membership edits a delete does, in the same entry.
- **Confirms ADR 0006 and ADR 0054.**

## Considered and rejected

- **Removing any type left empty.** Deletes a blank sticky note placed on purpose.
- **Never removing.** Invisible instances left by every abandoned create.
- **A member on `IInlineEditable`**, such as `bool IsEmpty`. Reads component state, which is valid only while the instance is mounted, where the predicate reads `Props`.
- **The component calling a removal method itself.** Puts the create-or-existing and history policy into every author's component.
- **Removing only on an edit opened by creation.** Keeps an invisible instance when the user clears an existing one, and makes the rule depend on how the edit opened.
- **A create entry followed by a removal entry.** Two undo steps for nothing, the first of which brings back an invisible `Text`.
- **Keeping both `CommitPropsChange` and `EndInlineEdit`**, with `EndInlineEdit` on blur too and the predicate checked in `CommitPropsChange`. Two decision points for one gesture, editor-only behaviour in a method the property panel shares, and a call order every author must get right.
- **Always returning focus to the container.** Breaks a keyboard chain at the moment a user is most likely to retry.
