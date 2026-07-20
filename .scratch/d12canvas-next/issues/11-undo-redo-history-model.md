# Undo/redo history model

Type: grilling
Status: resolved
Blocked by: 06

## Question

Design the undo/redo history model, given the state/data model (ticket 06) resolved to mutable board entities, not immutable value records — undo/redo needs an explicit change-tracking mechanism (e.g. a command/memento pattern recording what changed and how to invert it), not free snapshotting of prior state. Decide: what's the unit of a history entry (a single field mutation, or a whole completed gesture like a drag/resize), how deep history goes, and whether it needs to distinguish a local user's changes from a future remote collaborator's.

## Answer

**Unit of a history entry**: one completed user gesture (a drag, a resize, a prop edit, a create, a delete, a group, an ungroup) — recorded on gesture commit (pointer-up/blur), never per intermediate frame. Selection changes are explicitly out of scope (ticket 09 already ruled selection out of undo/redo entirely).

**Command set** — a small closed set of primitives plus two named wrappers, not one bespoke class per user-facing action:
- `AddEntity(entity)` / `RemoveEntity(id)` — generic create/delete for any entity (component instance, group, edge); the entity object already carries its full state, so no per-type command is needed.
- `ChangeBoundsCommand(before: Bounds, after: Bounds)` — one command for both move and resize, since `Bounds` (ADR 0003) is already a single combined position+size struct and a corner-drag can change both at once. Move vs. resize is inferable after the fact (width/height unchanged ⇒ move; top-left unchanged ⇒ resize) without needing two classes.
- `MutateEntity(id, beforeProps, afterProps)` — generic swap for opaque, boxed `Props` (ADR 0001/0003); no per-component-type undo code is ever required, matching the registration contract's promise that a host app supplies `Props`, not undo logic.
- `GroupCommand` / `UngroupCommand` — named for readability, but implemented as thin wrappers over `AddEntity`/`RemoveEntity` on the `Group` entity itself. No member-instance side effects: `Group.MemberIds` is a reference list held by the group, not a back-pointer on members, so grouping/ungrouping never touches the member instances.
- `CompositeCommand` — an ordered list of child commands applied/inverted together (reverse order on undo) as one history-stack entry. Needed wherever one user gesture produces more than one primitive/named command — e.g. ticket 09's multi-select move/resize (N `ChangeBoundsCommand`s), a future delete-selection (N `RemoveEntity`s), or paste (N `AddEntity`s).

**Depth**: capped at 1000 entries, implemented as a circular buffer.

**Lifetime**: in-memory / session-scoped only, not part of the persisted envelope — ticket 07's `IBoardSerializer` format has no history field, so a reload always starts with empty undo/redo stacks.

**Collaboration non-foreclosure**: no `OriginId`/actor field is added to commands now. Undo is local-user-only by design — it only ever reverts the local user's own changes. If/when multi-user collaboration lands as its own future effort, keeping each user's undo scoped to their own changes is that effort's problem to solve (likely needs its own attribution/filtering mechanism); nothing here stubs or anticipates its shape.

**Considered and rejected:**
- Semantic per-action command classes (`MoveCommand`, `ResizeCommand`, `CreateComponentCommand`, etc.) for every operation — rejected in favor of generic primitives; `Board`'s own entity model is already flat/generic (ADR 0003), and per-type commands would duplicate apply/invert logic that a generic swap already covers, especially for opaque `Props`.
- Reserving an unused `OriginId` field now (the `SchemaVersion`-style "reserve now, use later" pattern from ADR 0004) — rejected; the user was explicit that undo stays local-only, so there's no near-term consumer for the field, and adding it would invite speculative per-user filtering logic before a second user exists.
- Unlimited history depth — rejected in favor of a bounded circular buffer; a bounded default set explicitly (1000) rather than left to grow indefinitely across long editing sessions.

Seeded ADR [0007](../../../docs/adr/0007-undo-redo-history-model.md) and new `CONTEXT.md` terms (`Command`, `Gesture`, `History`).
