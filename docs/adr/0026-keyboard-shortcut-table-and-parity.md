# One shortcut table behind one guard, and a keyboard route to every surface this effort added

This effort added gestures with no keyboard equivalent, changed triggers ADR 0010 assumed, and grew ADR 0009's shortcut table by six addenda across five other ADRs. This decides the reconciled table, the single guard behind it, and which of the new capabilities get chords.

## The table supersedes ADR 0009's, and three bindings enter documentation for the first time

ADR 0009's table is superseded rather than amended a seventh time. Reading the current bindings had come to require the table plus every addendum, and two addenda contradict the table's own framing — `Ctrl+C`, `Ctrl+X` and `Ctrl+V` appear as rows in a "keydown shortcut table" while the ADR 0013 addendum states they are not keydown bindings at all. ADR 0009 keeps its actual claim, which is that there are no persistent tool modes.

Every row is guarded by focus (see the next section). The **Typing** column records whether `isEditableTarget` additionally applies; the **Mechanism** column is keydown unless stated.

| Binding | Action | Typing | Mechanism | Decided by |
|---|---|---|---|---|
| `Delete` / `Backspace` | Delete the selection | guarded | | ADR 0006 |
| `Escape` | Cancel the active pointer gesture, otherwise clear the selection | | | ADR 0018 |
| `Arrow` | Nudge the selection; pan the viewport when the selection is empty | guarded | | ADR 0010, here |
| `Shift`+`Arrow` | Coarse nudge | guarded | | ADR 0010, here |
| `Alt`+`Arrow` | Resize a single instance, opposite edge anchored | guarded | | ADR 0010 |
| `Alt`+`Shift`+`Arrow` | Resize a single instance, anchor flipped | guarded | | ADR 0010 |
| `Ctrl`+`Arrow` | Quick-create and connect in that direction — **suspect on macOS, see the addendum** | guarded | | ADR 0030 |
| `Ctrl`+`Tab` | Move focus without selecting — **suspect, see below** | guarded | | ADR 0010 |
| `Space` | Toggle the focused entity's membership of the selection | guarded | | ADR 0010 |
| `Enter` | Commit a port attachment | scoped to an instance tab stop | | ADR 0010 |
| `PageUp` / `PageDown` | Zoom in / out | guarded | | here |
| `Ctrl`+`Z` / `Ctrl`+`Shift`+`Z` | Undo / redo | guarded | | ADR 0007 |
| `Ctrl`+`G` / `Ctrl`+`Shift`+`G` | Group / ungroup | guarded | | ADR 0006 |
| `Ctrl`+`]` / `Ctrl`+`Shift`+`]` | Bring forward / bring to front | guarded | | ADR 0008 |
| `Ctrl`+`[` / `Ctrl`+`Shift`+`[` | Send backward / send to back | guarded | | ADR 0008 |
| `Ctrl`+`Shift`+`L` | Lock or unlock the selection | guarded | | here |
| `Ctrl`+`'` | Toggle snap-to-grid | guarded | | ADR 0011 |
| `Ctrl`+`D` | Duplicate | guarded | | ADR 0013 |
| `Ctrl`+`A` | Select all | guarded | | ADR 0013 |
| `Shift`+`1` / `Shift`+`2` / `Shift`+`0` | Zoom to fit / to selection / to 100% | guarded | | ADR 0015 |
| `Shift`+`F10`, `ContextMenu` | Open the context menu | guarded | | here |
| `Ctrl`+`Enter` | Focus the property bar | guarded | | here, ADR 0021 |
| `Ctrl`+`C` / `Ctrl`+`X` / `Ctrl`+`V` | Copy / cut / paste | guarded | DOM `copy`/`cut`/`paste` events | ADR 0013 |

Every binding accepts `metaKey` alongside `ctrlKey` and matches on `event.code`, so it is layout-independent.

**Three rows appear in an ADR for the first time here.** `PageUp` and `PageDown` have zoomed since before ADR 0009's table existed; ADR 0015 deliberately left them to the wheel decision and ADR 0019 does not mention them, so without this table they belong to nothing. **Arrow keys pan the viewport by `PanStep` whenever the selection is empty**, which is likewise undocumented and turns out to be load-bearing — it is half of what makes navigation keyboard-complete, which is what lets the minimap carry no keyboard route at all.

Writing the pan row down exposed a defect. `PanStep` is 50 board units and is **not** divided by scale, so one press travels 200 screen pixels at 4x and five at 0.1x. Every other keyboard step in the library is screen-relative on ADR 0010's stated reasoning, that a press should not read bigger or smaller as the user zooms. **It is corrected to `PanStep / scale`.** `Shift` is ignored while panning, so `Shift`+`Arrow` coarsens a nudge and changes nothing about a pan; that asymmetry is recorded rather than changed.

## One guard, expressed once, and what it deliberately lets through

Three guard regimes were live, plus a fourth special case: no guard at all on `PageUp`, `PageDown` and `Escape`; `isEditableTarget` on nine rows; `isEditableTarget` **and** focus within the container on the eight rows ADRs 0013 and 0015 added, each of which said reconciling the difference belonged here; and a target test on `Enter`.

**The listener stays on `window` and the focus rule becomes one early return above the `switch`.** It passes when `document.activeElement` is inside `.diagram-container`, or when nothing is focused and no text selection lives outside the container. A new row inherits the guard by position rather than by remembering, which is the whole reason for putting it there rather than in eighteen places.

`isEditableTarget` stays per-row, because it genuinely varies: `Ctrl`+`]` has nothing to do with typing and `Delete` has everything to do with it. The moving-listener alternative was rejected, but it is worth recording that it would have been behaviourally identical — a `keydown` targets `activeElement`, so "activeElement is inside the container" and "the event bubbles through the container" select exactly the same events. The choice was about how the rule reads, not what it does.

**`Escape` survives the uniform rule with nothing added.** It now routes to the active pointer gesture's cancel, and ADR 0018's press-time focus transfer means a live gesture implies focus inside the container, so there is no reachable state where it needs to fire from outside.

**`Enter`'s `isComponentContainerTarget` predicate survives but is reclassified.** ADR 0018 justified the keyboard guard's narrowness by saying `Enter` on a focused palette entry must reach native activation. That justification is a consequence of the listener being window-level and the palette being outside the container — the focus rule now covers it. What is left is semantic: `OnEnterPressed` means "commit this port attachment", which is only defined on an instance tab stop. It stays as a scope, not a guard. ADR 0018's conclusion that the two target predicates differ in width is unaffected; one of its two reasons is.

**Nothing focused passes, and a live text selection outside the container does not.** Treating an untouched canvas as available reinstates a hazard ADR 0013 named precisely — a host page user selects a paragraph and presses `Ctrl`+`C` — because selecting text never moves focus off `body`. That one case is not ambiguous: the user has pointed at what they mean, on screen. So it is excluded by one more clause on the same condition, reading `window.getSelection()`.

**What is deliberately let through is the ambiguous remainder.** With nothing focused, an embedded canvas answers `PageDown` and `Ctrl`+`A` rather than the host page. That is the same trade ADR 0019 already made for the wheel, where always-capture means a host page cannot scroll while the pointer is over the canvas. Both are the embedded-in-a-scrolling-host case, deferred until it bites, and the choice is at least consistent across the two input paths rather than each guessing separately.

## The nudge step follows the grid, and stays screen-relative doing it

ADR 0024 flipped snap-to-grid on by default, so content sits on grid lines out of the box while `NudgeCommand` still stepped by one screen pixel. A single arrow press took a grid-aligned entity off the grid, for everyone rather than for the minority who had opted in.

**With snap-to-grid on, one press moves the selection to the next grid line in that direction, and `Shift` moves ten of them.** With snap off, ADR 0010's step is untouched.

**This costs ADR 0010's reasoning nothing, which is not obvious and is the reason to record it.** `SnapBounds` targets `DominantGridSpacing()` rather than a fixed 20 units — it tracks whichever of ADR 0011's layers is currently the more opaque, so the step is 20 board units near 1x and 200 further out. On screen that dominant cell is always between 6.3 and 63 pixels, because the layer split exists to keep a cell legible. ADR 0010 chose a zoom-relative step so a press would not read bigger or smaller as the user zooms; this preserves that within a bounded range instead of reversing it into a fixed board-space amount.

**`Shift` keeps exactly the meaning it already has, and lands on something visible.** Ten dominant cells is one cell of the next coarser rendered layer, because ADR 0011's stack steps by 10x. The coarse nudge therefore moves by a line on screen rather than an abstract multiple.

**Next grid line, not current position plus one spacing.** The additive version leaves an off-grid entity off-grid forever, moving it in grid-sized jumps that never land on a line, and it is asymmetric — adding a spacing and then rounding skips zero when moving left off a near-zero position. Next-line repairs the first press and is exactly one spacing thereafter. **This needs a third snap primitive**: `SnapBounds` rounds a whole `Bounds`, ADR 0014 already found that non-reusable and needed a scalar coordinate snap, and neither expresses a directional ceiling and floor.

Two consequences for implementation. **The nudge measures from whatever point snap-to-grid anchors** — currently the selection's top-left corner, which is ticket 40's to settle — so it inherits that answer rather than choosing its own. And **each press's delta now depends on where the selection currently is**, so it must be computed against live bounds rather than `NudgeCommand`'s captured `_before`; the command's accumulate-and-reapply shape handles a varying delta already.

**There is no momentary keyboard escape from grid nudging.** `Ctrl`+`Arrow` would mirror ADR 0024's suppressing `Ctrl` and is unusable: on macOS `Ctrl`+`Left`/`Right` switches Spaces and `Ctrl`+`Up` is Mission Control, intercepted above the browser, which is the same class of reservation ADR 0010 used to reject `Alt`+`Tab`. The escape is turning snap off, and that mirrors the cost ADR 0024 already accepted for a user with both toggles off.

## Nudge does not snap to objects, because align is the keyboard's better answer

**The keyboard already has a better mechanism than object snapping, which is an unusual shape for an accessibility question.** Normally the keyboard lacks something the pointer has. Here ADR 0014's eight align commands are exact where a snap is tolerance-based, named where a snap is emergent, and land as one history entry. An object-snapping nudge would be a worse version of a capability the keyboard already holds.

Three mechanical arguments point the same way and each is independently sufficient. ADR 0020 states outright that keyboard work must not reach for the `Gesture preview`, and object snapping needs candidate geometry against a provisional position. An `Alignment guide` with no owning gesture would have to appear and expire on a timer, and nothing in this library has a timer — it would be the first, for a feature nobody asked for. And object snapping is off by default, so this would serve only the opt-in user, for whom the align commands are already there.

**The decisive argument is legibility rather than mechanism.** On the pointer a snap is legible because content moves continuously and you watch it get pulled the last few pixels. A keypress has no continuous motion, so an object snap would move the selection anywhere between one grid step and a neighbour's far edge, with nothing on screen explaining why this press travelled further than the last. That is the objection ADR 0024 already made against snapping placement and paste, where it said a silent correction is worse than none.

The declined capability, stated rather than buried: a user who has enabled object snapping gets guides while dragging and none while nudging, so the two input paths visibly differ on a feature they deliberately turned on.

## Chrome enters keyboard navigation only where it is the only route

ADR 0010's tab-stop model covers entities in reading order and says nothing about chrome at all. The minimap, property bar and context menu each needed an answer, and answering them one at a time is how three inconsistent answers happen.

**A chrome surface enters keyboard navigation only if it offers a capability no other keyboard route provides. Where it does, canvas-rendered chrome is reached by a deliberate chord and host-placed chrome takes its natural tab order.**

That settles all four surfaces with no special case:

**The minimap gets nothing: no tab stop, `aria-hidden="true"`.** It fails the first rule, because every capability it has is pan and jump and navigation is already keyboard-complete without it — `Shift`+`1`/`2`/`0` for framing and arrow-key pan for movement. This is recorded as a decision rather than left as a default, because a mouse-only control is exactly what ADR 0010 set out to eliminate. The defence is that it is not a control; it is a redundant view of state that a keyboard user reaches by other means.

**The property bar gets `Ctrl`+`Enter`**, which is Miro's binding for the same move and free against the table and against the browser on a page. ADR 0021 specified the rest — roving arrow focus inside, `Escape` or `Tab` to leave, no focus trap — and deferred only the chord.

**The menu gets `Shift`+`F10` and the `ContextMenu` key.** **The palette and property panel keep their natural tab order**, which was already true; the rule now explains why rather than leaving it as an unrelated fact.

## The menu from the keyboard, and the two anchors that differ

ADR 0022 put the menu on the secondary button's sub-threshold release and noted that this left the keyboard route missing outright.

**`Shift`+`F10` and the `ContextMenu` key, both matched on `event.code`.** macOS has no keyboard context-menu convention and its keyboards have no `ContextMenu` key, so `Shift`+`F10` is the binding that serves both platforms and the dedicated key is a Windows addition rather than the primary route.

**The content set splits on whether anything is selected.** ADR 0023 splits on whether the press hit an entity; the keyboard has no press target, and the selection is the honest analogue because it is already what the object menu operates on. No new predicate appears.

**The menu draws at the selection's on-screen box**, bottom-left corner in container pixels, with ADR 0023's flip rule handling the container edge unchanged. The canvas menu has no box and draws at the viewport centre.

**The paste anchor is the viewport centre, and explicitly not ADR 0022's stored press point.** This is the part most easily got wrong by reading ADR 0023 literally: it anchors a menu paste at "the press point that opened the menu", generalised to where the user last indicated. A keyboard-opened menu has no press point, and a point stored earlier in the session names somewhere the user has since navigated away from. ADR 0013 already states the pointer-free case as the viewport centre, so this needs no new rule — only the note that the generalisation does not reach here. **The drawing position and the paste anchor therefore differ for a keyboard-opened menu**, which is correct rather than sloppy: one answers where to put a surface, the other where content lands.

**Focus returns to the tab stop that opened the menu when it closes.** Nothing does this today, because until now only a pointer opened it. Without it a keyboard user activates Delete and lands at the top of the document. Opening and dismissal otherwise need no new mechanism: `SelectionContextMenu` already autofocuses its first item and already handles `Escape` locally with propagation stopped, so the board selection survives a dismissal.

**This closes a hole in ADR 0017 rather than only adding a route.** That ADR named the property panel as the keyboard unlock route for a locked entity, and the panel is host-placed and optional — ticket 08 found `BoardEditor` mounts none. So before this binding, a keyboard user on a host without a panel had no unlock route at all, which is the locked-forever failure ADR 0017 wrote its participation-versus-reachability rule to prevent. The menu route is library-guaranteed and needs no host cooperation.

## What gets a chord, and the shape of the table decides it

**Lock and unlock get `Ctrl`+`Shift`+`L`.** The argument is the table's own shape rather than reference-tool consensus. Sorted by category it comes out clean: **every action on a selection has a chord** — Delete, group and ungroup, the four z-order commands, duplicate. The rows without chords are **preferences** and **one family of eight**. Locking is an action on a selection and would be the only one in that category without a chord. One binding covers both directions, matching ADR 0023's single row that reads Unlock on a locked single selection and Lock otherwise, so the hint is the same string either way.

ADR 0024's "reachable rather than fast" reasoning does not transfer, and the reason is worth recording: it was an argument about a global preference set once, and locking is a per-selection operation performed about as often as grouping.

**ADR 0014's eight align and distribute actions get no chords.** With the menu keyboard-openable they are already operable without a mouse — `Shift`+`F10`, arrow to the strip, arrow across, `Enter` — and ADR 0010's goal is that the board be operable without a mouse, not that everything have a chord. ADR 0023 chose the strip partly because its keyboard cost is small, and that reasoning pays off here rather than needing supplementing. Three further reasons: eight is past the three-to-five chords people hold per application, so most would be dead weight in the table and the hint column; ADR 0023's own argument that a glyph is a diagram of the action in a way a word is not applies more strongly to a letter; and **Figma's set is not safely available in a browser** — `Alt`+`D` focuses the address bar in Chrome, Edge and Firefox, and `Alt`+`V` and `Alt`+`H` reach the menu bar in Firefox on Windows and Linux, so the obvious candidate set needs a real-browser probe before it could be believed. Figma runs a desktop app where the question does not arise.

This amends ADR 0014 in one phrase: the commands are surfaced in the menu and reachable by keyboard **through it**, rather than by keyboard directly. The commands themselves are untouched — they were built as public methods with no chrome of their own precisely so their surfaces could be decided later.

**Nothing else gains a chord.** Unlock All is a recovery action reached from the canvas menu and invisible on a board with nothing locked, so a chord would advertise a key that usually does nothing. Object snapping declined one in ADR 0024. `WheelDeviceProfile` is a host parameter in the same category.

## Hints, and one platform fact with two consumers

ADR 0023 shipped the menu with no shortcut hints on any row and handed both halves of the problem here.

**Every row whose binding is live shows a hint; every other row shows nothing.** ADR 0024 set that precedent when it declined a chord for the object-snapping toggle and said the row should show no hint rather than invent one. With the align strip taking no chords, it shows none either, so ADR 0023's claim that the strip costs nothing here holds literally.

**The platform fact is one boolean cached at init, with two consumers.** ADR 0022 flagged needing a platform check for a pointer modifier and ADR 0024 built it, for the macOS `Ctrl`+click secondary press. Hint rendering is the second consumer, so it rides on the existing init round trip as one extra field and C# renders every hint without an interop call per row.

Three things recorded rather than left to be rediscovered:

**There is no clean API for the detection.** `navigator.platform` is deprecated and `navigator.userAgentData.platform` is Chromium-only, so the answer is the latter with a regex fallback to the former. Writing that down stops the next reader hunting for an API that does not exist.

**A hint is two rendering conventions, not one string with a substituted modifier name.** Apple platforms concatenate symbols with no separator in a fixed order, `⌃⌥⇧⌘`, so `Ctrl`+`Shift`+`Z` renders as `⇧⌘Z`. Windows and Linux join words with `+`. Treating this as interpolation produces `⌘+Shift+Z`, which is wrong on both platforms at once.

**Hint eligibility follows the enabling flag, not the binding.** `EnableSnapToGridShortcut` lets a host disable `Ctrl`+`'` while the menu row keeps working — ADR 0023 moved that guard to the keydown call site for exactly that reason. So the Snap to Grid row shows its hint only while the chord is live. That is the one place hint rendering is not a pure function of the table.

## `Ctrl`+`Tab` is recorded as suspect rather than rebound

ADR 0010 rejected `Shift`+`Tab` and `Alt`+`Tab` because both are captured by browser or OS convention before a page-level handler would see them, and chose `Ctrl`+`Tab`. **`Ctrl`+`Tab` is the tab-switching chord in Chrome, Edge and Firefox**, handled above the page, and because every binding accepts `metaKey` the Mac reading of the row is `Cmd`+`Tab`, the OS application switcher, which never reaches the browser at all. The rejection ADR 0010 wrote appears to apply to its own choice.

**The existing tests cannot see it.** `DiagramCanvasCtrlTabSpaceMultiSelectTests` calls `OnCtrlTabPressed()` directly, so it proves the C# is right and says nothing about whether the chord arrives. That is ADR 0025's plumbing-not-magnitudes rule pointing at a hole rather than confirming a fix, and structurally it is ticket 04's finding again — a large, green suite blind to whether the input path exists.

**The row is carried with the doubt attached and is not rebound here.** Rebinding blind would repeat the original mistake, and there is no confidently free replacement: `Ctrl`+`Shift`+`Tab` is the reverse tab switch, `Ctrl`+`Arrow` is Spaces and Mission Control on macOS, `Ctrl`+`Space` is IME switching on Windows, and the `F6` family is browser chrome. If none is available, the fix is not a key change but reopening ADR 0010's decision to weld focus to selection — "move focus without selecting" needs a chord only because selection follows focus by default. That is a design question and must not be answered as a footnote to a table.

**Verification is a `task` and not a probe, and the reason bounds ADR 0025.** The observable is whether the *browser* switches tabs, which is invisible from inside the page: Playwright drives the page, not the browser UI, so a probe would confirm the handler fires and miss the failure entirely. ADR 0025 bounded Playwright's reach at device physics; this is a second boundary, browser-chrome-level bindings, and it needs a human pressing keys in Chrome, Firefox and Safari on both platforms.

## Two questions this does not answer

**The port affordance model's keyboard consequence stays with that decision.** Ticket 06 still changes what affordances exist, so this cannot answer whether ports become visible on focus. What it hands over is a constraint rather than a preference: **ports must be reachable and visible without hover**, because ADR 0010's mouse-free attachment already depends on it and the code is live — `Enter` picks a port, arrows jump to the four standard ones, `Space` reaches a custom one.

**Press-to-drag has no keyboard consequence at all.** It is purely a pointer concern. Two notes worth keeping: ADR 0022's additive marquee already has its keyboard analogue in `Ctrl`+`Tab` and `Space`, and ADR 0022's rejection of a held-space pan quasimode is consistent rather than lucky — `Space` was never available, being the multi-select toggle.

## What this amends and confirms

**ADR 0009's shortcut table is superseded.** Its claim that there are no persistent tool modes is untouched and is what the ADR is for.

**ADR 0010 is amended in four places**: the nudge step under snap-to-grid, the `PanStep` correction, chrome's absence from the tab-stop model now filled by the two rules above, and the doubt recorded against `Ctrl`+`Tab`.

**ADR 0014 is amended in one phrase** — surfaced by keyboard becomes reachable by keyboard through the menu.

**ADR 0017's keyboard unlock route gains the menu**, which turns a host-dependent route into a library-guaranteed one.

**ADRs 0011, 0013, 0015, 0018, 0020, 0021, 0022, 0023 and 0024 are confirmed.** Three resolve questions they explicitly routed here: ADR 0024's "keyboard nudge gets nothing" becomes the grid step yes and object snapping no; ADR 0023's no-hints-on-any-row becomes hints on live bindings; ADR 0021's deferred landmark chord becomes `Ctrl`+`Enter`. ADR 0018's two-target-predicate conclusion stands with one of its two reasons reclassified. ADR 0025 gains a second boundary on Playwright's reach.

## Considered and rejected

- **Moving the keydown listener to `.diagram-container`** — makes the focus rule structural rather than a test and deletes the `Enter` target predicate outright, but the rule would then live in the attachment rather than in the code someone reads, and the behaviour is identical either way.
- **A permissive focus rule with no text-selection clause** — three lines cheaper and reinstates the exact `Ctrl`+`C` hazard ADR 0013 introduced the strict guard for.
- **A strict focus rule where nothing focused fails** — closes the embedded-host cases, and leaves a freshly loaded canvas dead to the keyboard until first contact.
- **Per-row focus guards** — each row self-describing, at the cost of eighteen copies of one test and a nineteenth row that omits it silently.
- **Leaving the nudge step alone** — ships a default where the grid and the keyboard disagree, which is the reported defect.
- **Keeping the one-pixel step and snapping the result** — makes the arrow key silently do nothing at every zoom below 1x, where a dominant cell is wider than a pixel.
- **A fixed board-space nudge step of 20 units** — the obvious reading of "the nudge step becomes the grid size", and it does reverse ADR 0010's zoom-relative reasoning, because it ignores that snapping already tracks the dominant layer.
- **`Ctrl`+`Arrow` as a momentary grid escape** — coherent with ADR 0024's suppressing `Ctrl` and dead on macOS, where the OS takes it for Spaces and Mission Control.
- **Object snapping on nudge** — needs a timer, a guide with no owning gesture, and either breaking ADR 0020's preview rule or working around it, to deliver a worse version of the align commands.
- **Figma's eight `Alt`+letter align chords** — the one convention users have met, and at least three of the eight are browser-level bindings, so it needs a probe before it could be trusted.
- **One chord that opens the menu with the align strip focused** — spends one from the budget instead of eight and reaches all eight in three keystrokes, but invents a pattern no reference tool has and still needs a letter from the same contested namespace.
- **A chord for Unlock All** — advertises a key that does nothing on the ordinary board, since the row is invisible whenever nothing is locked.
- **`Ctrl`+`F6` for the property bar**, as Figma uses — the whole `F6` family is browser chrome.
- **A tab stop for the minimap** — puts a redundant view in the middle of reading-order traversal, between one entity and the next.
- **Rebinding `Ctrl`+`Tab` now** — every candidate is reserved somewhere, and picking one blind is the mistake ADR 0010 already made.
- **Treating `Ctrl`+`Tab` as out of scope** because ADR 0010 predates this effort — this decision supersedes the table, so shipping a row believed dead without saying so is the failure this map keeps naming.
- **Shipping no menu hints** — leaves the menu as the place shortcuts go to be forgotten, which is ADR 0023's own phrase for the outcome it was avoiding.

## Addendum (surfaced while resolving the create-adjacent-and-connect ticket)

**`Ctrl`+`Arrow`, quick-create and connect in the arrow's direction** (ADR 0030), guarded like every other row. It is written **into the table above** rather than left to be discovered here, because a table that grew by six addenda across five ADRs is the specific failure this decision exists to end, and adding a row by addendum would repeat it on the first opportunity. This addendum carries only the reasoning. It reaches the four standard ports only, which is not a parity gap but ADR 0027's existing split, where the pointer distinguishes by where you release and the keyboard by how far you drill.

The row completes a set rather than squeezing into one. A plain arrow nudges, `Shift` coarse-nudges, `Alt` resizes, `Ctrl` creates: four modifiers over the arrows, four meanings, no collision. It is also Excalidraw's own binding for the same gesture.

**It ships as a documented doubt on macOS, the second on this table.** Bindings accept `(ctrlKey || metaKey)`, and macOS has reserved both readings: `Cmd`+`Left` and `Cmd`+`Right` are back and forward in Chrome and Safari, while `Ctrl`+`Left`, `Ctrl`+`Right` and `Ctrl`+`Up` are Mission Control at the operating-system level, which outranks the browser. Excalidraw ships the chord regardless, so its own binding is at best half-working there.

Rather than opening a second investigation, [Whether `Ctrl+Tab` survives the browser](../../.scratch/canvas-interaction-quality/issues/41-ctrl-tab-browser-reservation.md) is **widened to measure `Ctrl`+`Arrow` and `Cmd`+`Arrow` alongside `Ctrl`+`Tab`**. Same measurement, same browsers, one more row in a table someone is already building.

This ADR's own nudge-step finding gains a second consumer. ADR 0030's placement gap is `2 × DominantGridSpacing()`, taking this decision's argument verbatim: a step that follows the grid stays screen-relative within a bounded range instead of becoming a fixed board amount, and for the same reason it cannot join ADR 0025's screen-pixel ordering.
