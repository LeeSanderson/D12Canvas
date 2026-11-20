# Delete PortRef if nothing uses it

Status: ready-for-agent

`lib/D12Canvas/Model/PortRef.cs` has no callers left. Its comment, now deleted, pointed at `StartPortDrag`, which no longer exists.

## Fix

Confirm that nothing in `lib/`, `app/` or `test/` references `PortRef`, and that it isn't public API a host could depend on. Then delete it. If it is public, decide whether to keep it as API and add a test that uses it.
