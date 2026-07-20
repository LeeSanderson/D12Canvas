# Undo/redo history model

Type: grilling
Status: open
Blocked by: 06

## Question

Design the undo/redo history model, given the state/data model (ticket 06) resolved to mutable board entities, not immutable value records — undo/redo needs an explicit change-tracking mechanism (e.g. a command/memento pattern recording what changed and how to invert it), not free snapshotting of prior state. Decide: what's the unit of a history entry (a single field mutation, or a whole completed gesture like a drag/resize), how deep history goes, and whether it needs to distinguish a local user's changes from a future remote collaborator's.
