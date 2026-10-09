# 104 — Keyboard port placement and the add-port rows

**What to build:** An end user adds a custom port from the context menu at the pressed point or, from the keyboard, by sliding a provisional port along the border with the arrows and pressing Enter (ADRs 0050, 0028 rows). The object menu's Add port here row is eligible when the press landed on a border span and is pointer-only by construction, because only the stored press point carries a side and a fraction. From the keyboard the row reads Add port… and enters `Port placement`: a hollow provisional port starts on the Top side at 0.25, moves by the nudge step along the side, turns at corners, crosses to the opposite side when the arrow points inward, is committed by Enter as one `AddCustomPortCommand`, and is cancelled by Escape or any focus move. Sliding writes nothing to `Board`, and every other board-writing key is a no-op while placing.

**Blocked by:** 89 (Composed context menu with shortcut hints), 92 (Border partition and ports on selection)

**Status:** resolved

- [x] Right-click on a side's resize span shows Add port here and adds a port at that fraction; right-click on the body shows no such row
- [x] Shift+F10 on a selected shape shows Add port…; choosing it shows the provisional port on the top side
- [x] Right slides it along the top; Down at the top-right corner turns it down the right side; Left from the right side crosses to the left side
- [x] Enter adds the port in one entry; Escape or Tab leaves the board untouched
- [x] Delete during placement does nothing
- [x] Escape's staging treats placement like port picking
- [x] bUnit covers the walk as a table over sides and corners; a probe proves the real Add port… key path
- [x] A baseline shows the provisional port; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Port placement` term describes what shipped

Shipped with four choices the ADR did not spell out. At a corner, an arrow along the adjacent side moves the port down that side rather than crossing to the opposite side, which is what makes `Down` at the top-right corner go down the Right side; an arrow pointing out of the shape does nothing. Both rows need a single selected, unlocked instance with no edge selected, the same eligibility as `Ctrl`+`Arrow` quick create. Add port here shows on a port span as well as a side's resize span, since both are border spans; ticket 105 replaces it with Remove port on a custom port's span. Entering placement hands focus to the instance's tab stop, because a menu opened while the canvas held focus hands focus back to the canvas, where `Enter` does not reach; a canvas focus landing ends placement only after the stop has held focus, so that hand-back does not cancel it.

The guard covers every key that writes `Board` or the selection, plus image drop and paste, which are not keys but write the board. `Ctrl`+`'` still toggles snap during placement, so the next step follows the new state.

Carry-overs closed: Escape's placement stage (84), the directional focus guard (85), the menu row as the pointer route to a custom port (92) and the quick create guard (103).

Remove port shipped with ticket 105. A custom port's span now offers Remove port in place of Add port here, and offers neither while removal is unavailable; a standard port's span and a side's resize span keep Add port here.
