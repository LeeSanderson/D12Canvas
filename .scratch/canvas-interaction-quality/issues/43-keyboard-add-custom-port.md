# Keyboard route to adding a custom port

Type: grilling
Status: open

## Question

Decide how a keyboard user adds a custom port to a component instance, choosing both a side and a position along it.

Today they cannot, by any route. `AddCustomPort` has exactly one caller in the library — the border-strip double-click in `ComponentContainer.razor` — and neither ADR 0010 nor ADR 0026 mentions adding a port anywhere in the shortcut table. ADR 0010's keyboard connector attachment reaches every port that already exists (`Enter` enters port picking, arrows step the four standard ports, `Space` cycles to a custom one), so the gap is narrow and total at once: a keyboard user can *use* custom ports and can never *make* one.

Surfaced while resolving [Port affordance model](06-port-affordance-model.md), which found the gap rather than created it, and which closed the pointer half. ADR 0028 keeps the double-click and adds an **Add port here** row to the object menu, anchored on ADR 0022's stored press point — and that row is pointer-only *by construction*, not by preference: a menu opened by `Shift+F10` carries no press point, so it names no side and no fraction and the row is ineligible. Every other row ADR 0023 composed is reachable from both input paths; this is the only one that is not, which is why it is worth a ticket rather than a footnote.

What makes this harder than the rows around it is that the action takes a **continuous argument**. Every other command in the table acts on the selection as it stands. This one needs a side and a fraction along it, and the two obvious sources both fail: the viewport centre (ADR 0026's fallback for a keyboard `Paste anchor`) names no point on any border, and the current port focus names a port that already exists.

Decide:

- **Whether the keyboard picks a fraction at all**, or whether it only ever adds at a fixed set of positions — quarter points, thirds, or the midpoint of whichever half of a side is currently empty. A closed set needs no picker and is a smaller decision; it also means the two input paths can express different things, which ADR 0027 treated as a parity failure worth fixing when it found the same asymmetry over endpoint kinds.
- **Where the gesture starts.** Port picking is already a two-stage keyboard mode with a live highlight (`FocusedPortId`/`FocusedCustomPortId`), so a third stage inside it is one candidate; a chord on a selected instance is another.
- **How the position is adjusted and committed**, if it is adjustable. Arrow keys sliding a provisional port along a side would reuse `Nudge`'s repeat-key shape, and ADR 0026 already established that a keypress has no continuous motion to make a tolerance-based pull legible — which argues for a step, and therefore for the step's size being this ticket's problem too.
- **What is drawn while it is being chosen.** ADR 0028 deliberately never draws the `Border partition`, expressing it through the cursor instead, and a keyboard user has no cursor. So this is the first case on the map where the keyboard needs an affordance the pointer path decided not to render.
- **Whether the new port is placed against the partition's rules or merely near them.** ADR 0028 drops a port span below the floor and lets a standard port clip a custom one, so a keyboard-placed port can be invisible the moment it exists. Whether the picker should refuse such a position, allow it silently, or say something is the same choice ADR 0028 made for the pointer by allowing it silently.

Adds no command type — `AddCustomPortCommand` already exists and is already undoable.
