# Ctrl+Shift+Arrow moves focus to the nearest stop in that direction

ADR 0054 put every edge in the tab ring and accepted the longer ring as the cost of what is on the board, noting that anything that makes a long ring cheaper helps every stop. ADR 0059 settled what `Tab` does: a plain `Tab` selects, and inside `Additive traversal` it moves focus only. This decides the faster route: **`Directional focus`, on `Ctrl+Shift+Arrow`, moves focus to the nearest instance or group stop in the arrow's direction.**

## The ring is the viewport, not the board

`OrderedTabStops` reads the windowed viewport query, so the ring holds only stops inside the viewport plus overscan, and ADR 0054 keeps that rule for edges. A long ring is a dense viewport. On a large board it is a zoomed-out one, and `Shift+1` makes the whole board the ring.

That splits the problem in two: too many stops on screen, and reaching a stop that is not on screen. This decision answers the first only. The second already has a route that ADR 0026 defends as keyboard-complete: `Escape` until the selection is empty, arrow pan, and `Shift+1` / `Shift+2`. It costs keystrokes, but it is not missing.

A 20-node `Quick create` chain has about 40 stops. Reaching the middle of it by `Tab` takes about 20 presses. Declining was weighed and rejected: the user's task is to reach one stop, and linear order was the only way to do it.

## The binding

All four arrow modifiers are spent: plain nudges or pans, `Shift` coarse-nudges, `Alt` resizes, `Ctrl` is `Quick create`. Plain arrows cannot move focus, because under the weld the focused stop is the selection, and inside `Additive traversal` the arrows nudge the selection being built.

**`Ctrl+Shift+Arrow`** is free in ADR 0026's table and pairs with `Quick create`: `Ctrl+Arrow` creates toward a direction, `Ctrl+Shift+Arrow` goes toward one. It accepts `metaKey` and matches on `event.code`, like every row.

It is unmeasured everywhere. Ticket 41 measured `Ctrl+Arrow`, not its `Shift` variants. So:

- **Windows** is measured before anything is built, by [Ctrl+Shift+Arrow on Windows](../../.scratch/canvas-interaction-quality/issues/63-ctrl-shift-arrow-on-windows.md), with the same probe page. If Windows reserves the chord, this decision reopens on the key alone. The rule, the origin and the guards below do not depend on which chord carries them.
- **macOS** ships as a documented doubt, on the same footing as `Ctrl+Arrow`. The `Ctrl+Shift` and `Cmd+Shift` rows join [Chord survival on macOS and in Firefox](../../.scratch/canvas-interaction-quality/issues/56-chord-survival-macos-firefox.md).

## What it does

**It is a focus move.** It lands focus on the target stop's element the way `focusTabStopAt` does, and the stop's own `@onfocus` decides the rest. Under the weld it selects the target. Inside `Additive traversal` it moves focus only and keeps the mode. It is not a command handoff in ADR 0036's sense, because the user picked the target, so it does not end the mode.

**Targets are instance and group stops only.** An edge stop's only geometry is the union box of its endpoints, and on a long diagonal edge that box's centre sits in empty space far from both shapes. Edges stay reachable by `Tab`, which ADR 0054 already places one press after each edge's source. The keyboard route to an edge is `Directional focus` to its source, then `Tab`.

**The candidate set is the current ring.** Only stops in the viewport plus overscan, and while a group is entered, only its direct members, as ADR 0054 already gives the ring. `Directional focus` never pans.

## The rule

Measured on stop bounds, from an origin box:

1. **Candidates** are stops whose box lies entirely past the origin's leading edge in that direction. A stop that overlaps the origin on that axis is never in that direction.
2. **Beam first.** If any candidate overlaps the origin's perpendicular extent, the same row for left and right or the same column for up and down, the nearest of those by gap wins. Snap-to-grid is on by default, so rows and columns usually line up, and the beam keeps `Ctrl+Shift+Right` on the row instead of jumping to a closer shape a row below.
3. **Otherwise** the candidate with the smallest gap plus perpendicular offset wins.
4. **Ties** go to reading order, `Y` then `X`.
5. **Nothing in that direction is a no-op.** No wrap. A linear ring wraps, but a spatial wrap sends focus across the screen with nothing explaining why.

## The origin

- **The focused stop's box.** For a focused edge stop, the box of the stop its source resolves to, which is ADR 0054's ordering anchor. For an edge whose source floats, the source point.
- **With nothing focused, the selection's box.** One selected entity's own box, or the union box of a multi-selection. A selected edge measures from its source, by the rule above. After a pointer press focus sits on `.diagram-canvas` (ADR 0036), so this is what lets a click on a shape lead straight into keyboard traversal. A pointer press also ends `Additive traversal`, so the move then selects its target and replaces any multi-selection, as a plain `Tab` does.
- **With nothing selected either, the viewport centre.** The first press on an untouched canvas lands on the stop nearest the middle in that direction.

## Guards

- The typing guard applies, as on every arrow row.
- **No-op while a pointer gesture owns the press** (ADR 0038). This holds inside `Additive traversal` too, where the move writes no selection, so the row never depends on the mode.
- **No-op during port placement** (ADR 0050), which already makes every selection-writing row a no-op.
- **No-op during port picking.** Moving away would leave the instance mid-pick. `Escape` ends the pick as its own stage (ADR 0059), and then `Ctrl+Shift+Arrow` works again.

## How this is verified

Per ADR 0025:

- **An `Interaction probe` sends real keys** in Chromium: focus a stop, press `Ctrl+Shift+Right`, assert focus and selection moved to the expected neighbour. A second case starts `Additive traversal` and asserts the selection is unchanged after the move.
- **bUnit for the rule**, as relationships: a beam candidate beats a nearer off-beam one; a stop overlapping the origin on the movement axis is never chosen; equal scores resolve in reading order; no candidate is a no-op; an edge stop is never a target; a focused edge measures from its source's stop; with nothing focused the selection's box is the origin, and with nothing selected the viewport centre; inside an entered group only members are candidates.
- **bUnit for the guards:** no-op during a gesture, port placement and port picking; the mode survives a move.
- No visual baseline moves. Nothing new is drawn: the focus indicator is ADR 0059's.

## Amends, confirms

- **Amends ADR 0026** by one table row, `Ctrl+Shift+Arrow`, carried as a doubt on macOS like `Ctrl+Arrow`.
- **Extends ADR 0010.** Reading order still governs `Tab`, and `Directional focus` is a second focus route over the same stops. The weld is unchanged: focus drives selection.
- **Confirms ADR 0054.** Edges stay in the ring, and their placement after the source is what makes them one `Tab` from a `Directional focus` target.
- **Confirms ADR 0059.** `Tab` is unchanged, and `Directional focus` behaves like `Tab` inside and outside the mode.

## Considered and rejected

- **Accepting the ring length.** ADR 0054's own position, and honest, but it leaves linear order as the only way to reach one stop among 40.
- **Making this ticket also cover off-screen reach.** ADR 0026 already defends navigation as keyboard-complete, and zoom-to-fit turns the off-screen case into the dense-viewport one.
- **Skipping edge stops in the ring.** Halves the ring on a `Quick create` board, but any version reopens ADR 0054's placement or needs an edges-only key, both of which 0054 rejected.
- **Jumping by group or region.** A group is already one stop, and "region" has no definition in the model.
- **Type-ahead by accessible name.** `AccessibleName` is per type, so it could only ever mean "next Rectangle".
- **`Home` / `End` to the first and last stop.** Free keys, but little help for a stop in the middle.
- **Edge stops as targets.** A long edge's union box puts its centre where neither end is.
- **Wrapping at the edge of the screen.** Sends focus to the far side with nothing explaining why.
- **Panning to a target outside the ring.** That is the off-screen problem this decision leaves to navigation.
- **A no-op with nothing focused**, matching ADR 0059's `Space`. Makes the key dead straight after the most common act on the canvas, a click.
- **Ending the pick and moving during port picking.** Silently abandons a half-made connection, which ADR 0059 refused for the mode.
- **Shipping before any measurement.** `Ctrl+Tab` shipped that way and was dead in Chrome and Edge.
