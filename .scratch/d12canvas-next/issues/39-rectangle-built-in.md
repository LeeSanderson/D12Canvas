# 39 — Rectangle built-in

**What to build:** An end user opens the palette and can immediately place a Rectangle — the first built-in component type, shipped by the library but registered through the same public mechanism as any custom type. It has its own props type carrying its visual fields (fill, stroke, …) as ordinary props, a distinct default size, default props, an icon, and `Category: "Basic Shapes"`. This ticket establishes the pattern the other built-ins follow.

**Blocked by:** 22 (Canvas renders a Board), 26 (Palette lists registered types)

**Status:** ready-for-agent

- [ ] Rectangle appears in the palette under "Basic Shapes" with its icon
- [ ] It registers through the public registration mechanism — no private shortcuts
- [ ] Its visual fields are ordinary props on its own props type — no separate theming model
- [ ] Placed instances render with the default props and default size
- [ ] Screenshot case for a rendered Rectangle
