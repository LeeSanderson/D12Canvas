# Viewport inset for host-placed chrome

Type: grilling
Status: resolved

## Question

Decide whether `DiagramCanvas` learns which part of its container is occluded by chrome, or whether occlusion stays entirely the host's problem.

ADR 0015 frames content into the **full container rect**: zoom-to-fit centres board content in the viewport, inset by a fixed 0.9 fraction, and knows nothing about what is drawn on top of that viewport. ADR 0002 is why — canvas chrome is a standalone component the host places with its own CSS, deliberately given no `Position` parameter, so `DiagramCanvas` is ignorant of the palette by construction and of anything else a host floats over it.

Those two decisions collide in a real deployment. `D12Canvas.App`'s board editor puts a fixed 220px palette rail beside the canvas today, but a host that floats it *over* the canvas — which ADR 0002 explicitly permits — gets a fit that centres content underneath it. ADR 0015 added the minimap as a second occluder, and the selection-anchored property bar will add a third. No margin constant fixes this: the 0.9 inset is symmetric and the occlusion is not, so loosening it hides the problem in the common case while still cropping content behind a wide rail.

The same blind spot affects more than framing. ADR 0009's click-to-add places a new instance at the **viewport centre**, which is the same rect and the same lie; so is the paste anchor's viewport-centre fallback (ADR 0013), and so is the empty-selection case of any future centre-on-something behaviour. If an inset exists, the question is which of these read it.

Decide:

- **Whether an inset exists at all**, or whether a host that floats chrome accepts the consequence. "The host's problem" is a defensible answer — the host is the only party that knows its own layout — but it currently has no mechanism to act on it, so choosing it means saying that out loud rather than by omission.
- **If it exists, what shape it takes.** Four edge insets in CSS pixels as a parameter is the obvious form; a `RenderFragment`-free "reserved region" list is more general and probably more than anything needs. Whether it is one parameter or per-edge matters less than whether the value is a host-supplied constant or something measured from the DOM.
- **Whether it is measured rather than declared.** Chrome sizes are not constants — a palette rail collapses, a property bar's height depends on the selection. Measuring occluders means a JS round trip per layout change and a way to nominate which elements count, which is a substantially bigger mechanism than a declared inset and reintroduces exactly the container-size ordering hazard that produced this codebase's worst gesture leak.
- **Which behaviours read it.** Framing certainly; click-to-add and the paste anchor arguably, since a new instance placed under the palette is the same defect one layer down. Applying it to some and not others needs a stated reason.
- **Whether the minimap is a special case.** It is the one occluder the library itself introduces, so unlike the palette it *could* be known without a host parameter — which is either a useful shortcut or an inconsistency that makes the general mechanism harder to explain.

Answerable now; nothing blocks it. The property bar will add a third occluder and its geometry may sharpen the "measured versus declared" question, but the decision does not wait on it.

## Answer

**No inset, in any shape.** `DiagramCanvas` never learns what is drawn on top of it. Framing, click-to-add and the paste anchor's pointer-free fallback all compute against the full `.diagram-container` rect, and a host that floats chrome over the canvas owns what ends up underneath. Recorded as **ADR 0033**.

**Bullet 1 decided the ticket, and answering it collapsed the other four.** Shape, measured-versus-declared, which behaviours read it and the minimap special case all presuppose an inset exists. What survives of them is recorded in ADR 0033's rejected list rather than as decisions, so the next ticket that reaches for an inset finds the walk already done: four edge insets is the shape to reach for, a region list buys nothing because every consumer tolerates over-reservation, and measuring needs element nomination plus a second asynchronous container-shaped value on the channel whose await ordering produced ticket 04's worst leak.

**The reasoning is that layout already is the inset.** Chrome docked beside the canvas shrinks the box `ZoomPanTracker` measures, so every reader is correct with no library code. `D12Canvas.App` does exactly that today, a 220px flex sibling, so nothing in the acceptance surface is occluded at all. Floating is a host trading real estate for overlay, and the host is the only party that knows the trade it made.

**The ticket's bullet 1 was wrong on its own premise, and correcting it is most of why this is affordable.** It said a host choosing to own occlusion "currently has no mechanism to act on it". `DiagramCanvas.ZoomPanTracker` is public at `DiagramCanvas.razor.cs:172`, carrying `SetPanPosition`, `Pan(dx, dy)` and a settable `Scale`, with `OnZoomOrPanChanged` to hang off. A host floating a 300px rail pans by +150 after a fit.

**ADR 0015's initial fit is the one cost a floating host cannot pre-empt, and it survives unchanged.** Every other case is a command the user pressed. The fit runs on every `Board` load, unconditionally, so the compensating pan is a visible jump. An opt-out was rejected because it hands back something worse: suppressing the fit leaves scale 1.0 at pan origin, which after ADR 0011 removed board extent has no relationship to content, and is the exact hazard ADR 0015 built the fit to prevent. ADR 0015's rejection of that opt-out therefore moves from speculative to harmful, and its rejection of a host-configurable margin is confirmed on a *different* argument than it used: an inset has a real use case where that knob had none, and is declined anyway.

**No preference between docking and floating.** ADR 0002 holds no opinion on host layout, and expressing one here would smuggle it back through the side door of a consequence. It would also tell hosts not to build what all four reference tools build: Miro, FigJam, tldraw and Excalidraw all float chrome over a full-bleed canvas, and ticket 03 recorded one tell of it without naming the layout, tldraw's **25px drag threshold on its own toolbar** against 4px for the mouse elsewhere, which a toolbar needs only when presses on it reach the same arbitration.

**Two facts made the decision safe rather than merely cheap.** `ToBoardPoint` (`DiagramCanvas.razor.cs:2044`) reads the container's `left` and `top` and never its width or height, as do all four gesture-feeding `getContainerDimensions` call sites, so screen-to-board conversion is inset-agnostic by construction and no version of this could have broken hit-testing. And `ZoomPanTracker.Viewport` has two readers wanting opposite things: `Board.GetVisible`/`GetVisibleGroups` mount against it, `ClickToAdd` places against it, so an inset folded into the property would have unmounted content under a floating panel. Had the inset been built, that is ADR 0020's two-named-entry-points rule arriving at a second site.

**Nothing implemented changes.** `ClickToAdd` at `DiagramCanvas.razor.cs:2367` is the only true viewport-centre computation in the repo, and framing, the minimap and the clipboard do not exist yet, so the existing centre-semantics tests (`DiagramCanvasClickToAddTests`, `DiagramCanvasSnapToGridTests:83`, `DiagramCanvasConnectorPaletteTests:85`) all stand. Windowing, `Overscan`, `Content extent` and ADR 0024's viewport-bounded snap candidates were never inset questions, and the snap candidates deliberately are not: occlusion is a fact about painting, and an object under a host's panel is still an object the user is dragging toward.

**Found and corrected: ADR 0031 called the minimap canvas-rendered chrome**, against ADR 0002, ADR 0015, ADR 0021 and `CONTEXT.md`, which all make it host-placed. Its argument strengthens under the fix, since host-placed puts the minimap further from ADR 0026's focus guard rather than nearer, and the suspected downstream cost dissolved on inspection: ADR 0023's dismissal listener is capture-phase on `document`, and the `.diagram-container` scope governs only whether the press is *consumed*, so a host-placed minimap press closes an open menu and starts the pan, which is what its line 109 already accepts.

Pointers added to ADR 0002 and ADR 0015; `Canvas chrome` and `Framing` widened in `CONTEXT.md`. The revisit trigger is [Chrome layout for the acceptance surface](39-app-chrome-layout-rework.md): it prototypes this effort's own acceptance surface, and if it floats the minimap or the property panel, this decision is tested on contact rather than by a future host's complaint.
