# 95 — System clipboard

**What to build:** Ctrl+C, Ctrl+X and Ctrl+V use the system clipboard, so an end user can copy between boards and browser tabs, and pasting the text of a saved board file merges that board in (ADRs 0013, 0045). The payload is the board envelope verbatim as `text/plain`, recognised structurally. Ctrl+C, Ctrl+X and Ctrl+V are DOM `copy`, `cut` and `paste` listeners, not keydown rows, because the `paste` event is the only permission-free read path; the menu rows use the async clipboard API and are lost outside a secure context. A copy carries the selected instances closed over `Interior edge`s, every selected edge (an end on an uncopied instance becomes floating at its resolved position), groups recursively, and every referenced `Asset`. Every entity id regenerates across all five references, `PortDef.Id` included; asset ids never regenerate. Paste drops an edge only when an attached end names an instance that did not materialise. Cut is eligible only when its delete would remove something and carries exactly that. The `Paste anchor` is the pointer's board position over the canvas, the stored press point for a menu paste, or the viewport centre; the payload translates as a rigid body measured against its extent; an unchanged anchor cascades by +20,+20 board units and a changed anchor resets. Pasting plain text creates a text shape. Paste warnings (unknown component types) are raised as a host event. The copy fragment builder is the duplication path tickets 96, 97 and 103 reuse. The route probe on `prototype/clipboard-menu-route` proved user activation survives the interop hop; a probe here keeps that proven.

**Blocked by:** 68 (Asset storage seam), 89 (Composed context menu with shortcut hints)

**Status:** ready-for-agent

- [ ] Ctrl+C then Ctrl+V in another tab of the same app pastes the selection with fresh ids; the clipboard holds readable board JSON
- [ ] Pasting a saved board file's text merges its content at the anchor
- [ ] Two selected connected shapes copy with their edge; a lone selected edge copies, cuts and pastes; an edge to an uncopied shape pastes floating
- [ ] Pasting five times at the same spot gives five visibly cascaded copies; moving the pointer resets the cascade
- [ ] Ctrl+X on a selection removes it in one history entry and the paste restores it elsewhere
- [ ] Cut, Copy and Paste rows appear in the composed menu and work in a secure context
- [ ] Pasting plain text creates a text shape at the anchor; an image asset referenced by a copied shape travels and is deduplicated on paste
- [ ] The paste-warnings event fires with the unknown type key
- [ ] Pure C# tests for the fragment builder and id regeneration; bUnit for the anchor rules; a probe proves user activation survives to the menu row's clipboard write
- [ ] `CONTEXT.md`'s `Paste anchor` and `Interior edge` terms describe what shipped
