# Edge attachment to whole Group

Type: grilling
Status: open
Blocked by: 08, 09

## Question

Given the resolved connectors/edges model (08 — edges attach via ports on individual component instances or float) and the resolved selection model (09 — `Group` is a persisted entity with computed bounds), decide whether an edge can attach to a whole `Group` as a single unit (e.g. a port-like anchor on the group's computed bounding box that tracks as members move/resize), or whether edges only ever attach to individual component instances (or float), with a `Group` never itself being an attachable thing.
