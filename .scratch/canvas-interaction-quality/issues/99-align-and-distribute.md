# 99 — Align and distribute

**What to build:** Six align actions and two distribute actions tidy a selection exactly rather than by eye (ADR 0014). Eight public methods over two private implementations: align left, centre, right, top, middle, bottom; distribute horizontally, vertically. Computed against the unexpanded top-level selection's bounds, so a selected group moves as one rigid body; thresholds of two for align and three for distribute counted unexpanded; rows hidden below threshold; zero-delta entities contribute no command and a no-op align adds no undo entry. Under snap, align rounds the target coordinate (a scalar snap, not the bounds snap) and distribute rounds the gap clamped to at least one step, pinning the first entity. "Top-level" means the entered group's direct members when one is entered. Edges in the selection are skipped. The menu's align strip is one `role="group"` row of glyph menu items. Align and distribute have no chord by design.

**Blocked by:** 89 (Composed context menu with shortcut hints)

**Status:** resolved

- [x] Align left on three shapes moves two of them to the leftmost edge in one history entry; aligning already-aligned shapes adds no entry
- [x] Aligning a selection containing a group moves the group's members together
- [x] Distribute horizontally on mixed-size shapes equalises the gaps; under snap each gap is a whole grid step
- [x] The strip appears only with two or more top-level entities and the distribute glyphs only with three or more
- [x] Inside an entered group the strip acts on the members selected
- [x] The eight methods are public on the canvas so a host can wire its own buttons
- [x] Pure C# tests for the eight computations and the snap rounding; bUnit for the strip
- [x] A baseline shows the strip in the menu; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
