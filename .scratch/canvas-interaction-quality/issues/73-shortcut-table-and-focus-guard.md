# 73 — One shortcut table behind one focus guard

**What to build:** A keyboard user can predict when a key reaches the canvas (ADR 0026). The window keydown listener has one early-return guard: pass when the active element is inside the canvas container, or when nothing is focused and no text selection lives outside it. `isEditableTarget` stays per row. Every binding accepts `metaKey` with `ctrlKey` and matches on `event.code`. Every row that writes `Board` or the selection also requires that no pointer gesture owns the press. Rows for features that do not exist yet (Ctrl+A, Ctrl+D, F2, Ctrl+Shift+L, Shift+1/2/0, Ctrl+Enter, Shift+F10) are added by the tickets that build them; this ticket reconciles the rows that exist and fixes three live defects:

- `Nudge` under snap moves to the next dominant grid line in the arrow's direction rather than one screen pixel, so an off-grid shape is repaired on the first press; Shift moves ten lines. With snap off the step stays one screen pixel divided by scale. The nudge anchor is the selection box's top-left and nudge never object-snaps.
- Arrow keys with nothing selected pan by `PanStep` divided by scale, so keyboard panning is screen-relative at any zoom.
- The snap-to-grid chord's host guard moves from the public method to the keydown call site, so `OnToggleSnapToGridPressed` is ungated and a host can still disable the chord.

Ctrl+Tab is not removed here; ticket 84 replaces it.

**Blocked by:** 71 (Pointer arbitration spine)

**Status:** resolved

- [x] The keydown listener has a single guard function and the table is one switch over `event.code`; a key pressed with focus on a host input outside the canvas never reaches the canvas
- [x] Every chord row accepts `metaKey` as well as `ctrlKey`
- [x] An arrow nudge with snap on lands exactly on the next grid line from an off-grid start; Shift lands ten lines on; with snap off the step is one screen pixel at any zoom
- [x] Arrow pan with an empty selection moves the viewport by the same screen distance at 0.1x and 4x
- [x] The snap chord does nothing when the host disables it, and the public toggle method still works when called directly
- [x] A board-writing row pressed while a pointer gesture owns the press does nothing; the gesture carries on
- [x] bUnit covers the table through the public entry points; the nudge step is asserted by landing on a grid line, not by a pixel value
- [x] `CONTEXT.md`'s `Nudge` term describes what shipped

## Comments

Built as specified, with one deviation. The pan row needed no change. `ZoomPanTracker`'s pan is already in screen pixels, so `PanStep` moves the viewport 50 screen pixels at every zoom. Dividing by scale, as the ticket and ADR 0026 asked, would have broken exactly the criterion the ticket states. A theory now pins the screen distance at 0.1x and 4x, and ADR 0026 has an addendum recording this.

The snap chord now lands on `OnSnapToGridChordPressed`, which checks `EnableSnapToGridShortcut` and calls the ungated `OnToggleSnapToGridPressed`. The focus guard is covered by `KeyboardFocusGuardProbes`, because bUnit cannot reach the window listener.

Review found that `PageUp` and `PageDown` had no typing guard, though ADR 0026's table marks both guarded. They now check `isEditableTarget` like the other rows. The probes press keys away from any editable element, so they still test the focus guard alone. With the guard forced open, the host-button and host-selection probes fail.

