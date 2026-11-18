# 16. Palette "…" overflow flyout

**What to build:** when a host registers more than 8 types, a "…" button at the bottom of the rail opens a flyout with the rest. The flyout groups them in one column per category, each with a heading and glyph-and-name rows. Clicking a row adds the type and dragging places it, as from the rail. Esc or a click outside closes the flyout and returns focus to the "…" button. This ticket builds the library flyout that the Property bar's chips reuse later.

See the spec's "Palette" and "Library tooltip and flyout" sections.

**Blocked by:** 15 (Palette icon rail)

**Status:** resolved

- [x] With more than 8 registered types, a divider and a "…" button sit at the bottom of the rail; with 8 or fewer the button isn't drawn
- [x] The "…" button's tooltip gives the count ("More components (14)"); its accessible name is "More components" with `aria-expanded`
- [x] The flyout opens to the right of the rail, bottom-aligned with the button, and is a `menu`
- [x] It holds only the types not on the rail, one column per category, each with a small uppercase heading and glyph-and-name rows
- [x] Picking a row adds the type and closes the flyout; rows drag onto the board like rail buttons
- [x] Opening moves focus into the flyout; Esc and an outside click close it and return focus to the "…" button
- [x] The flyout is a reusable library component the Property bar can open under a chip
- [x] bUnit: the button appears only past 8 types, the flyout holds only the overflow types grouped by category, and its roles and names
- [x] Probe: the flyout takes focus, Esc and an outside click close it and return focus, and an entry in it still places on the board
- [x] New screenshot case: the "…" flyout open with columns by category, in light and dark
- [x] The full visual suite passes with baselines updated in this commit

## Comments

- The flyout is an internal C# component, `Flyout`, rendered through `Flyout.Around(label, placementClass, opener, onRequestClose, content)` (like `Tooltip.Around`), with `wwwroot/flyout.js`. It is a `menu`. Opening focuses the first item. Up/Down/Home/End move through the items and Left/Right move between columns. Escape, Tab or a press outside the flyout and its opener asks the owner to close it. The owner places it with its own class and carries `Flyout.Rules` once in its style block.
- Focus goes back to the opener only when it was inside the flyout or had been lost. An outside press that lands on another focusable control, such as the canvas, keeps that control's focus. The return waits for the outside press to end, because the press's mousedown moves focus after .NET has already closed the flyout. That was the first full run's probe failure.
- Rows close the flyout on `dragend`, after the drop, so the drag source isn't removed mid-drag.
- The rail's Up/Down/Home/End no longer scroll the page: `Palette.razor.js` cancels the default action for those keys. "…" is the last rail stop for the arrows and End.
- `DiagramCanvas.ConnectorPaletteKey` is now the internal `EdgePaletteKey` (`"__d12-edge__"`). It was public before, and no host used it. `ConnectorPaletteEntryVisualTests` keeps its name.
- The "…" glyph is private to the Palette rather than a `Glyphs` member, since the spec's glyph table has none.
- The Demo now registers 8 more host types (Flowchart ×5, Annotations ×3), 14 in all, so every Demo Palette shows "…" with 6 in the overflow. Its Flowchart category is split between the rail and the flyout, as the display-order rule gives.
- Baselines:
  - 39 cases move (78 files), plus 2 new ones (4 files): the "…" flyout open, light and dark.
  - On every moved case only the rail changes: it grows by Process, Decision, a divider and "…".
  - The TwoThemes page and the two `RenderedPalette` cases also grow in height because the rail is taller, and TwoThemes' canvases move down with it.
  - Two multi-selection HTML baselines also reorder `aria-selected` within one element. Their PNGs differ only in single-pixel noise outside the rail.
- Still open: no probe checks where focus ends up after a pick, where the canvas may focus the new instance.
