# Removing a custom port

Type: grilling
Status: open
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
