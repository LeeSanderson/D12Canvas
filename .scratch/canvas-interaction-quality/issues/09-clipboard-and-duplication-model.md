# Clipboard and duplication model

Type: grilling
Status: open

## Question

Design copy, cut, paste, duplicate and select-all — the fog patch ADR 0009 explicitly parked.

ADR 0009 recorded these as out of scope with a precise reason: "none has an underlying model decided yet (clipboard format, duplicate-position semantics, 'select all' semantics under grouping), so binding a shortcut now would be speccing ahead of a decision that doesn't exist." That decision is this ticket. Nothing in the codebase implements any of it today.

Decide:

- **Clipboard format.** An internal in-memory clipboard, the system clipboard via the async Clipboard API, or both. The system clipboard buys cross-tab and cross-board paste and interoperability, and costs permissions handling, async failure paths, and a serialisation format that is necessarily a public contract. ADR 0004's board envelope is a natural starting point — decide whether a clipboard payload is a `Board` fragment in that format or something narrower.
- **What travels.** Component instances are obvious. Do edges come along when both endpoints are in the selection? When only one is? Do `Group` entities survive a round trip, and do member relationships rebuild correctly given entity IDs are GUIDs that must be regenerated on paste?
- **Paste position.** At the pointer, at the viewport centre, offset from the original, or in place when pasting into a different board. Ticket 14 of `d12canvas-next` already established a viewport-centre-plus-cascading-offset convention for click-to-add placement — decide whether paste reuses it.
- **Duplicate** as a distinct action from copy-then-paste, including its own offset convention, and whether it is one history entry.
- **Select-all semantics under grouping.** Does it select top-level entries (with a `Group` counting as one) or every component instance? ADR 0006 collapses a grouped member's selection onto the `Group`, so the naive answer conflicts with the selection model.
- **History shape.** Paste and duplicate are multi-entity gestures; ADR 0007's `CompositeCommand` exists for exactly this. Confirm no new command type is needed — `CONTEXT.md` explicitly warns against inventing one per feature.
- **Shortcut bindings**, and how they pass the existing `isEditableTarget` guard so they do not fire while inline text editing is active.

Feeds the context menu's item set (10) and keyboard parity (16).
