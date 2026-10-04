# A custom port is removed from the object menu or from port picking, and the edges pinned to it become auto endpoints on the same instance

Nothing removes a custom port today except undo. `CustomPorts.Remove` has one caller, `AddCustomPortCommand.Undo`, so a misplaced port can be taken off only by undoing at once. After any other command it stays until the instance is deleted. ADR 0028 and ADR 0050 both allow clipped and dropped positions and rely on undo to recover, and that recovery expires after one more action. This decides how a port is removed, by pointer and by keyboard, and what happens to the edges on it.

## Pinned edges become auto endpoints

**Every edge end pinned to the removed port becomes an `Auto endpoint` on the same instance.** The edge still connects the same two things. It loses only the side it leaves from, and ADR 0027 then picks that side again as either end moves. Removing a port says where an edge attaches, not whether the connection exists.

The other end of each edge is not touched. When the other end is already an `Auto endpoint` on the same instance, the edge is left with two auto ends on one instance and draws through ADR 0027's fallback for an end inside the rect. That is a short stub, but it can be seen, selected and deleted.

Leaving the end pinned to the missing port is not available. `Board.ResolveEndpoint` returns null for a `CustomPortEndpoint` whose port is gone, and an edge with an unresolved end draws no line and no label and has no tab stop (ADR 0054). The edge would stay in `Board.Edges` and in every save with no route to see, select or remove it. ADR 0053 refused that state for groups.

## Standard ports cannot be removed

They exist on every instance, they are derived from `StandardPorts` rather than stored, and they are what an `Auto endpoint` resolves to. Removing one would need a stored per-instance list of missing sides, which changes ADR 0004's format, and a rule for an auto end that faces a removed side. The first section also depends on them: converting an end to auto works only because the four standard ports are always there.

## Pointer: a menu row on the port's span

**The object menu shows Remove port when the press that opened it landed on a custom port's span.** ADR 0022's stored press point names the span, the same way it names the side and fraction for **Add port here**. ADR 0023 gains one eligibility predicate.

**On a custom port's span, Remove port takes the place of Add port here.** A new port on top of an existing custom port is almost never wanted, and the `Border partition` would clip one of the two. A standard port's span and a resize span still show Add port here.

A clipped or dropped custom port has no span, so the pointer cannot reach it until the user zooms in. The `Border partition` already accepts that for connecting.

## Keyboard: Delete during port picking

**`Delete` or `Backspace` during port picking removes the highlighted port when it is a custom port.** Port picking's `Space` cycle reaches every custom port, including dropped ones, so the keyboard route does not depend on the port being visible.

- **With a standard port or the auto stage highlighted, `Delete` is a no-op.** Today nothing guards `Delete` during port picking, so ADR 0026's row applies and the instance being picked on is deleted. ADR 0050 blocked the same hazard for placement. This blocks it for picking.
- **The pick stays open after a removal**, with the highlight back on the auto stage where every pick starts (ADR 0027). The user can remove another port or go on to connect.
- **An armed connector source on the removed port is disarmed.** Otherwise the next `Enter` would connect from a port that does not exist.

Nothing on screen says `Delete` works here, and nothing is announced. That is the gap ADR 0050 already carries for every keyboard mode.

## Locks

The **Locked** rule is that no `Command` modifies a locked instance or edge.

- **On a locked instance, removal is unavailable.** The row is hidden and `Delete` in port picking is a no-op.
- **When any edge pinned to the port is locked, removal is unavailable**, the same way. Converting the edge's end changes a locked edge, and leaving it pinned is the state the first section rules out. ADR 0058 lets an operation skip a group's locked members, but an edge cannot be skipped here: it must change or dangle.

On a custom port's span where removal is unavailable, the menu shows neither Remove port nor Add port here. A user who sees no row is not told why, which is ADR 0023's hide-rather-than-disable rule as it already applies to every row a lock removes.

## Commands

**A new `RemoveCustomPortCommand`, in one `CompositeCommand` with one `ChangeEdgeEndpointCommand` per edge end that becomes auto.** One removal is one history entry, and undo restores the port and every edge end together (ADR 0007).

- `RemoveCustomPortCommand` records the port's index in `CustomPorts`, and undo inserts it at that index. That order is the `Space` cycle's order and the serialized order, so appending on undo would change both.
- `ChangeEdgeEndpointCommand` is ADR 0037's, decided there to make repositioning an endpoint undoable and not yet built. Whichever ticket builds first adds it.
- The canvas computes every write and `Board` cascades nothing, the pattern ADR 0053 used for group membership. ADR 0003's flat model holds.

## How this is verified

Per ADR 0025:

- An `Interaction probe` drives the real key path: `Enter` on an instance, `Space` to a custom port, `Delete`, and asserts the port is gone and the instance remains. A test that calls `OnDeletePressed` directly cannot prove the key arrives in port picking.
- The same path with a standard port highlighted, and with the auto stage highlighted, leaves `Board` and `History` unchanged.
- Removing a port with two edges pinned to it makes both ends `Auto endpoint`s on the same instance and leaves the other ends unchanged. One undo restores the port at its original index and both ends.
- A removed port that was the armed connector source leaves no source armed.
- The object menu opened on a custom port's span lists Remove port and not Add port here. On a standard port's span it lists Add port here and not Remove port. On a locked instance, or on a port with a locked edge pinned to it, it lists neither.
- An edge with one end on the removed port and the other an `Auto endpoint` on the same instance still has a resolvable line.

## Amends, confirms

- **Amends ADR 0023's Add port here row**: it is not shown on a custom port's span, where Remove port is shown instead when removal is available.
- **Amends ADR 0026** by one table row: during port picking, `Delete` and `Backspace` remove a highlighted custom port and are otherwise a no-op.
- **Corrects ADR 0053** in one sentence. It says an edge whose component was deleted "still draws from its other end". It does not: `ResolveEdgeLine` returns null when either end is unresolved, and ADR 0054 already states that such an edge has no line, label or stop. The dangling-endpoint precedent stands; only the description of it was wrong.
- **Confirms ADR 0027.** An `Auto endpoint` resolves among the standard ports only, and is the state an edge end falls back to.
- **Confirms ADR 0003.** `Board` gains no cascade.
- **Uses ADR 0037's `ChangeEdgeEndpointCommand`** without changing it.

## Considered and rejected

- **Deleting the pinned edges.** Destroys a connection, with its label, style and arrows, that the user did not ask to remove. Fixing a misplaced port would cost the connection.
- **Leaving the end floating at the port's last point.** Matches the drawing for one frame, then stops tracking the instance, so the next move leaves a stub.
- **Leaving the end pinned to the missing port.** An edge nobody can see, select or remove, saved forever.
- **Removing standard ports.** Needs a format change and a rule for auto ends facing a removed side, for something nobody has asked for.
- **A hover × on the port dot.** Ports are not shown on hover (ADR 0028), and the dot is deliberately smaller than its region.
- **`Delete` acting on the port under the pointer.** `Delete` acts on the selection; making it depend on where the pointer rests is a new kind of rule.
- **A Remove port… row on a keyboard-opened menu that then asks which port.** A second picker beside port picking, which already chooses one port.
- **A separate removal mode.** A mode inside a mode, which ADR 0050 rejected.
- **Removing anyway and converting a locked edge's end.** Modifies a locked edge.
- **Appending the port on undo.** Reorders the `Space` cycle and the saved file.
