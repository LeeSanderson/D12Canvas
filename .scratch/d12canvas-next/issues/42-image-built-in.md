# 42 — Image built-in

**What to build:** An end user places an Image component from the palette: it renders an image from a URL prop with sensible fitting, has its own default size, default props, icon, and "Basic Shapes" category — following the pattern ticket 39 established.

**Blocked by:** 39 (Rectangle built-in)

**Status:** ready-for-agent

- [ ] Image appears in the palette under "Basic Shapes" with its icon
- [ ] It registers through the public mechanism with its own props type (source URL, fit) and defaults
- [ ] A missing/unloadable source renders a graceful placeholder state rather than breaking
- [ ] Placed instances render correctly
- [ ] Screenshot case for a rendered Image
