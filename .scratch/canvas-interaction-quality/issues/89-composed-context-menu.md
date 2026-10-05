# 89 — Composed context menu with shortcut hints

**What to build:** The right-click menu shows only rows that apply, grouped in a stable order, opens away from the nearest container edge and dies on the next press (ADRs 0023, 0026 hints). One `Context menu` component is handed a computed menu context with two content sets split on whether the press hit an entity. Rows render by per-item eligibility, unavailable rows are hidden rather than disabled, and separators render only between rendered sections. Object menu order: Cut, Copy, Paste, Duplicate; Delete; Group, Ungroup; align strip then four flat z-order rows; Lock or Unlock; Zoom to Selection; plus the port and image rows where they apply. Canvas menu: Paste; Select All; Zoom to Fit, Zoom to 100%; Snap to Grid (checked), object snapping; Unlock All. Rows whose commands do not exist yet are added by the tickets that build them, so this ticket ships the component, the context, the two sets, and the rows that exist today plus Select All and the snap toggles. The menu flips to open away from the nearest edge and clamps only if it fits nowhere, closing the live bounds-check defect. The dismissing press is consumed in the capture phase on `document`, only inside the canvas container, so a press on the host's own buttons is never swallowed. `Shortcut hint`s render beside rows whose binding is live, from one platform boolean cached at init: Apple concatenates `⌃⌥⇧⌘` symbols, everywhere else joins words with `+`; the snap row's hint follows the host's shortcut flag.

**Blocked by:** 73 (One shortcut table behind one focus guard), 80 (Edges in the selection)

**Status:** ready-for-agent

- [ ] Right-clicking a shape shows the object set; right-clicking empty canvas shows the canvas set; no row is ever greyed out
- [ ] Ungroup is absent when nothing selected is a group and its section's separator is absent with it
- [ ] A menu opened near the bottom-right corner opens up and left with no row clipped
- [ ] A press on a host button outside the container activates the button and closes the menu; a press inside the container only closes the menu
- [ ] Every row with a live chord shows it in the platform's convention; the align strip, Unlock All and object snapping show none
- [ ] Select All, Snap to Grid and object snapping rows work from the canvas set
- [ ] A keyboard user can arrow through the rows as today
- [ ] bUnit covers eligibility, ordering, flipping and hints; the old menu tests are rewritten against the composed component
- [ ] Baselines for both sets in light and dark; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Context menu` and `Shortcut hint` terms describe what shipped
