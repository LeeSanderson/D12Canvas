# A keyboard user adds a custom port by sliding a provisional one along the border, entered from the menu row the pointer already has

ADR 0028 found that a keyboard user cannot add a custom port by any route, and ADR 0030 later left the pointer with exactly one, the **Add port here** row on the object menu. That row needs ADR 0022's stored press point for its side and fraction, so a menu opened by `Shift+F10` hid it. This decides the keyboard route.

## Any position, reached by stepping

The keyboard reaches any point on any side, not a fixed set of positions. A provisional port moves along the border one step per press. The step is the nudge rule from ADR 0026 and ADR 0048, measured along the side: the next grid line when `Snap-to-grid` is on, `1 / scale` when it is off, and `Shift` moves ten.

A fixed set was rejected on parity. ADR 0027 treated an endpoint kind the pointer could express and the keyboard could not as a defect. Quarter points or thirds would bring that defect back for positions. A set derived from the ports already present ("the middle of the empty half") was rejected because the same keys would then put the port somewhere different on each instance.

Reusing the nudge rule adds no constant. With snap on, which is the default since ADR 0024, a snapped instance's sides lie on grid lines, so the keyboard lands on the points a careful pointer user would aim for. At far zoom-out the dominant grid spacing can be wider than the side. Positions are clamped to the side, so a step stops at the corner.

The cost is the one nudging already has: with snap off, crossing a long side takes many presses.

## Entered from the menu row

**The Add port here row stays on the object menu and is shown when the menu was opened from the keyboard.** There it reads **Add port…** and enters a short-lived placement mode instead of adding a port at once. Eligibility is unchanged: a single selected instance.

This follows two existing decisions. ADR 0026 made the eight align commands keyboard-reachable through the menu instead of by chord, and the same reasoning applies to an action this rare. ADR 0026 also anchors a keyboard-opened paste at the viewport centre instead of the press point, so one row behaving differently by input path has a precedent. The pointer opening supplies a side and a fraction and the keyboard opening supplies neither, so the keyboard row asks for them.

Placement is a transient mode like port picking: entered by one action, left by `Enter` or `Escape`, never left on. ADR 0009's rule against persistent tool modes is unaffected.

## Moving, committing and cancelling

- **Start:** the Top side at fraction 0.25, rounded to the nearest grid line under snap. The start is arbitrary and harmless, because nothing is added without an explicit `Enter`. ADR 0027's objection to the Top seed was that it could be committed without choosing. 0.25 keeps the provisional port off the standard port's dot.
- **Arrows move the port in the arrow's direction, kept on the border.** Along a side they slide it. Pressed into a corner, the port turns onto the adjacent side and stops there, so holding `Left` on the Top side ends at the top of the Left side. An arrow pointing into the shape moves the port to the opposite side at the same fraction.
- **`Enter` commits** one `AddCustomPortCommand`, so one history entry. Sliding writes nothing to `Board` or `History`. The provisional port is mode state on `DiagramCanvas`, like `_portFocusEndpoint`, and not a `Gesture preview`, which ADR 0020 keeps away from keyboard work.
- **`Escape` cancels placement and keeps the selection.** The next `Escape` clears the selection as usual. Port picking resets everything on one `Escape`; placement is staged because it was opened from a menu on a selection the user meant to keep.
- **Focus leaving cancels.** `Tab`, a pointer press (ADR 0036 moves focus to `.diagram-canvas` on every press) and anything else that moves focus off the instance's tab stop end placement without adding a port.
- **After commit**, focus stays on the instance's tab stop. The new port is in port picking's `Space` cycle at once, so adding a port and connecting from it are consecutive.

**Inside placement only `Arrow`, `Shift+Arrow`, `Enter` and `Escape` act.** Every other guarded row in ADR 0026's table that writes `Board` or the selection is a no-op, including `Delete`, `Space`, `Ctrl+Z`, `Alt+Arrow` and `Ctrl+Arrow`. Port picking already makes `Alt+Arrow` a no-op for the same reason. Without this, `Delete` would remove the instance the port is being placed on.

**Entering placement ends an in-progress port pick but keeps an armed connector source.** A user can arm a source on one instance, add a port to another, and connect to it.

## What is drawn

The provisional port is a port dot in the existing `port-focused` style, drawn hollow. On commit it becomes an ordinary custom port.

The `Border partition` stays undrawn. ADR 0028 left it to the cursor because the cursor shows it only where the user is looking. A keyboard user is looking at the dot, and the dot's position is what they are choosing.

**Nothing is announced.** The library has no live region anywhere, and port picking already highlights a port without announcing it. Announcements belong to every keyboard mode at once, so they are left to the map as their own patch rather than invented here for one mode. Until that is decided, a screen-reader user can place and commit a port but cannot hear where it is.

## Clipped and dropped positions are allowed

A position within `N` of the standard port is clipped by ADR 0028's precedence, and below the floor the committed port drops from render and hit. Placement allows those positions without comment, as the pointer does.

Refusing them would depend on zoom, because spans are measured in screen pixels: a position that drops at 0.5× is a full port at 2×. ADR 0028 already rejected rules that make adding a port "refuse under conditions the user cannot see". A dropped port is still a tab stop and still resolves as an endpoint, so it stays in the `Space` cycle and the keyboard user loses nothing they can use. `Ctrl+Z` straight after the commit removes it.

That recovery is weaker than it looks. No route removes a custom port except undo, for the pointer as well as the keyboard: `CustomPorts.Remove` is called only from `AddCustomPortCommand.Undo`. That gap is carried as its own ticket.

## How this is verified

Per ADR 0025:

- An `Interaction probe` drives the real key path instead of calling the handlers: open the menu with `Shift+F10`, activate **Add port…**, press arrows, press `Enter`, and assert the added `PortDef`. A test that calls `OnEnterPressed` directly could pass over a key that never arrives, which is how the `Ctrl+Tab` defect stayed green.
- One commit is one history entry, whatever the number of slide presses.
- `Escape`, `Tab` and a pointer press each leave `CustomPorts` unchanged.
- The step equals the nudge step for the same zoom and snap state, asserted as a relationship.
- Pressing into a corner turns the port and stops it; the next press in the same direction leaves it in place.
- The hollow provisional dot gets a visual case.

## Amends, confirms

- **Amends ADR 0028** in one clause: the row is no longer ineligible without a press point. It enters placement instead.
- **Amends ADR 0023's Add port here row** the same way, with the keyboard label **Add port…**.
- **Amends ADR 0026** by one table row for the keys inside placement.
- **Confirms ADR 0027.** The keyboard can now express every position the pointer can.
- **Confirms ADR 0009.** Placement is transient, like port picking.
- **Confirms ADR 0020.** The provisional port is not a `Gesture preview`.

## Considered and rejected

- **A fixed set of positions** (quarters, thirds, or the middle of the largest empty stretch). Smaller to build, and it lets the two input paths express different things, which ADR 0027 called a defect.
- **A third stage inside port picking.** Port picking exists to make connections, its arrows are already spent on jumping to the standard ports, and its second `Enter` already means "arm this as the source". Adding a port there mixes two intents and needs a mode inside a mode.
- **A new chord on a selected instance.** ADR 0026 found the chord namespace nearly empty, and its rule that every selection action has a chord covers actions done about as often as grouping. Adding a port is rarer, and the menu is where a keyboard user already finds rare actions.
- **Starting at the side's midpoint.** The provisional dot would begin under the standard port's dot and be invisible.
- **Arrows that name a side, as in port picking, with another key to slide.** Consistent with port picking, but it needs two key families for one movement. "The port goes where the arrow points" covers both with one.
- **One-press `Escape` that also clears the selection,** matching port picking. It throws away the selection the menu was opened on.
- **Drawing the partition during placement.** It would warn about clipped positions, but it gives the keyboard a visual language the pointer path deliberately declined, for a case `Ctrl+Z` recovers.
- **A dimmed provisional dot when the position would drop.** Needs the span computation run against a port that does not exist yet, and gives the keyboard feedback the pointer lacks.
- **Refusing clipped or dropped positions.** The refusal would depend on zoom, and a dropped port still works from the keyboard.
- **A live region for placement alone.** The first announcement in the library should be designed for every keyboard mode, not one.
