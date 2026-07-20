# State/data model

Type: grilling
Status: open
Blocked by: 02

## Question

Design the in-memory (and eventually serializable) representation of a board's content — component instances, their registered-component-type reference and props, groups, and (per the connectors ticket) edges. It must remain collaboration-ready (amenable to a future CRDT/OT/sync layer without a rewrite) and informed by the performance findings/mitigation shape from tickets 01-02 (e.g., does it need spatial indexing or a viewport-aware structure to support virtualization?). This is the central ADR most other tickets depend on.
