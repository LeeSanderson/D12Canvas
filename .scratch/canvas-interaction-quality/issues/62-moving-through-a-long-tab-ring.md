# Moving through a long tab ring

Type: grilling
Status: resolved
Blocked by:

## Question

Decide whether a keyboard user gets a faster route through the board than one `Tab` per stop, and if so what it is and which keys it uses.

ADR 0054 put every edge in the ring, directly after its source's stop, which roughly doubles the ring on a `Quick create` board. ADR 0054 accepted the length as the cost of what is on the board. [Keyboard multi-select without Ctrl+Tab](55-keyboard-multi-select-without-ctrl-tab.md) has now settled what `Tab` does (ADR 0059): a plain `Tab` still selects, and inside `Additive traversal` it moves focus only. Any faster route has to work in both.

Candidates, not a complete list: spatial arrow navigation between stops, a way to skip edge stops, and jumping by group or region.

The key space is crowded. `Arrow` nudges the selection or pans when it is empty, `Shift+Arrow` coarse-nudges, `Alt+Arrow` resizes, `Ctrl+Arrow` is `Quick create` (still unmeasured on macOS, see [Chord survival on macOS and in Firefox](56-chord-survival-macos-firefox.md)), `Space` starts `Additive traversal`, and port picking and placement take `Arrow` and `Space` while active. A new chord needs a probe in a real browser before it is trusted. That is the lesson of `Ctrl+Tab`.

## Answer

Resolved by grilling on 2026-10-03. Recorded in [ADR 0060](../../../docs/adr/0060-directional-focus.md), with a new `CONTEXT.md` term, `Directional focus`.

The ring is windowed to the viewport plus overscan, so a long ring means a dense viewport. This answers that case only. Reaching off-screen stops stays with arrow pan and `Shift+1` / `Shift+2`.

- `Ctrl+Shift+Arrow` moves focus to the nearest instance or group stop in that direction. It selects under the weld, and moves focus only inside `Additive traversal`, which it does not end.
- Edges are never targets. Each stays one `Tab` after its source.
- Rule: candidates lie entirely past the origin's leading edge; a same-row or same-column candidate wins by nearest gap; otherwise smallest gap plus perpendicular offset; ties in reading order; nothing in that direction is a no-op, no wrap. Candidates are the current ring, so it never pans, and inside an entered group only members count.
- Origin: the focused stop (an edge stop measures from its source's stop, or its source point if floating). With nothing focused, the selection's box. With nothing selected, the viewport centre.
- No-op during a pointer gesture, port placement and port picking. The typing guard applies.
- The chord is unmeasured. [Ctrl+Shift+Arrow on Windows](63-ctrl-shift-arrow-on-windows.md) measures it before the build. macOS rows joined [Chord survival on macOS and in Firefox](56-chord-survival-macos-firefox.md) and ship as a doubt.

Rejected: accepting the ring length, skipping edge stops, group or region jumps, type-ahead (`AccessibleName` is per type), `Home`/`End`, wrapping, panning, and a no-op when nothing is focused.

Amends ADR 0026 (one row). Extends ADR 0010. Confirms ADRs 0054 and 0059.
