# The canvas never learns what is drawn on top of it, and places against the full container rect

`DiagramCanvas` does not learn which part of its container a host has covered with chrome. There is no inset parameter, no reserved-region list and no measurement of occluders. Framing, click-to-add and the paste anchor's pointer-free fallback all compute against the full `.diagram-container` rect, and a host that floats chrome over the canvas owns what ends up underneath it.

This is a decision not to build something, taken because the mechanism a host needs already exists one layer up. It is scoped to this effort, and the section on revisiting says what would change it.

## Layout already is the inset

Chrome placed beside the canvas shrinks `.diagram-container`. `ZoomPanTracker` measures that box through `getContainerDimensions` and a `ResizeObserver`, so every reader is correct with no library code at all. `D12Canvas.App` does exactly this today: a 220px palette rail as a real flex sibling, which narrows the measured container before the canvas ever sees it, and nothing in the acceptance surface is occluded.

Floating is a host trading screen real estate for overlay. The host is the only party that knows what it floated, how wide it is and whether it is currently collapsed, and it is also the party that wrote the CSS that made it float. An inset parameter would be a second expression of something layout already expresses, kept in sync by hand.

The precedent that looks like it applies does not, and saying so matters because the honest version of this decision is harder than the easy one. ADR 0011 rejected an extent knob and ADR 0015 rejected a host-configurable framing margin, both as knobs added for symmetry with no use case behind them. An inset has a real use case. It is declined anyway, on the different ground above.

## What the host is left holding

`DiagramCanvas.ZoomPanTracker` is public. It carries `SetPanPosition`, `Pan(dx, dy)` and a settable `Scale`, and `OnZoomOrPanChanged` fires when the transform moves. A host floating a 300px left rail pans by +150 after a fit and gets the view it wanted.

Ticket 24 asserted that a host choosing to own occlusion "currently has no mechanism to act on it". That was wrong, and correcting it is most of why this decision is affordable.

## The initial fit is the one cost the host cannot pre-empt

Every other case is a command the user pressed, where "you floated it, you own it" is a fair answer. ADR 0015's initial fit is not. It runs whenever a new `Board` is set, it is unconditional, and it has no host opt-out, so a floating host can only correct it after it has landed. Its compensating pan is therefore a visible jump on load.

An opt-out was rejected because it hands back something worse. Suppressing the fit leaves the canvas at scale 1.0 and pan origin, and ADR 0011 removed board extent, so the origin holds no relationship to content. A board authored away from the origin would open onto empty canvas, which is the exact hazard ADR 0015 built the fit to prevent. The escape is worse than what it escapes.

## The library takes no position between docking and floating

This decision states a consequence. It does not recommend a layout.

ADR 0002 makes placement "entirely the host's own CSS/layout" and explicitly permits floating on top, and a preference expressed here would smuggle an opinion back in through the side door of a consequence. There is also no evidence for the preference: Miro, FigJam, tldraw and Excalidraw all float chrome over a full-bleed canvas. Ticket 03 recorded one tell of it without naming the layout, tldraw carrying a **25px drag threshold on its own toolbar** against 4px for the mouse elsewhere, which a toolbar needs only when presses on it reach the same arbitration.

## Two facts that made this safe rather than merely cheap

**The coordinate transform could never have picked an inset up.** `ToBoardPoint` at `DiagramCanvas.razor.cs:2044` reads the container's `left` and `top` and never its width or height. Screen-to-board conversion is inset-agnostic by construction, so no version of this decision could have broken hit-testing, and the four `getContainerDimensions` call sites that feed gestures all take `left`/`top` only.

**`Viewport` has two readers that want opposite things.** `Board.GetVisible` and `GetVisibleGroups` read it to decide what to mount; `ClickToAdd` reads it to pick a destination. An inset folded into the property would have unmounted content sitting under a floating panel, which is the opposite of every intent behind the feature. Had the inset been built, this is ADR 0020's rule arriving at a second site: derived geometry gets two named entry points rather than one property with an optional source, because a call site that forgets the source fails silently.

## Scope of what reads the full rect

Implemented today: `ClickToAdd` at `DiagramCanvas.razor.cs:2367`, the only true viewport-centre computation in the repo.

Specified and not yet built: `Framing` and the three viewport commands (ADR 0015), the initial fit (ADR 0015), the paste anchor's viewport-centre fallback (ADR 0013) and the keyboard menu's paste anchor (ADR 0026). All of them take the full container rect.

Nothing changes for windowing, `Overscan`, `Content extent`, ADR 0024's viewport-bounded snap candidates or the `Minimap`'s own framing of its own box. Those were never inset questions, and the snap candidates deliberately are not: occlusion is a fact about painting, and an object under a host's panel is still an object the user is dragging toward.

## When to revisit

The cheap trigger is [Chrome layout for the acceptance surface](../../.scratch/canvas-interaction-quality/issues/39-app-chrome-layout-rework.md), which prototypes `D12Canvas.App`'s own chrome and is where the minimap, the property panel and the wheel device profile all need somewhere to live. If it floats any of them, this decision is tested on contact by the effort's own acceptance surface rather than by a future host's complaint.

Reopen it if a host that must be full-bleed reports the compensating pan as unacceptable, or if a second library-owned behaviour starts placing content and the correction has to be written twice.

## Considered and rejected

- **Four edge insets in CSS pixels as a `DiagramCanvas` parameter.** The obvious form, and the one that would over-reserve gracefully: every consumer picks a destination with tolerance, so a corner-anchored minimap insetting a full-width bottom strip costs slightly more zoom-out and nothing else. Declined with the feature rather than on its own merits, and it is the shape to reach for if this is ever reversed.
- **A reserved-region list rather than four edges.** More general, and the generality buys nothing, because every consumer tolerates over-reservation. It would be a second geometry vocabulary for a decision with no exact case.
- **Measuring occluders from the DOM.** Needs a way for a host to nominate which elements count, a `ResizeObserver` per occluder, and a second asynchronous container-shaped value arriving on the same channel whose await ordering produced this codebase's worst gesture leak. It also answers a question the declared form answers for free: a rail that collapses is host state, and a host binding `Inset` to that state is one expression, not a measurement problem.
- **Folding the inset into `ZoomPanTracker.Viewport`.** Unmounts content under a floating panel, per the section above.
- **A host opt-out on the initial fit.** ADR 0015 rejected this as speculative; this decision rejects it as harmful, since the suppressed view is scale 1.0 at pan origin.
- **Making `Framing` and `Content extent` public so a floating host computes its own opening view.** More defensible than an opt-out, and still a public expansion aimed at one host shape, reaching a place `ZoomPanTracker` already reaches.
- **Recommending docking over floating.** Tells hosts not to build what all four reference tools build, and puts an opinion about host layout into a library ADR 0002 deliberately kept out of it.
- **Insetting around canvas-rendered chrome too**, on the ground that the library does know where the context menu and the property bar are. This one is not merely declined, it is impossible, and the reason is worth keeping because it explains why the split falls where it does. An occluder may only inset if its position is independent of the viewport. Host-placed chrome qualifies by construction, since ADR 0002 makes it the host's own layout against the page, which knows nothing of pan and zoom. Canvas-rendered chrome disqualifies itself by construction, since ADR 0021 positions the bar from `bounds.X * Scale + PanX`: frame the selection, the bar moves to the new position, the inset changes, reframe. The split between the two kinds is therefore forced rather than an inconsistency needing an excuse.
