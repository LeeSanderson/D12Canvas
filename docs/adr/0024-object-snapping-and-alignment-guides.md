# Grid snapping ships on, object snapping ships off, and when both run they resolve per axis with object winning

Object snapping is new: while a selection is moved or resized, its edges and centres align to those of neighbouring entities, and an **alignment guide** is drawn along each line that matched. Grid snapping already existed as an off-by-default toggle. This decides how the two compose, what participates, which modifiers govern them, and what the guides look like.

The reference bar had already answered more of this than the ticket assumed. Both open-source tools arrived independently at an 8 screen-pixel threshold, both make the modifier invert rather than suppress, both ship equal-spacing as a first-class snap type, both draw a line plus a per-point glyph, and neither shows a numeric distance. Where this ADR diverges from that bar it says so and gives the reason, because the divergences are not preferences.

## Two toggles, opposite defaults

`SnapToGrid` flips from off to **on**. Object snapping arrives as a second bindable parameter, **off**. Both are ordinary host-controlled parameters with a `Changed` callback, and neither writes the other.

**The default flip amends one word of the unbounded-zoom decision and costs it none of its reasoning.** That ADR says "an optional, off-by-default toggle" and then never defends the default anywhere: all ten of its rejected alternatives concern persistence, snap granularity, and how the toggle is exposed. The map holds that ADR settled, and it stays settled. This is the map's recurring lesson running backwards, where a rule stated something its own argument never supported.

The flip is a user-visible behaviour reversal, the second on this effort after pan left the primary button. Every placement and every single-instance move now snaps out of the box, and because live gesture geometry moved snapping into the tick, the snap is visible during the drag rather than at release.

One downstream consequence is inherited rather than created. The align/distribute decision accepted that distribute rounds the gap, pins the first entity and lets the last drift by up to half a grid step per interval, and it accepted that explicitly because snapping was opt-in, noting "with snap off, both extremes stay put; the drift exists only because snapping introduced it." That drift is now what everyone gets. Nothing in that ADR breaks, but a cost it priced as a minority case is now the default case.

**Mutual exclusion at the toggle level was rejected on an API argument rather than a behavioural one.** Excalidraw makes enabling one mode disable the other. Both of ours are bindable parameters, so mutual exclusion means flipping one fires *the other's* changed callback as a side effect, giving a host that binds both a write it never asked for, possibly from a chord it disabled.

## Object snap wins per axis, and grid fills the rest

When both are on, each axis resolves independently: object snapping takes the axis if it fires there, and grid snapping takes any axis it did not.

Alignment is an axis-wise fact. Lining up a left edge says nothing about vertical placement, so letting a horizontal match silently abandon the grid vertically is a side effect nobody asked for. Whole-position precedence also has an unstable edge, where the moment object snapping stops firing on one axis the other jumps back onto the grid, so a small pointer movement produces a two-axis correction.

The accepted cost is that one drag can be governed by two rules at once, and only the object-governed axis draws a guide, so the grid-governed axis has no feedback. Judged in the prototype and accepted.

## Ctrl suppresses, because inversion needs one toggle to be coherent

While Ctrl is held, nothing snaps. Not object, not grid.

The reference bar inverts instead, and its argument is good: one-way suppression only serves the user who has snapping on, while inversion also gives the user who has it off one snapped drag. That argument depends on there being **one** snap mode. With two at opposite defaults, inverting each independently takes the default user from grid-on/object-off to grid-off/object-**on**, handing them alignment guides they never enabled at the exact moment they pressed a key meaning "let me place this freely". Inverting the aggregate instead needs a special case for the both-off state, at which point Ctrl stops being one sentence.

Suppression is also the only direction that can never *summon* chrome. Inversion can put guides on screen from a key press for someone who opted out of them.

The capability being declined, recorded rather than buried: a user who has turned both toggles off has no route to a momentary snapped drag, where Excalidraw and tldraw would give them one.

## The macOS secondary click forces the modifier live, and closes a hole

On macOS, Ctrl+click is the system secondary click. **A platform check makes a Ctrl+primary press a secondary press there**, so the button-semantics ADR's secondary row applies unchanged: it pans, and it opens the object menu on release.

That ADR's rejected list already flagged that a press-time Ctrl would need this check. What it did not notice is the hole its own decisions left: it suppresses `contextmenu` inside `.diagram-container` as a correctness fix, and puts our menu on the secondary button's release, so **on macOS today a Ctrl+click opens no menu at all**. The native one is suppressed and ours never fires, because the press reported button 0. A trackpad two-finger tap reports button 2 and is unaffected; a Ctrl+click user simply loses the gesture. The platform check fixes that as a side effect of taking the key.

The consequence for this ADR is the useful part. **Ctrl is read live on every move, never latched at press**, because on macOS a Ctrl press is not a primary press at all, so there is nothing to latch. The user presses without Ctrl and holds it mid-drag. That is identical on both platforms and needs no branch.

This hands the latched-versus-live decision a *derived* answer for Ctrl rather than a free choice, and it dissolves that ticket's channel worry for this modifier specifically: a modifier pressed with no pointer movement generates no event, but snapping has no observable effect until the pointer moves, so carrying modifier state on `OnPointerMoved` is sufficient and the arbitration model needs no new channel.

It also empties the modifier budget for pressing through content. Ctrl is not merely spent, it is **unusable at press time**, and a modifier that works at press on Windows but not macOS is worse than none. That is a harsher answer than the button-semantics ADR expected when it deferred the question, and it tilts the buried-instance decision toward the menu route it currently treats as the expensive one.

## Shift locks an axis on moves, and stays unbound on resize

Holding Shift during a move discards motion on one axis, so the selection travels in a straight line. Which axis is locked comes from the press-anchored delta and is recomputed every move, so a drag that starts horizontal and turns vertical follows.

**The locked axis is exempt from all snapping.** Locking means the coordinate does not change, and rounding it to the grid would move the selection along the axis the user just locked. This composes with per-axis precedence without a new rule: the lock removes an axis from play and snapping resolves on what is left.

Shift keeps its existing selection meaning untouched. A Shift press on a non-member still appends it at press and then axis-locks the drag of the enlarged selection. The button-semantics ADR had already established that these compose, because the drag threshold separates them and the release-time toggle never fires once the threshold is crossed.

**Shift is deliberately left unbound on resize.** This effort ruled aspect-lock out of scope along with rotation, which technically frees the key. Spending it would be worse than leaving it idle: Shift during a resize means preserve aspect ratio in every tool a user has met, so binding it to axis-lock would mislead now and make aspect-lock harder to add later.

## What snaps to what

**Three anchors per axis on each side**, so nine pairings per axis. Left, centre and right horizontally; top, centre and bottom vertically. This covers edge-to-edge alignment and edge-to-edge adjacency in one cross product, and matches what Figma documents as "align the centers and outermost points".

**The mover is the selection's bounding box, not its members.** Live gesture geometry already fixed this: one snap per tick applied to the selection as a rigid body with no branch on selection size, which keeps members' relative offsets intact at any count.

**Candidates come from the existing viewport query, which bounds them to what is on screen.** The ticket framed this as a performance concession. It is better read as semantics, with performance as the side effect: a guide drawn to an object the user cannot see is a line to nowhere, so an off-screen candidate has nothing to offer even if it were free.

**Locked entities are candidates.** Locking withholds an entity from commands, from primary-press hit-testing and from marquee selection, and snapping is none of those. It is reference, not participation, so nothing is amended. It is also the case that matters most in practice, because people lock exactly the background frames and templates they then want to align against.

**Level-of-detail placeholders are candidates**, for the same reason they are full hit targets: the cost that mechanism exists to avoid is mounting, not reachability.

**Edges are not candidates**, having no bounds, and neither are floating endpoints.

**A `Group` contributes nothing of its own, and the cost of that is exactly one anchor.** A group's bounds are the union of its members', so every edge of a group's bounds already coincides with some member's edge by construction: the leftmost member defines the group's left, and so on for all four. Excluding groups therefore loses no edge anchors at all. It loses the group's **centre**, which generally sits where nothing is drawn. That is the argument for excluding them: a group's bounds are derived and never painted, so a guide along a group centre would be a line through empty space with nothing on it to explain why it appeared. Centring against a group stays reachable through align-centre, which is the deliberate counterpart this work is paired with.

## Equal-spacing is in, and the naive implementation is quadratic

An equal-spacing snap reproduces a gap that already exists elsewhere, which is what lets a fourth box drop into a row of three and land evenly. Both open-source tools ship it as a first-class snap type with named variants, and Miro advertises it in the same sentence as plain alignment, so it is expected rather than exotic.

**It applies to moves only, never resize**, copying tldraw's limit for its stated reason: equal spacing is a claim about layout *between* objects, and it is not clear what it should mean while one object's size is changing.

**A gap only exists between two entities that overlap on the perpendicular axis.** A horizontal gap between two boxes means nothing if they do not share any vertical extent.

That predicate is a filter, not a bound, and getting this backwards is easy. Enumerating every candidate pair and then filtering is O(n²) per frame, which breaks the structural budget live gesture geometry set of work proportional to a gesture's participants rather than to board size. The order that fixes it: **filter candidates to those overlapping the mover on the perpendicular axis first, which is linear, then enumerate pairs only within that set.** On a real board few entities share a row with the mover, so the pair enumeration runs over a small set. Point snapping was never at risk, being linear in candidates either way.

**The snap search runs twice per move.** The first pass finds the offset; the second runs from the already-snapped position to collect what matched. Without it a guide describes where the selection *was* rather than where it now *is*. Excalidraw does the same and says why in a comment, and it is not discoverable by reading the output.

**Snapping is skipped while the pointer is moving fast**, above 3 screen pixels per millisecond. tldraw does this and the reasoning generalises: snapping is help for a user being careful, and an obstacle to one who is not. This needs a pointer-velocity signal that the arbitration model does not currently produce, so it is a small addition to what JavaScript reports alongside each coalesced move.

## Scope by gesture

**Resize snaps, with point snaps only.** The anchor set shrinks to the edges actually moving, one for an edge handle and two for a corner. The anchored opposite edge and the centre do not participate, because they are not what the user is aiming.

**Edge endpoints get nothing here.** Dragging a floating endpoint near a shape is attachment, and the attachment decision owns it. The reference bar treats it as a separate mechanism rather than a case of this one: tldraw gives arrow snapping its own distance formula entirely, "4 at the minimum and either 16 or 15% of the smaller dimension of the target shape, whichever is smaller", against the flat 8 used here.

**Align and distribute are untouched.** Those commands compute exact targets and object snapping would fight them for the same coordinate. Their grid handling is already decided.

**Keyboard nudge gets nothing.** Live gesture geometry states outright that keyboard work should not reach for the preview, because a keypress's result is fully determined the moment it is pressed and there is nothing provisional to show. Whether the nudge *step* becomes the grid size under grid snapping, and whether a nudge that lands within tolerance of a neighbour should snap at all, both belong to the keyboard-parity decision.

**Placement and paste get grid snapping only.** Neither is a member of the arbitration model's closed set of pointer gestures, so neither publishes a gesture preview and neither has a frame loop for a guide to live in. Object snapping without a visible guide is a silent correction, which is worse than none: the content lands somewhere the user did not put it and nothing says why. The boundary is mechanical rather than a preference, so if placement ever grows a preview this should be revisited rather than treated as settled against.

## The guides

**A full-bleed line spanning the viewport, with no per-point glyph.** Three renderings were prototyped: full-bleed lines, a line between the outermost matched objects with a cross at each matched point, and short ticks at matched points with no connecting line. The middle one is what both open-source tools draw, and it lost.

**Choosing full-bleed changes the colour, and this is a consequence rather than taste.** A line crossing the whole viewport puts several times more coloured pixels on screen than one stopping at the outermost match. Excalidraw and tldraw can afford saturated red precisely because their lines are short. Ours is drawn at half intensity for the same legibility, and a saturated value at full-bleed length is genuinely unpleasant.

**A new theme token, not the accent, and not a hard-coded value.** Adding a content-role token follows the precedent set for edge colour, and the theming contract is satisfied rather than amended. The value cannot be the accent: guides only ever appear while something is selected and being dragged, and the selection bounding box is already accent, so a guide in that hue would vanish against the thing it describes. Both open-source tools independently landed on red or pink for this reason and both carry an explicit light and dark pair.

**No numeric distance readout**, following both tools.

**Guides draw in board space, above content and below selection chrome.** One implementation note worth banking, because it changes what was assumed: SVG's `vector-effect="non-scaling-stroke"` gives screen-constant stroke *thickness* under the canvas transform for free, so a guide line needs no `calc(1px / var(--d12-scale))` at all. It does nothing for glyph *size*, so the equal-spacing gap caps still divide by scale. A full-bleed line additionally needs the viewport extent rather than a board-space constant, and the zoom/pan tracker already holds it.

## Numbers, judged rather than reasoned

Tolerance **8 screen pixels**. Both open-source tools arrived there independently and divide by zoom exactly as we do. Screen space rather than board units is not a close call: the threshold is a statement about how precisely a human can aim a pointer, which is a screen-space fact, and it is the same reasoning that already governs the drag threshold and hit-region sizing.

A snap is **sticky to 1.75x tolerance**, so it holds until the pointer pulls nearly twice as far as it took to acquire. Neither reference tool documents this and it was settled by feel.

Velocity cutoff **3 screen pixels per millisecond**. Guide intensity **0.5**.

## The surfaces

Object snapping gets a row in the canvas context menu and **no keyboard chord**.

Ctrl already covers what a chord would serve. People flip snapping mid-task to escape it for one placement, and momentary suppression does that better than a toggle can. What remains for the toggle is a set-and-forget preference, which needs to be reachable rather than fast. The chord budget is also about to get tight, with the keyboard-parity work holding assignments for eight align actions and the context-menu ADR deliberately hinting none of them.

Declining the chord also means no matching host-disable flag, so the library's surface grows by one bindable pair rather than two plus a guard.

## What this amends and what it confirms

**The unbounded-zoom decision is amended in one word**, the snap-to-grid default. Its reasoning is untouched and it stays settled in every other respect.

**The button-semantics decision is amended twice.** A Ctrl+primary press is a secondary press on macOS, behind a platform check, which also repairs the missing menu described above. And `Shift` gains an in-gesture meaning, which that ADR explicitly declined to give it while routing the question here.

**The arbitration model gains two things JavaScript reports**, modifier state on each coalesced move and a pointer velocity. Neither changes the four invokable methods or the threshold rule.

**Live gesture geometry is confirmed, not amended.** Snapping inside the tick, one rigid-body snap per tick, and the commit writing the preview verbatim are all used exactly as written. Its rule that you snap the target coordinate rather than each result is what makes the rigid-body snap work at any selection size.

**The theming contract and the align/distribute model are both untouched.** The first gains a token, which is it working as designed. The second keeps every rule it has, while one cost it priced as a minority case becomes the default case.

## What this deliberately does not decide

**Which point of a selection grid snapping anchors.** Today's implementation snaps the bounds' X and Y, meaning the top-left corner, so an entity whose width is not a multiple of the grid step has its right edge permanently off-grid, and two grid-snapped entities of different widths still fail to line up on facing edges. That was a minority-case wart while grid snapping was opt-in and is now what everyone meets, which is why it is stated here and owed its own decision rather than a clause.

Attachment snapping for edge endpoints belongs to the attachment decision, keyboard behaviour to the keyboard-parity decision, and the route to a buried instance to its own, now knowing that no pointer modifier is available to it.

## Considered and rejected

- **A single combined snapping toggle** — smaller host-facing surface, but it forecloses per-axis composition, which is the useful part.
- **Mutual exclusion between the two toggles**, as Excalidraw does — makes flipping one write to the other's bindable parameter as a side effect.
- **Whole-position precedence**, where object snapping firing on either axis disables grid on both — one rule instead of two, at the cost of a two-axis correction whenever a single-axis match drops out.
- **An inverting Ctrl**, as both open-source tools ship — coherent with one snap mode, incoherent with two at opposite defaults, and able to summon guides for a user who disabled them.
- **Treating a macOS Ctrl+primary press as an ordinary primary press** — keeps the modifier available at press time on both platforms, and leaves macOS users with no context-menu gesture at all.
- **Latching Ctrl at press** — impossible on macOS under the platform check, and unnecessary given snapping has no effect until the pointer moves.
- **Binding Shift to axis-lock on resize** — the key is free because aspect-lock is out of scope, and every tool a user has met makes Shift mean aspect ratio there.
- **Including a `Group`'s own bounds as a candidate** — buys only the group centre, since all four edges already coincide with a member's, and pays for it with a guide through empty space.
- **Excluding locked entities**, mirroring their exclusion from hit-testing and marquee — confuses reference with participation, and locked background frames are the thing people most want to align to.
- **Equal-spacing as a separate ticket** — a later ticket would have to re-derive this one's anchors, tolerance, guides and modifiers before adding anything.
- **Equal-spacing on resize** — tldraw excludes it and the reason holds: the concept does not have an obvious meaning while one object's size changes.
- **Enumerating all candidate pairs and filtering by perpendicular overlap afterwards** — the obvious order, and quadratic per frame.
- **Object snapping on placement and paste** — no preview to draw a guide in, so the correction would be silent.
- **A line between the outermost matched objects with a cross per matched point**, which is what both open-source tools draw — lost the prototype to full-bleed lines.
- **Short ticks with no through-line** — much quieter, but the alignment has to be inferred rather than seen.
- **A numeric distance readout** — neither reference tool shows one.
- **Sizing guide strokes with `calc(1px / var(--d12-scale))`** — correct, and unnecessary once the guide layer is SVG, where non-scaling-stroke does it natively.
- **A keyboard chord for the object-snapping toggle** — spends a key from a tight budget on a preference most users set once, for a job momentary suppression already does better.
