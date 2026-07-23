# 68 — Keyboard connector attachment

**What to build:** A keyboard user connects two component instances entirely mouse-free: from a focused instance they enter port-focus, choose a source port, initiate a connection, move to the target instance/port via the same focus mechanics, and confirm — creating exactly the edge a pointer drag would have. Creation is undoable. (ADR 0010.)

**Blocked by:** 48 (Drag port-to-port creates an edge), 63 (Tab stops + focus-follows-selection)

**Status:** ready-for-agent

- [ ] From a focused instance, ports can be focused and a source port chosen by keyboard
- [ ] The target instance and port are reachable via focus navigation; confirming creates the `Edge`
- [ ] The resulting edge is identical to one created by pointer drag (attachment, undo, persistence)
- [ ] Escape cancels a half-built connection cleanly
- [ ] bUnit coverage of the keyboard connection flow
