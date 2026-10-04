# A window blur cancels the pointer gesture and ends its press

ADR 0031 made window `blur` the third interruption channel and left one fact unmeasured: whether a real browser delivers `blur` when the user leaves the window with a button held. It also had a blur cancel follow Escape's rule, where a cancelled gesture keeps the pointer until its claiming button comes up.

**A window blur cancels the live gesture as ADR 0031 defines it, then ends the press: the canvas clears the gesture and releases capture. Escape and a `Board` swap still hold the press until release.**

## What the browser delivers

Measured by hand on Windows with a standalone probe that presses, captures and calls `preventDefault` the way ADR 0018's gestures do. Chrome 153 was logged in full. The dev ran the same attempts in Edge and Firefox and reported identical results without pasting logs.

- **`blur` fires with the button held**, both on `Alt+Tab` to another application and on `Ctrl+PgDn` to another tab. It arrived 0.6s to 1s after the press, when focus left.
- **`preventDefault` on the press makes no difference.** The reason this was in doubt is that suppressing the default suppresses focus movement. It does not suppress losing focus to another window.
- **`pointermove` with `buttons=1` can arrive after the blur.** Chrome sent one or two while the `Alt+Tab` switcher was open.
- **`pointerup` never arrives.** The release happens in the other application or tab.
- **`lostpointercapture` does fire, but late on the path that matters.** On a tab switch it fired within a millisecond of the blur. On `Alt+Tab` it fired only at the first pointer move after the user came back, together with a move carrying `buttons=0`, which can be minutes after the release.
- **`visibilitychange` caught nothing `blur` missed.** It fired on both paths, always after the blur.

WebKit and macOS are unmeasured, because neither can be driven by hand here.

## The release a cancelled gesture waits for never comes

ADR 0031 holds the press after Escape for two reasons. Releasing capture with the button still down would send the eventual `pointerup` to whatever is under the cursor, and a user who keeps dragging by reflex should get silence rather than a new gesture. Both assume the button comes up on this page.

After a blur it comes up somewhere else. Holding the press then means waiting for an event that will not be delivered, and ADR 0038 makes that wait visible: while any gesture owns the press, keys that write `Board` or the selection do nothing. A user who drags, `Alt+Tab`s away, releases, comes back and presses `Ctrl+Z` or `Delete` before touching the mouse gets nothing, with nothing on screen to say why. In Chrome that lasts until the first mouse move, and in an engine that never fires `lostpointercapture` on this path it lasts until the next press.

So the blur cancel runs ADR 0031's three steps, then clears the gesture and releases capture in that order. The order is ADR 0031's own: clearing first means the `lostpointercapture` that follows finds no gesture, and ADR 0018's net stays silent.

What this gives up is the case ADR 0031 held the press for. A user who holds the button through `Alt+Tab` and back, then releases over the canvas, sends that `pointerup` to whatever is under the cursor with no gesture to absorb it. That needs a held button through two window switches, and the cost is at most one click, so it is accepted.

## No fallback channel is added

In every measured browser `blur` arrived before any other signal, so no other listener is needed. A guard on the first move with `buttons === 0` was the fallback the ticket named, and it would only matter in an engine that does not send `blur`. In that engine the gesture stays live until `lostpointercapture` fires on return, and ADR 0018's net reverts it and logs `console.error`. That is the correct response to a leak: loud, and recoverable. Whether WebKit fires `lostpointercapture` on return is as unmeasured as whether it fires `blur`.

No `visibilitychange` listener is added either. It never fired without a `blur` before it.

## Consequences

**ADR 0031 is amended by addendum**: a blur cancel ends the press as well as the gesture's effects. Its statement that neither `pointercancel` nor `lostpointercapture` fires when the window loses focus is corrected: `lostpointercapture` fires, too late to be the channel.

**ADR 0038 is amended by addendum**: after a blur no gesture owns the press, so the keyboard works as soon as the window has focus again.

**ADR 0018 and ADR 0025 are reused, not amended.** The net, its revert and its `console.error` are unchanged. The plumbing is assertable as ADR 0025 describes: dispatch `blur` on `window` with a live gesture, then assert the preview is gone, the selection is restored, the canvas no longer has capture, and a board-writing key works.

## Considered and rejected

- **Holding the press until `lostpointercapture`, and ending a cancelled gesture there quietly**: keeps the keyboard blocked after `Alt+Tab` until the mouse moves, which in Chrome can be minutes.
- **Committing on blur**: rejected by ADR 0031, and nothing measured here changes the argument.
- **A `buttons === 0` guard on the first move**: no measured engine needs it, and where `blur` is missing the net already catches the leak.
- **A `visibilitychange` listener**: never fired without a `blur` first.
