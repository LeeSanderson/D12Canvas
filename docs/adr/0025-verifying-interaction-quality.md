# Interaction is verified by invariants and never by magnitudes, and the closed set of eight is the coverage obligation

Pointer behaviour is asserted at three drive points: **gesture objects driven directly** over a purpose-built context, **the press-to-kind mapping** through bUnit, and **interaction probes** that drive a real browser and assert state rather than pixels. No layer asserts a duration, a frame rate, or a tuned constant's value. The standing rule is a parameterised test over ADR 0018's eight pointer gestures rather than a sentence asking an author to remember, and there is no manual acceptance pass.

This exists because neither existing layer can observe what this effort is about. A screenshot cannot see that a gesture leaked, and bUnit's current route to a gesture is about to be deleted.

## The existing suite loses its drive point, not its assertions

39 of the 86 bUnit test files dispatch pointer events at elements, across 573 call sites. ADR 0018 deletes every `@onmousedown`, `@onclick` and `@ondblclick` binding on board content, so those tests do not fail, they stop being able to reach the code at all.

That reframes the work. This is not a decision about which new tests to add on top of a working suite. It is a decision about where roughly forty files reattach, and getting it wrong costs the rework twice.

## Three drive points, and what each owns

**Gesture objects, driven directly.** A gesture is constructed over a fake context, given moves and a release, and asked what it published and what it committed. No renderer, no DOM. ADR 0018 already paid for this without being asked to: it gave gestures an explicit context rather than a back-reference to `DiagramCanvas`, specifically so the context could not become reach-into-everything. A fake context is the same seam used from the other side.

**The press-to-kind mapping, through bUnit.** Role, buttons, modifiers, `pointerType` and the current selection resolve to one of eight owners. That is a table, and it is tested as a table. Eight owners, eleven roles and three buttons is a space no quantity of hand-written drag sequences covers evenly, which is how 573 call sites managed to leave all six leaks undetected.

**Interaction probes, in a real browser.** Everything JavaScript owns and bUnit cannot see: the classification walk, the press count, the movement threshold and the decision not to call C# below it, the five synchronous role-derived decisions, animation-frame coalescing, ownership by pointer and button, and the three release channels.

The split is not tidiness. A gesture correctly implemented and never selected is invisible to the first drive point, and move arithmetic asserted through rendered markup is expensive and vague. Each drive point exists because the others are blind to something.

## Probes live in the visual-test project, and the container is not the reason

Interaction probes are new classes in `D12Canvas.VisualTests`, not a third project. Both prior probes of this shape already lived there: the wheel input probe on `prototype/wheel-pan-zoom` and the clipboard route probe on `prototype/clipboard-menu-route`.

A separate project looks attractive because these tests have no font or anti-aliasing sensitivity and therefore no need of the pinned container or of committed baselines. It buys nothing. The container is a convention about how the suite is invoked rather than a property of the project, and `-parallel none` is not a screenshot concern: its documented symptoms include a locator timing out at zero elements and a click intercepted by an overlapping element, which are contention on the one shared `D12Canvas.Demo` process. An assertion-only suite has exactly that contention. Splitting would inherit the constraint and add a CI job.

The cost is stated rather than discovered: probe failures are gated behind the slow container job. For this effort that is close to free, since nearly every ticket on this map touches rendered markup and triggers the container run regardless.

## A `[JSInvokable]` method cannot be internal, and the test seam is narrower than it looks

Blazor requires `[JSInvokable]` methods to be public. All twenty existing ones are public on `DiagramCanvas` with `DotNetObjectReference.Create(this)`, and the four pointer entry points follow by force. So bUnit reaches `OnPointerPressed`, `OnPointerMoved`, `OnPointerReleased` and `OnPointerCancelled` through the rendered component with no build-file change at all.

**ADR 0018's "entry is `internal`" therefore cannot be read literally**, and the failure mode is worth recording because it is silent: a `[JSInvokable]` method that is not public fails at *runtime*, not compile time. Honouring that sentence as written ships a canvas where no pointer gesture works, with a clean build. The sentence means the arbitration surface, which is the gesture objects and their context, not the interop entry.

`InternalsVisibleTo` on `D12Canvas.Tests` is taken, and it buys exactly one thing: **ADR 0020's preview**. That ADR's stated payoff is that live geometry becomes assertable without a browser because the preview is data rather than pixels. Without internals access the preview is reachable only through rendered markup, so "does the edge follow the shape" reverts to a question about a `style` attribute. Reading the preview directly is the whole purchase.

This does not reopen ADR 0018's rejection of a public observation surface. That rejection was about a *host* inspecting a live gesture, and it is upheld: nothing here exposes public API, and the leak invariant below is deliberately asserted without any observation surface at all.

## A leak is a behavioural fact, so it is asserted behaviourally

The definition comes from the probe that found all six: **a response to a buttonless pointer is a leaked gesture.** A release-reliability case ends one gesture the way a user plausibly might, moves the pointer with no button held, and asserts nothing responded.

This needs no reflected attribute, no `data-gesture` on the canvas, and no probe page rendering internals. A reflected attribute was the obvious alternative and is rejected on a specific cost: the visual suite verifies `.verified.html` alongside the images, so gesture state would enter every HTML baseline in the project.

The behavioural assertion is blind to a leaked gesture that does nothing on a buttonless move, which is exactly `SelectEdge` and `Native`. Those are ADR 0018's two degenerate members, and its own argument is that the gesture holding no state is the one that cannot leak. **The blind spot and the harmless set are the same set**, which is why this is a property of the approach rather than a gap in it.

**`lostpointercapture` on a live gesture writes `console.error`, and the fixture fails any test that logs one.** ADR 0018 calls this net loud and says it should never fire, which leaves it with no behaviour to observe and therefore nothing to assert. The console is an observation channel that costs no production markup and no public API, and it converts "should never fire" from an aspiration into something the suite enforces.

## The line, drawn three times

Every layer here proves an invariant. No layer measures a magnitude. The same line arrived from three directions and is recorded once:

**Plumbing, not magnitudes.** Ticket 17 found `WheelInputProbeTests` asserting green that a wheel notch delivers `deltaY: 120` while real hardware delivers 100, because Playwright's synthetic wheel skips the engine's line-to-pixel conversion. Any constant tuned against that probe inherits a 20% error. Probes prove that modifiers survived the interop hop, that `defaultPrevented` is set, that the pointer-anchor invariant holds. They never establish a number.

**Relationships, not values.** A test asserting `DragThreshold == 4` catches nothing the source diff does not already show, and would have passed ticket 17's constant unchallenged, since that number was wrong the day it was written rather than drifted into. What is asserted instead is the ordering the family must keep: **drag threshold (4) < object snap radius (8) < edge hit band (20)**, all screen pixels. A threshold raised above the snap radius puts the pointer out of snapping range of its origin by the time the drag promotes, so object snapping silently stops firing on short drags and nothing else in the suite notices.

The test's first job is enforcing that these quantities share a unit at all, and it finds a live defect on contact. **`PortHitRadius = 10` is board space**, documented in its own comment as unaffected by zoom, so it cannot join the ordering. ADR 0017 made hit regions screen-constant precisely because such a number describes how precisely a hand can aim; the port tolerance is the one member of that family still measured in board units, covering 2.5 screen pixels at 0.25x zoom, at exactly the zoom where aiming is hardest. The numbers belong to the port affordance work, and this hands it the unit rather than the value.

**Counts, not clocks.** ADR 0020 states its budget structurally, as one frame per pointer move with per-frame work proportional to the gesture's participants and the edges touching them rather than to board size, and says a millisecond figure would be device-dependent and untestable. Its assertions are counts. Render counts in bUnit put five hundred edges on a board, drag one instance, and assert the number that re-rendered tracks the two it touches. Coalescing counts in the browser dispatch ten `pointermove` events inside one frame and assert one `OnPointerMoved` arrives.

A wall-clock ceiling as a canary was considered and rejected: one generous enough not to flake on a loaded runner is generous enough to pass any regression worth catching, so it costs maintenance and returns a green light that means nothing.

## There is no manual acceptance pass, and what that costs

Roughly ten tuned numbers ship defended by nothing that judges them: the zoom sensitivity constant, the ambient smoothing durations and the idle boundary from ADR 0019, ADR 0015's framing flight, ADR 0022's threshold, ADR 0024's snap radius and velocity cut-off, ADR 0017's edge band and press margin, and the level-of-detail threshold. The relationship assertions keep them consistent with each other and say nothing about whether any of them feels right.

The same gap covers rate rather than count. A canvas that drops from sixty frames to thirty with identical render counts is invisible to every layer described here.

This is recorded as a known consequence rather than an oversight, because the alternative was considered and declined. A checklist scoped to those ten items was the proposal; the judgement was that no manual layer belongs in this deliverable.

## The standing rule is a test, not a sentence

The existing rule works because a person recognises a new rendered visual state when they have made one. "A new interaction" is not like that, so the same rule written for gestures gets applied where the suite cannot verify anything and skipped where it could.

**The obligation is a parameterised test over ADR 0018's closed set of eight.** One release-reliability case per member, and a ninth gesture fails the suite until its case exists. The rule stops being something a reviewer remembers and becomes something that breaks.

That is the payoff from closing the set. A prose rule relies on the author agreeing they added an interaction; a theory over a closed enumeration does not care whether they agree. Closure is already load-bearing in ADR 0018, so depending on it here costs nothing new.

`docs/agents/testing.md` gains one line pointing at that test rather than restating the obligation in prose, so there is one source of truth and it is the executable one. The visual-state rule is unchanged and unaffected; this is additive.

## What the browser can reach, and the one thing it cannot

The leak probe settled reachability empirically across thirteen probes, and both shapes assumed hardest were expressible: the right button pressed and released mid-pan, and a press and release dispatched in one JavaScript turn, which is the await-ordering race that no static reading finds.

Under ADR 0018 most of them stop being cases. Pointer capture delivers events to the capturing element regardless of pointer position, so "released outside the viewport" has no meaning once capture is taken, and both cancel channels are reachable from script: removing the captured element fires `lostpointercapture`, and `pointercancel` can be dispatched directly.

**Device physics is the boundary.** Playwright's synthetic wheel is coarse by construction, so ADR 0019's `Auto` profile classifies it as a mouse every time. The trackpad branch is reachable only by forcing the profile explicitly or by dispatching fine-delta events rather than using the mouse wheel API, and this is recorded so that a future green suite is not mistaken for trackpad coverage.

## Animation runs reduced, and one case runs it both ways

ADR 0015 already required visual tests to run the reduced-motion path so baselines capture the destination rather than racing a transition. That is applied **suite-wide** in the browser context options, not per case, on the same grounds as the standing rule: a policy depending on each author remembering is the failure mode this decision exists to remove.

Two constraints follow and are not optional.

**A reduced-motion rule may only zero durations.** Suite-wide means every baseline in the project documents the reduced-motion rendering and none documents the default, which is honest only if the two agree at rest. A rule that changed a destination or a layout would make the entire baseline set describe a rendering no ordinary user sees.

**ADR 0015's pointer-event suppression must key off the transition's own lifecycle, not a timer.** That ADR suppresses pointer events on the container for the duration of a framing flight without naming the mechanism. Implemented as a 250ms timer, reduced motion leaves the canvas dead to the pointer for 250ms with nothing animating. This is a defect prevented rather than discovered, and it is the reason **one case deliberately opts back into animation**: it asserts suppression is applied during a flight and cleared after, never asserting the duration, and its reduced-motion counterpart asserts the suppression has already cleared. That pair is the only place in the plan where both paths run.

**Two of the originating ticket's premises were wrong and are corrected here.** All 26 visual test files already pass `ScreenshotAnimations.Disabled`, so the screenshot itself has never raced the ambient transition; the exposure is confined to steps *before* the screenshot, such as bounding-box reads and clicks taken mid-flight. And no `prefers-reduced-motion` rule exists anywhere in the codebase, so the reduced-motion context option is a no-op until ADR 0015's implementation adds one. The policy above has that as a precondition, not an assumption.

## Two findings handed to implementation

**CI runs the visual job without `-parallel none`**, the flag every other document calls mandatory and without which failures are documented to be indistinguishable from real regressions. A gate that flakes proves nothing, so this is in scope for a decision about how the effort proves its deliverable.

**ADR 0015 shifts baselines twice**: the canvas frames all content when a `Board` is first set, changing the opening view of every board-mounting baseline, and the minimap is new chrome needing its own demo coverage. Both are planned for rather than discovered mid-run.

## What this amends and confirms

**ADR 0015 is amended in one place**: pointer-event suppression during a framing flight is driven by the transition's lifecycle rather than a fixed duration. Its requirement that visual tests run reduced is confirmed and widened from a recommendation to a suite-wide setting with a stated constraint on what such a rule may change.

**ADR 0018 is confirmed and one sentence is clarified.** The closed set of eight becomes the suite's coverage obligation, which is a second load-bearing use of that closure. Its rejection of a public observation surface is upheld, since the leak invariant is asserted behaviourally. Its "entry is `internal`" is clarified as covering the arbitration surface and not the interop entry, which the framework forces public.

**ADR 0020 is confirmed.** Its structural budget is what the render-count assertions assert, and its claim that a data-shaped preview is assertable without a browser is what `InternalsVisibleTo` is bought for.

**ADR 0019 gains a caveat rather than an amendment**: its `Auto` profile is not exercisable through the synthetic wheel API, so any test touching the trackpad branch sets the profile explicitly.

**The layered testing strategy settled in `d12canvas-next` is extended, not replaced.** Two projects remain. What changes is that one of them now holds tests that assert state rather than pixels, and that the interaction obligation is expressed as a test rather than as prose.

## Considered and rejected

- **A third test project for assertion-only browser tests** — the container is a convention about invocation rather than a property of the project, and `-parallel none` is shared-process contention that an assertion suite inherits anyway, so the split buys nothing and adds a CI job.
- **A reflected `data-gesture` attribute on the canvas** — precise, but the visual suite verifies HTML alongside images, so gesture state enters every baseline in the project.
- **A probe page in the demo app rendering gesture internals** — needs the arbitration surface pushed across an assembly boundary, a larger hole than the assembly-internal test seam.
- **Making the gesture types public** — widens the library's real API to serve tests, and commits to a shape ADR 0018 deliberately declined to commit to.
- **Dropping `InternalsVisibleTo` and asserting the preview through rendered markup** — spends ADR 0020's main payoff to save one line of build configuration.
- **A manual acceptance checklist scoped to the tuned constants** — proposed and declined; the consequence is recorded above rather than hidden.
- **Pinning each constant by value** — restates the source, catches only drift, and would have passed the one real constant defect this effort has seen.
- **A generous wall-clock ceiling as a performance canary** — loose enough to survive a loaded runner is loose enough to miss anything worth catching.
- **A per-ticket interaction rule mirroring the visual-state rule** — relies on the author agreeing they added an interaction, which is exactly what the closed set makes unnecessary.
- **Per-case reduced motion** — a policy each author must remember, which is the failure mode rejected in three other places here.
- **Keeping the current bUnit drag tests and adding assertions** — they dispatch at bindings ADR 0018 deletes, so there is nothing to add assertions to.
