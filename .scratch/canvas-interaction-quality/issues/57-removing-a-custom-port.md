# Removing a custom port

Type: grilling
Status: resolved
Blocked by:

## Question

Decide how a user removes a custom port from a component instance, by pointer and by keyboard, and what happens to edges attached to it.

Today nothing removes one. `CustomPorts.Remove` has exactly one caller, `AddCustomPortCommand.Undo`, so a misplaced port can be removed only by undoing straight away. After any other command it stays until the instance is deleted.

Surfaced while resolving [Keyboard route to adding a custom port](43-keyboard-add-custom-port.md). ADR 0050 allows clipped and dropped positions, as ADR 0028 already did for the pointer, and both lean on undo for recovery. Without a removal route that recovery expires after one more action.

Decide:

- **The route.** A menu row on a port span (ADR 0022's stored press point already names the span), a key while a custom port is highlighted in port picking, or both. The keyboard half must not depend on the port being visible, because a dropped port is still a tab stop and still in the `Space` cycle.
- **Attached edges.** An edge pinned to the port could be deleted, turned into an `Auto endpoint` on the same instance, or left floating at the port's last point. Whichever it is, undo must restore the port and its edges together.
- **Whether standard ports are excluded.** They exist on every instance and are what `Auto endpoint` resolves to. Presumably they cannot be removed, but say so.
- **Whether this needs a new command type.** `AddCustomPortCommand` exists; a removal that also changes edges may need a composite.

## Answer

Recorded as ADR 0061, worked as a grilling.

Decided:

1. **Pinned edges become `Auto endpoint`s on the same instance.** No edge is deleted. The other end is untouched, so an edge left with two auto ends on one instance draws through ADR 0027's inside-the-rect fallback. Leaving an end pinned to the missing port is ruled out: such an edge draws nothing, has no stop, and stays in every save.
2. **Standard ports cannot be removed.** They are derived, not stored, and `Auto endpoint` needs all four.
3. **Pointer:** a **Remove port** row on the object menu, shown when the opening press landed on a custom port's span, in place of Add port here. A clipped or dropped port needs a zoom-in first.
4. **Keyboard:** `Delete`/`Backspace` during port picking removes a highlighted custom port. The pick stays open on the auto stage and a source armed on that port is disarmed. With a standard port or auto highlighted it is a no-op, which closes a live hazard: today `Delete` mid-pick deletes the instance.
5. **Commands:** a new `RemoveCustomPortCommand` that restores at the original index, with ADR 0037's `ChangeEdgeEndpointCommand` per changed end, in one `CompositeCommand`. `Board` cascades nothing.
6. **Locks:** removal is unavailable on a locked instance and on a port with a locked edge pinned to it. The menu then shows neither Remove port nor Add port here.

Checked against each other before sign-off: answer 3 replaced Add port here on a custom span, so answer 6 had to say that a blocked removal does not bring it back.

Found on the way: ADR 0053 said an edge whose component was deleted "still draws from its other end". `ResolveEdgeLine` returns null when either end is unresolved and ADR 0054 already said so, so the sentence is corrected in place. Another instance of the tenth variant, one ADR stating a wrong fact about another's subject.

Amends ADR 0023 (one row) and ADR 0026 (one row), corrects ADR 0053, and updates `CONTEXT.md`'s `Port` entry. No new term.
