# Gesture cancellation and revert semantics

Type: grilling
Status: resolved
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

## Answer

Recorded as **ADR 0031**. Cancel is three canvas-level steps — drop the `Gesture preview`, restore the `Selection snapshot` taken at press, mark the gesture `cancelled` — and no gesture implements any of them.

**Three of the five bullets were already answered when this was worked.** The cancel input is Escape (ADR 0018, amending ADR 0009's row; ADR 0026 carries it), and the collision this ticket asks to resolve was resolved the other way by ADR 0022, which rejected a secondary press as a cancel because two abort routes leave a user unable to tell which they invoked. Revert-versus-commit and the history question both fall out of ADR 0020's invariant that no pointer gesture creates a command before release: committed state is never touched, so there is no inverse to apply and a cancelled gesture is zero entries by construction. `_history` never has to express a mark; tldraw needs one because it writes through.

**The viewport is the one thing cancel does not restore.** Selection and viewport are both outside history — ADR 0007 excludes selection explicitly and never mentions the viewport — and they differ only on whether the user can get it back by hand. Rebuilding a twelve-shape selection has no route; panning back is one drag you can watch yourself make. A cancelled `Pan` therefore stops panning and restores nothing.

**Selection revert reaches the press, not just the drag.** ADR 0022's press-time collapse reverts too, on the asymmetry that settled the viewport: restoring when the user wanted the thing they pressed costs one click, not restoring when they wanted the twelve costs a rebuild with no undo route — and ADR 0022's own reason for the collapse is that *the drag* must move something, which Escape has just denied. Two consequences follow rather than being chosen: **every** gesture snapshots, because a per-gesture rule about who needs one is what the ninth gesture forgets; and the snapshot covers **both selection fields together**, since ticket 08 found them mutually exclusive.

**Cancel ends the gesture's effects, not its ownership.** `pointing → active → cancelled`, capture held until the claiming button releases, and that release is a no-op. ADR 0030 supplies the victim of the alternative: a `pointing`-phase press on a port span has no preview to discard, so "cancel means drop the preview" leaves Escape doing nothing and the release quick-creating the instance it was pressed to prevent. Today's code gets this right for its one gesture only because `CancelPortDrag` and the mouseup path read the same flag. Dropping capture instead would send the eventual `pointerup` to whatever is under the cursor. `cancelled` rather than `abandoned`, which ADR 0018 already spends on a threshold crossing under a pointing-only owner.

**Interruption is a cancel, and the channel does not exist yet.** Pointer capture survives window focus loss, so neither `pointercancel` nor the `lostpointercapture` net fires when you `Alt`+`Tab` away with a button held, release over another application and come back — the gesture is still live and the next move continues it. That is leak path seven through the one door ADR 0018 left unwatched; `DiagramCanvas.razor.js` has no `blur` or `visibilitychange` listener, only `keydown` and `keyup`. A window `blur` listener becomes the third channel and it cancels, because committing writes history at a coordinate nobody chose while the user cannot see the canvas. **One word means two events**: ADR 0007's "gesture commit (pointer-up/blur)" is *element* blur committing an inline edit, as ADR 0018 states when it explains why focus is transferred explicitly, and it does not pre-authorise commit-on-interruption. Verification splits on ADR 0025's own line — the plumbing is assertable by dispatching the event, and whether a real browser delivers it is the device physics ADR 0029 had to measure by hand.

**Escape has three rungs and is spent for the press.** ADR 0018 wrote a two-rung rule for a three-rung reality: today's `OnEscapePressed` also clears `_portFocusInstanceId` and `_pendingConnectorSource`, the keyboard connector pick, which ADR 0027 depends on. The chain is pointer gesture, keyboard pick, selection. **A second Escape does nothing**, because the fall-through gates on who owns the pointer rather than on whether the gesture is still useful — otherwise an innocent double-tap destroys the selection the first tap restored, which is the exact loss the snapshot exists to prevent. Staging Escape at all is a **behaviour change**: today one press does everything, and its comment calls that deliberate.

**`OnCancel` has no work, so it leaves the interface**, and an argument ADR 0018 made is corrected with it. That ADR chose polymorphic gesture objects over a switch partly on this ticket's ground — "cancellation becomes a method you cannot forget to implement" — and a method with no body guarantees nothing. The decision survives on `OnMove`/`OnRelease`; the sentence does not. Where the guarantee actually lands is ADR 0025's `[Theory]` over the closed eight, which gains a cancel case per member — the third load-bearing use of that closure. The accepted cost is that ADR 0025's cheapest drive point no longer reaches the cancel path.

**The minimap could not hear Escape.** ADR 0026 gives Escape no guard on the reasoning that "a live gesture implies focus inside the container", and there is one counterexample: `MinimapPan` is the member with no role, ADR 0018 lists the press-time focus transfer among four decisions deriving "from the role alone", and ADR 0026 gives the minimap no tab stop and marks it `aria-hidden`. With focus on a host input outside `.diagram-container` the guard fails and Escape never arrives. **The four synchronous decisions follow the press, not the role** — role-derived when classified, fixed for the minimap — which makes an existing exception honest, since ADR 0018 already grants the minimap capture. The reason to close it rather than declare `MinimapPan` uncancellable is that the broken thing is ADR 0026's claim, which the next chrome gesture would inherit; the fog already names one.

**Cancel is silent and that is accepted**, handed to the cursor and micro-feedback fog patch with a correction to the argument that patch has been using. Pointer capture freezing the cursor does not make mid-gesture feedback unreachable: the cursor showing *is the capture element's own*, and ADR 0018 puts capture on a stable element the library controls, so restyling it is an open channel. Three of that patch's dependants move from proven-unreachable back to undecided.

Amends **ADR 0018** in three places (interface, the hollow argument, press-derived rather than role-derived), **ADR 0026** in one, **ADR 0025** in one, and ADR 0009's Escape row again. Confirms 0020, 0022 and 0007. `Selection snapshot` added to `CONTEXT.md`; `Pointer gesture`, `Gesture preview`, `Hit target` and `Release-reliability case` widened.
