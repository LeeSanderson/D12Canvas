# Gesture cancellation and revert semantics

Type: grilling
Status: open
Blocked by: 01

## Question

Decide what cancels an in-flight gesture, and what cancelling restores.

Ticket 01 owns how a gesture *terminates* — the guaranteed release path. This ticket owns the separate question of how a gesture is *abandoned*: the user has started dragging, realises it is wrong, and wants out without committing. Release and cancel are different outcomes from the same in-flight state, and nothing in the map currently owns the second.

Ticket 04 found the current answer is close to nothing. `OnEscapePressed` clears `_isConnectingPort` and nothing else — a leaked marquee survives Escape unchanged, and so do pan, group move, group resize and instance move. No gesture reverts: every one of them either commits whatever geometry it had reached or leaves the flag set. There is no path by which a user gets back to the state before they pressed.

Decide:

- **What the cancel input is.** Escape is the obvious candidate; the reference tools in ticket 03 also treat a right-press mid-drag as a cancel, which collides with ticket 07's right-button semantics — resolve the collision rather than leaving both tickets to assume different answers.
- **Whether cancel reverts or commits.** Reverting means every gesture must hold enough pre-gesture state to restore, which is a real constraint on the arbitration model's ownership token, not a free addition afterwards.
- **How cancel interacts with the one-entry-per-gesture history rule.** ADR 0007 says a gesture is exactly one history entry; a cancelled gesture should presumably be *zero* entries rather than one that undoes itself. Ticket 03 found tldraw achieves this with a history mark taken at gesture start and rewound on cancel — confirm whether this repo's `_history` can express that.
- **Whether an interrupted gesture (focus loss, the browser stealing the pointer) is a cancel or a commit.** Ticket 04 established these paths deliver no event at all today, so whatever ticket 01 introduces to notice them has to choose one, and the choice is user-visible.
- **Whether cancel is per-gesture or uniform.** A cancelled connector drag has an obvious null result; a cancelled pan arguably should not spring the viewport back.

Ticket 04's probe harness (branch `research/gesture-leak-probe`) is the fastest way to check any proposed behaviour against the current one.
