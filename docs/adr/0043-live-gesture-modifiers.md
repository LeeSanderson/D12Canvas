# A modifier that changes what a gesture does is read live, and a modifier change is a move

Every pointer modifier now falls under one rule. A modifier that chooses **which** `Pointer gesture` runs, or **what is selected**, is read once, at the moment it acts. A modifier that changes **what the running gesture does** is read live on every move. While a gesture is live, a change in modifier state re-sends the last pointer position as a move, so the gesture reacts with the pointer standing still.

The ticket expected a per-modifier table with a free choice in each row. By the time it was worked, two rows were already filled. ADR 0024 made Ctrl live because on macOS a Ctrl press is not a primary press at all, and ADR 0042 made Alt live because nothing is written to `Board` before release, so a toggle costs nothing. One row was left, and the rule below is what the three rows have in common.

## The rule

- **Read once, at the moment it acts:** a modifier that decides the gesture's identity or a selection outcome. ADR 0018 already fixes identity at press. Selection outcomes act at a moment too: `Shift` appends a non-member at press and toggles a member at release (ADR 0022). Each is read when it acts and then has nothing left to do.
- **Read live:** a modifier that changes what the running gesture does. The gesture reads it from every move, and the release commits the last preview published, as ADR 0042 already states for Alt.
- **Never at press if a platform takes the press:** a modifier that is a press-time modifier on one platform and changes the button on another cannot be read at press anywhere. Ctrl is the case: Ctrl+click is the macOS secondary click (ADR 0024).

A live modifier changes what the gesture commits and never which gesture owns the press. ADR 0042 established that for Alt, and it holds for every live modifier: owner, capture, claiming button and press-anchored delta all stay the same through a toggle.

## Per modifier

| Modifier | Meaning on the pointer | Read |
|---|---|---|
| `Shift` | append at press, toggle at release (ADR 0022) | once, when it acts |
| `Shift` | `Axis lock` on a `MoveSelection` (ADR 0024) | live |
| Ctrl | suppress all snapping (ADR 0024) | live |
| Alt | `Clone drag` on a `MoveSelection` (ADR 0042) | live |
| Alt | resize from centre on a `ResizeSelection` | pending, takes this rule if it ships |

`Shift`'s two meanings act at different times and so need no special case. A `Shift` press on a non-member appends it, and the user can then let go of `Shift` and drag the enlarged selection freely, or keep holding it for a straight line. Latching `Axis lock` at press would tie those two together for the whole drag. It would also make `Shift` the only in-gesture modifier that ignores the key once the drag starts. The user cannot see which modifiers latch, so a mixed set looks like the canvas ignoring them.

## A modifier change is a move

ADR 0024 accepted that a modifier changed with the pointer still shows nothing until the next move, because "snapping has no observable effect until the pointer moves". That is not true. With snapping on, the selection is drawn at the snapped position, up to 8 screen pixels from the raw pointer, so pressing Ctrl should let go of it there and then. The other two show it more plainly: `Shift` puts the selection back on the locked axis, and Alt puts the copies under the pointer.

The gap also produced a commit the user did not intend. ADR 0042's rule that the release writes the last published preview is right, but with no move after an Alt release the last preview is still a clone, so releasing Alt and then the button commits a clone after the user let go of the key that means clone.

So **while a gesture holds pointer capture, JavaScript listens for `keydown` and `keyup`, and when the state of `Shift`, Alt or Ctrl changes it re-sends the last pointer position through `OnPointerMoved` with the new modifiers.** For the gesture this is an ordinary move.

- **No new channel.** The four invokable methods stay four. Nothing about selection or gesture kind is mirrored into JavaScript: the listener needs only the fact that capture is held, which JavaScript already owns (ADR 0018).
- **It goes through the frame coalescer** like any other move (ADR 0020), so a key pressed and released inside one frame publishes nothing.
- **Only a change re-sends.** Auto-repeat `keydown`s carry the same state and are ignored.
- **The velocity is zero**, because the pointer has not moved. ADR 0024's fast-pointer cut-off therefore never suppresses the snap a stationary user is waiting for.
- **The listener is on the window, in the capture phase**, because focus may be anywhere while the pointer is captured. It prevents nothing. Whether Alt's `keyup` needs its default prevented on Windows is ADR 0042's probe question and stays there.
- **A lost `keyup` costs nothing new.** A window blur cancels the gesture (ADR 0031), which is the only way a held key's release can go unseen.
- **No timer.** The re-send is driven by the key event, so ADR 0029's refusal of a time base is untouched.

With the re-send, ADR 0042's Alt-release case commits a move, because the screen showed a move. The rule did not change; the screen is now current.

An `Interaction probe` (ADR 0025) asserts that a modifier change with no pointer movement reaches the gesture as a move carrying the new state. That is a claim about the browser, and a bUnit test calling `OnPointerMoved` directly would supply the step the browser was meant to take.

## Undo

No live modifier here needs a mark-and-rewind. ADR 0020 writes nothing to `Board` before release, so a toggle only changes what the `Gesture preview` holds, and the release writes one command. ADR 0007 is untouched.

## Making a latched release visible

The ticket asked how a modifier latched at press and released mid-drag would be shown to the user. Nothing in a running gesture latches, so the case does not exist. The selection meanings of `Shift` have finished acting before there is anything to show.

## What this amends

**ADR 0024's reason is corrected, and its ruling stands.** Ctrl is still live, still never at press, still carried on `OnPointerMoved`. Its claim that a stationary Ctrl change has no visible effect is wrong, and the re-send replaces the gap it accepted.

**ADR 0042 is amended in two places.** A modifier change now shows at once rather than on the next move, and releasing Alt and then the button with no move between commits a move rather than a clone.

**ADR 0018 is amended by addendum**: JavaScript re-sends a move on a modifier change while capture is held, with no new invokable method.

**ADR 0022 is confirmed**: `Shift`'s selection meanings are read at the moment they act, as written.

**ADR 0007 and ADR 0020 are untouched.**

## Considered and rejected

- **Latching `Axis lock` at press**: ties `Shift`'s append to a locked drag, and makes `Shift` the one in-gesture modifier that ignores the key after the press.
- **Per-modifier answers with no rule**: every later binding, starting with Alt on a resize, would decide the question again.
- **Keeping the gap and correcting only ADR 0024's reason**: leaves a stationary toggle invisible, and leaves releasing Alt and then the button committing a clone.
- **A new invokable method for modifier changes**: carries nothing a move does not already carry.
- **Reading modifiers from `pointerup`**: commits something the user never saw, as ADR 0042 already rejected.

**Amended by ADR 0057:** the pending row is filled. Alt on a `ResizeSelection` anchors the resize at the selection's centre and is read live. A toggle recomputes from the start box, so the opposite edge jumps and the handle stays under the pointer.
