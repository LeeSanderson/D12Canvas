# 104 — Keyboard port placement and the add-port rows

**What to build:** An end user adds a custom port from the context menu at the pressed point or, from the keyboard, by sliding a provisional port along the border with the arrows and pressing Enter (ADRs 0050, 0028 rows). The object menu's Add port here row is eligible when the press landed on a border span and is pointer-only by construction, because only the stored press point carries a side and a fraction. From the keyboard the row reads Add port… and enters `Port placement`: a hollow provisional port starts on the Top side at 0.25, moves by the nudge step along the side, turns at corners, crosses to the opposite side when the arrow points inward, is committed by Enter as one `AddCustomPortCommand`, and is cancelled by Escape or any focus move. Sliding writes nothing to `Board`, and every other board-writing key is a no-op while placing.

**Blocked by:** 89 (Composed context menu with shortcut hints), 92 (Border partition and ports on selection)

**Status:** ready-for-agent

- [ ] Right-click on a side's resize span shows Add port here and adds a port at that fraction; right-click on the body shows no such row
- [ ] Shift+F10 on a selected shape shows Add port…; choosing it shows the provisional port on the top side
- [ ] Right slides it along the top; Down at the top-right corner turns it down the right side; Left from the right side crosses to the left side
- [ ] Enter adds the port in one entry; Escape or Tab leaves the board untouched
- [ ] Delete during placement does nothing
- [ ] Escape's staging treats placement like port picking
- [ ] bUnit covers the walk as a table over sides and corners; a probe proves the real Add port… key path
- [ ] A baseline shows the provisional port; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Port placement` term describes what shipped
