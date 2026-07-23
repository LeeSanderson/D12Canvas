# 22 — Canvas renders a Board

**What to build:** An end user sees board content on screen: `DiagramCanvas` accepts a `Board` and renders each component instance's registered Razor component at its bounds on the pannable/zoomable surface, resolving each instance's component-type key through the registry. The registered `AccessibleName` and `Role` are applied as ARIA on each instance's container automatically. A Demo page seeds a board programmatically to prove the path. This is the first visible tracer through the new architecture.

**Blocked by:** 19 (Prefactor: colocated JS isolation), 20 (Component-type registration contract & registry), 21 (Board model with component instances)

**Status:** ready-for-agent

- [ ] Instances render at their bounds and remain correctly positioned through pan and zoom
- [ ] Rendering is registry-resolved: component-type key → registered component + typed props
- [ ] `AccessibleName` is auto-applied as ARIA on each instance's container; `Role` is applied
- [ ] `ZIndex` is respected in visual stacking
- [ ] A Demo page shows a programmatically seeded board
- [ ] bUnit coverage of registry-resolved markup
