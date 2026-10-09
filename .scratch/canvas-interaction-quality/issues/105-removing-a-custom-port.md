# 105 — Removing a custom port

**What to build:** An end user removes a custom port from the menu or with Delete while port picking, and any edge pinned to it falls back to auto-attachment on the same shape, so removing a port never deletes or strands a connector (ADR 0061). A Remove port row appears on a custom port's span, replacing Add port here there. Delete or Backspace while port picking highlights a custom port removes it, and is a no-op on a standard port or the auto stage, which also removes the old hazard of deleting the instance mid-pick. Every end pinned to the port becomes an `Auto endpoint` on the same instance. Removal is unavailable on a locked instance or when any pinned edge is locked. It is one composite of a new `RemoveCustomPortCommand`, restoring the port at its index, plus one `ChangeEdgeEndpointCommand` per converted end. Standard ports cannot be removed. This is the last of the three new commands, so `CONTEXT.md`'s command list is corrected to what exists.

**Blocked by:** 104 (Keyboard port placement and the add-port rows)

**Status:** resolved

- [x] Right-click on a custom port's span shows Remove port; choosing it removes the port and the edge pinned to it now auto-attaches to the same shape
- [x] Ctrl+Z restores the port at its original index and re-pins the edge
- [x] Delete while picking a custom port removes it; Delete while picking Top or auto does nothing and the shape survives
- [x] The row is absent on a locked shape and when a pinned edge is locked
- [x] Command tests for the new command and the composite; bUnit for the two routes
- [x] `CONTEXT.md`'s `Command` and `Port` terms describe what shipped, with the full command list

Shipped with two choices the ticket did not spell out. Remove port needs the same target as Add port here: one selected, unlocked instance with no edge selected. The menu route also resets port picking's highlight to the auto stage and disarms a source armed on the removed port, the same as the keyboard route.

`CONTEXT.md`'s command list now names every `ICommand` in the code, so its note that the list was short of the code is gone. A new `/port-removal-demo` page backs a baseline of the Remove port menu and the probes for the real `Enter`, `Space`, `Delete` path, `Delete` and `Backspace` on auto and Top, and the real right-click on the custom port's span.
