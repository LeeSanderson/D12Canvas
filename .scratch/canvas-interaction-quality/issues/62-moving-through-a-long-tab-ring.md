# Moving through a long tab ring

Type: grilling
Status: open
Blocked by:

## Question

Decide whether a keyboard user gets a faster route through the board than one `Tab` per stop, and if so what it is and which keys it uses.

ADR 0054 put every edge in the ring, directly after its source's stop, which roughly doubles the ring on a `Quick create` board. ADR 0054 accepted the length as the cost of what is on the board. [Keyboard multi-select without Ctrl+Tab](55-keyboard-multi-select-without-ctrl-tab.md) has now settled what `Tab` does (ADR 0059): a plain `Tab` still selects, and inside `Additive traversal` it moves focus only. Any faster route has to work in both.

Candidates, not a complete list: spatial arrow navigation between stops, a way to skip edge stops, and jumping by group or region.

The key space is crowded. `Arrow` nudges the selection or pans when it is empty, `Shift+Arrow` coarse-nudges, `Alt+Arrow` resizes, `Ctrl+Arrow` is `Quick create` (still unmeasured on macOS, see [Chord survival on macOS and in Firefox](56-chord-survival-macos-firefox.md)), `Space` starts `Additive traversal`, and port picking and placement take `Arrow` and `Space` while active. A new chord needs a probe in a real browser before it is trusted. That is the lesson of `Ctrl+Tab`.
