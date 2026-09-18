# Edit-on-create for a quick-created instance

Type: grilling
Status: open
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
