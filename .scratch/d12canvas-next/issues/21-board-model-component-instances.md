# 21 — Board model with component instances

**What to build:** The `Board` holds component instances as flat, independently-addressable entities: GUID identity assigned at creation, references by ID only, no ownership tree — keeping the model collaboration-ready. A `ComponentInstance` carries its component-type key, boxed props, bounds, and an explicit `ZIndex`. Entities are mutable. (ADR 0003.)

**Blocked by:** None — can start immediately.

**Status:** ready-for-agent

- [ ] Component instances can be added to, removed from, and looked up on a `Board` by GUID
- [ ] Bounds are tracked uniformly on every instance, structurally separate from props
- [ ] Every instance carries an explicit `ZIndex`
- [ ] No parent/child ownership exists anywhere in the model — entities reference each other only by ID
- [ ] xUnit coverage at the pure C# seam
