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
