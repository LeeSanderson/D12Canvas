# Edit-on-create for a quick-created instance

Type: grilling
Status: resolved
Blocked by: none

## Question

Decide whether a newly created instance can be opened for editing by the canvas, and whether that is worth reopening ADR 0001.

ADR 0030 made `Quick create` a true duplicate of its source: same type, same `Props`, same size. That is what the dev chose and it is right for a chain of same-shaped nodes, but it carries a stated cost. Chain five nodes off a sticky note reading "Login" and all five read "Login" until each is edited by hand.

The obvious fix is for the new instance to open ready to type, which is what makes Miro's and FigJam's chaining feel finished rather than half-done. It is not available today, and the reason is structural rather than missing wiring. Each built-in owns its own inline editor: `StickyNote` and `Text` both hold a private `_isEditing`, a `_focusPending` and a private `BeginEdit()`, entered only by a double-click on the component itself. `DiagramCanvas` has no way to say "start editing this one", and it must not grow one that only works for built-ins, because an author's registered component is the general case.

Supplying that route means a new member on the component registration contract, which is **ADR 0001**, on this map's settled list. That is why ADR 0030 declined to take it in passing: a prototype ticket quietly containing a registration-contract decision is a failure this map has already been bitten by once, and the reopening deserves an argument of its own.

Decide:

- **Whether ADR 0001 reopens at all**, and if so whether it narrows to this one seam the way ADR 0004 did for asset storage while resolving ticket 09, or stays shut and this cost is accepted for the life of the effort. Note that the map's Out of scope section already rules two things out on exactly this ground (containers on ADR 0003, named layers on ADR 0008), so declining has precedent as well.
- **What the contract member actually is**, if it reopens. A callback the canvas invokes, a cascading parameter the component reads, or something the registration declares once rather than per-instance. `ParentCanvas` and `InstanceId` already reach every component as cascading parameters, so a third may be cheaper than an API on the registration.
- **Whether an author can decline**, and what happens when they do. Not every component has anything to edit, and `Rectangle` and `Image` are two of the four built-ins that do not.
- **Which gestures it applies to.** `Quick create` is the case that motivates it, but palette click-to-add has the same shape and ADR 0009 gives it selection and focus already. Deciding it for one gesture and not the other is how a rule becomes unreadable later.
- **What the keyboard does**, since `Quick create` also has a `Ctrl`+`Arrow` route (ADR 0030) and a chain built from the keyboard is where typing immediately matters most.
- **Whether editing on create is one history entry with the creation or two.** ADR 0007 holds a gesture is exactly one entry, and ADR 0030 already spends one `CompositeCommand`; the built-ins commit an edit on blur as a separate `MutateEntityCommand`, so an undo straight after a chained-and-typed node currently has two plausible destinations.

## Widened by ADR 0036

The question above assumes double-click-to-edit works and asks for a second route beside it. **It may have no route at all after ADR 0018**, which makes this ticket about whether inline editing survives, not only about whether it can be started early.

ADR 0018 deletes every `@onclick` and `@ondblclick` binding on board content and dispatches from the press count instead. ADR 0035 narrowed that to the three bindings it names, deliberately sparing `StickyNote`'s and `Text`'s `@ondblclick="BeginEdit"`, and gave a reason: `author-content` is the one role that skips `preventDefault`, so the browser's own `dblclick` still fires there.

That reason does not hold for the element carrying the binding. It sits on `<p class="d12-sticky-note-text">` and `<p class="d12-text">`, plain paragraphs that are neither natively interactive nor opt-in marked, so ADR 0017's walk-up passes them and classifies the press `instance` — which does `preventDefault`, and ADR 0018 states outright that anything still depending on the browser's `dblclick` afterwards needs a probe before it is trusted.

Marking the paragraph `author-content` is not the repair. That region is the whole body of the shape, and it is what a sticky note is dragged by; handing it to `Native` would make the most common object on the board immovable from its middle.

So this ticket now owns two questions that share one answer:

- **Does `BeginEdit` have any pointer route after ADR 0018**, and is that route press-count dispatch on the `instance` role rather than a surviving `@ondblclick`? Note that press count is free on `instance`: its click outcome is select and its drag is move, so a press-count-2 outcome is unassigned there, which satisfies ADR 0030's rule that three meanings fit one target only when at most one is a click.
- If it is press-count dispatch, **the canvas is the thing dispatching it**, so the canvas needs a way to say "start editing this instance" for the double-click case as well as the create case. That is the same contract member bullet 2 above is already weighing, reached by a second road, which strengthens the case for reopening ADR 0001 rather than accepting the cost.

Two consequences for the bullets above. Bullet 1's declining option gets more expensive, because declining now means leaving double-click-to-edit without a mechanism, not merely leaving a chained node reading "Login". And bullet 6's history question is unchanged but arrives sooner, since an ordinary double-click-then-type would go through whatever route this settles.

## Answer

Resolved by grilling. Recorded as [ADR 0051](../../../docs/adr/0051-the-canvas-starts-an-inline-edit.md), with `Inline edit` added to `CONTEXT.md`.

1. **ADR 0001 reopens for one seam**: the canvas can ask an instance to begin editing, and the instance tells the canvas when it has finished. `ParentCanvas` and `InstanceId` are recorded as part of the contract, which they had been by convention only.
2. **The member is `IInlineEditable { void BeginEdit(); }` on `TComponent`.** Registration records whether the type implements it, so editability is known before mount. The canvas sets `_pendingEditId` and calls `BeginEdit()` on `DynamicComponent.Instance` after the render that mounts it. The deciding fact: the instance is often not mounted when the request arrives.
3. **Declining is a silent no-op** on every route.
4. **A gesture that makes a new node begins editing; a copy does not.** `Quick create` (pointer and `Ctrl`+`Arrow`), palette click-to-add, palette drag-and-drop and adding an edge label begin editing. Paste, `Ctrl`+`D` and `Alt`-drag do not.
5. **Existing instances:** a double-press on an addressable instance calls `BeginEdit()`, and on a non-addressable one enters the group. The built-ins' `@ondblclick` goes, and so does the need for a `dblclick` probe. **`F2`** begins editing the focused instance, because `Enter` there is port picking. A locked instance is never edited.
6. **Off screen:** a create that will begin editing pans the minimum distance into view, pan only. Below the LOD cutoff the request is dropped. A request lasts one render. Editors focus with `preventScroll: true`.
7. **`Escape` commits** (reversing the built-ins' discard) and the component calls `ParentCanvas.EndInlineEdit(id)`, which returns focus to the tab stop. An edge label returns focus to the canvas container until edges have tab stops.
8. **Two history entries**, creation then edit. Nothing new is built.
9. **`BeginEdit()` selects all**, on every route.

Two findings from the session:

- **The ticket's "press-count-2 is unassigned on `instance`" was overtaken by ADR 0044**, which uses it to enter a group. That is the seventh variant on the map (a ticket's premise overtaken by a later decision). It did not change the answer: double-press goes one level deeper, entering a group first and editing second.
- **ADR 0044 line 31 described the right behaviour over a mechanism that would not produce it.** It assumed a sticky note's text classifies `author-content`. A plain `<p>` classifies `instance`. Corrected by addendum.

Surfaced [An abandoned edit on new empty content](58-abandoned-edit-on-empty-content.md).
