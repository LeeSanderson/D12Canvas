# D12Canvas

A Blazor Razor-component library for an embeddable, local-first diagramming canvas — a Miro-like board of extensible, developer-registered components.

## Language

**Board**:
The full set of a canvas's persisted content — its component instances, groups, and edges — modeled as flat, independently-addressable entities rather than an owned tree. Distinct from canvas chrome (not board content) and from transient view state like zoom/pan (not persisted).

**Component type**:
A registered kind of canvas content (e.g. "sticky note"), identified by a stable key and defined by a rendered component plus a props type.
_Avoid_: shape, node — not used anywhere in this codebase; "component" is the established term.

**Component instance**:
A placed occurrence of a component type on a board, with its own bounds and props value.

**Key**:
The stable string a component type is registered under, chosen independently of its .NET type name so persisted boards survive renames/refactors of the underlying class.
_Avoid_: using the CLR type name as identity.

**Props**:
A component type's own serializable business data (e.g. a sticky note's text and color) — distinct from its bounds.
_Avoid_: parameters — Blazor's own term for a broader concept (includes callbacks, render fragments); "props" specifically means the serializable data payload.

**Bounds**:
A component instance's position and size, tracked uniformly across every component type independent of its props — what lets the canvas query "what's on screen" without knowing any specific component type's shape. Always the *committed* value: it is never written while a `Pointer gesture` is in flight, which is what lets a `Command` read its own before-value straight off the field; what is on screen mid-gesture comes from `Live geometry` instead.

**Entity**:
Any board-content item addressable by a stable GUID assigned at creation — a component instance, a group, or an edge. Entities reference each other only by ID, never by direct ownership, so board content stays flat and independently mergeable.
_Avoid_: node, object — ambiguous with terms already avoided for component instance.

**Group**:
A named collection of component instances and/or nested groups, treated as one movable/resizable unit. Membership is a reference list (`MemberIds`) held by the group, not a back-pointer on each member; a group's bounds are computed from its members on demand, not stored. Layering commands (ADR 0008) applied to a group are bulk writes across member `ZIndex` values, preserving members' relative order — a group has no z-position field of its own.

**Selection**:
The transient, unpersisted set of component instances (and/or groups) currently chosen by the user. Distinct from `Group`: selecting 2+ instances and invoking an explicit "group" action promotes that selection into a `Group`, but selection itself is never serialized, tracked in undo/redo, or part of `Board`.
_Avoid_: conflating with Group — a selection is ephemeral view state, a Group is persisted board content.

**Edge**:
A connection between two endpoints, rendered per its own routing style and arrow settings. Each end is independently one of four shapes: a standard `Port`, a custom `Port`, an `Auto endpoint`, or a fixed board point that tracks nothing. An entity in its own right (`Board.Edges`), but its label — when present — is embedded on the edge rather than a separate entity, since a label has no existence independent of the edge that owns it. Its routing style is independent of what its ends attach to, in deliberate divergence from every reference tool, because the router reads neither (ADR 0027).
_Avoid_: connector, link — "edge" is the established term (already anticipated in `Board`'s and `Entity`'s definitions before it had content of its own).

**Port**:
A named attachment point on a component instance that an edge can connect to, positioned as a fraction of the instance's bounds so it stays correct across resize. Every instance gets four standard ports (top/right/bottom/left, at border centers) automatically; an end user may add further custom ports to a specific instance at runtime — this is instance-scoped runtime state, not something a component type's developer declares at registration. An `Auto endpoint` chooses among the four standard ports only: a custom port is a deliberate placement and never catches an edge nobody aimed at. Drawn as a dot, but grabbed as a stretch of border: its `Hit region` is a span of the `Border partition`, so a connector is pulled from the side rather than aimed at the circle. Visible on a single `Selection` and on the one component under the pointer during a live connector drag, never on hover — and because a `Hit region` and its rendering leave together, **where a port is visible is also where an edge can be pinned rather than auto-attached** (ADR 0028).
_Avoid_: reading the dot as the target — the dot is the visual and the span is the region, and they are deliberately different sizes.

**Border partition**:
How one side of a component instance divides between connecting and resizing, computed per side in screen pixels. The side's **run** is its length minus a corner reserve at each end, so a corner handle is never on a side and two-axis resize can never be covered. Every `Port` on the run takes the stretch nearest to it, capped at the port target; ports closer together than that cap split the difference, and in a below-floor collision a standard port keeps its full width and the custom port is clipped. Resize takes what is left. Anything below the floor drops from render and hit together, so a crowded side can lose its resize spans entirely and a crowded cluster of ports can lose its custom members, both recoverable only by zooming in. One number governs it — the port target, 24 screen pixels, defined in C# and published through `ContentStyle` — with the floor and the corner reserve derived from it. Never drawn: the partition is expressed entirely by the **cursor**, which is one CSS declaration per span because a `Hit region` is already a real element (ADR 0028).
_Avoid_: port strip — that names the old full-length double-click target, which is now one kind of span among several.

**Auto endpoint**:
An edge end attached to a component instance without naming a `Port`, leaving the canvas to keep choosing one. It resolves to the standard port on whichever side an aiming line — from the instance's centre to the other end's own reference point — crosses, so the choice is re-made as either end moves and can never face away from what it connects to. The centre is only ever an aiming reference and never an attachment; the one case with no crossing, the other end being inside the rect, takes the nearest side instead. Produced by dropping a connector drag anywhere on a component's body rather than on a port, and by committing a keyboard port pick without naming a side. Because it consults the other end, resolution is pairwise — the one geometry on `Board` that cannot answer for a single endpoint alone (ADR 0027).
_Avoid_: calling it floating — a floating end tracks nothing, where this one tracks everything.

**Interior edge**:
An edge both of whose endpoints resolve to component instances inside a given set of entities — the test for whether an edge belongs to that set rather than merely touching it. Applied when a selection is copied (only interior edges travel) and again when a payload is pasted (against what actually materialised, so an edge that lost an endpoint to an unresolvable component type is dropped rather than left dangling).

**Canvas chrome**:
UI anchored to a canvas's viewport rather than its pannable/zoomable board surface — it doesn't move when the board pans and isn't part of persisted board content. Two kinds, and the distinction is who places it. **Host-placed** chrome (the palette, the minimap) is a standalone component the host positions with its own CSS, wired to a canvas by reference, so `DiagramCanvas` stays ignorant of it. **Canvas-rendered** chrome (the selection context menu, the `Property bar`) is rendered by `DiagramCanvas` itself as a sibling of `.diagram-canvas`, positioned in plain container-relative pixels; it may derive that position from board geometry without becoming board content (ADR 0021).
_Avoid_: overlay, widget.

**Palette**:
The default canvas chrome component that lists registered component types for the user to pick from.

**Context menu**:
The canvas-rendered chrome a secondary press opens on release, one component with two content sets. The **object menu** carries commands over the selection; the **canvas menu** carries board and view settings. Which rows appear is per-item eligibility rather than a layout per context, so a section renders when any of its members is eligible and unavailable rows are hidden rather than disabled. Placed once at the press point with its anchor corner chosen to open away from the nearest container edge, and it dies on the next press — unlike the persistent `Property bar`, which clamps instead of flipping for exactly that reason (ADR 0023). Also opened by `Shift+F10` or the `ContextMenu` key, where the content set splits on whether anything is selected rather than on what a press hit, it draws at the selection's on-screen box, and focus returns to the tab stop that opened it (ADR 0026). One row is pointer-only by construction rather than by choice: Add port here needs a side and a fraction, which only the stored press point carries, so it is ineligible whenever the menu was opened from the keyboard (ADR 0028).
_Avoid_: right-click menu, popup menu.

**Minimap**:
The canvas chrome component showing the whole board at a glance plus a rect marking the current viewport, so content that has been panned away from stays locatable. Renders one plain box per component instance — never a mounted component or an `LOD placeholder`, and never edges — and maps the union of `Content extent` and the current viewport, so it shows both where content is and where the user is even when those no longer overlap. Holds its own `ZoomPanTracker` and reaches its scale through the same `Framing` computation the viewport commands use. Navigation only: clicking or dragging pans, and never selects or zooms. Carries no keyboard route and is `aria-hidden`, decided rather than defaulted: framing chords and arrow-key pan already make navigation keyboard-complete, so it is a redundant view of state rather than a mouse-only control (ADR 0026).

**Property panel**:
The host-placed canvas chrome that surfaces editable `TProps` fields for the current selection, built generically from each component type's declared editable properties rather than one bespoke panel per type. Holds the full schema, including everything with no `Property role` to put it in the `Property bar`. A component's `Text`-type content is excluded — edited inline/WYSIWYG on the canvas instead. Reads the *expanded* selection, so a selected `Group` is editable through its members (ADR 0021).

**Property bar**:
The canvas-rendered chrome that floats above the current selection carrying the properties judged by eye, drawn as glyphs with no text labels. Supplements the property panel rather than replacing it: the bar shows only properties declaring a `Property role`, the panel keeps the full schema. Anchored by transforming the selection's bounds into container pixels and clamping the result inside the container, sliding along an edge rather than flipping below. Hides for the duration of a pointer gesture and while a context menu is open (ADR 0021).
_Avoid_: context toolbar, floating panel.

**Property role**:
The well-known role a property plays, drawn from a closed library-owned set (`Fill`, `Stroke`, `StrokeWidth`, `TextColour`, `FontSize`, `FontWeight`, `TextAlign`, plus four the library uses for an `Edge`). Declared explicitly by an author, never inferred — a glyph cannot be derived from a property's name or its `EditorKind`, since a rectangle's fill and stroke share a kind and a sticky note's `Color` is a background where a text instance's is a foreground. Each role owns its glyph and declares the `EditorKind` and CLR type it expects, so a mismatch is caught on the registration that causes it. Doing double duty: a role is both what admits a property to the `Property bar` and what merges it across types in a cross-type multi-selection (ADR 0021, replacing the free-string shared tag).

**Editable property**:
A `TProps` field exposed through the property panel, declared by default via an attribute on the `TProps` record and optionally overridden by the registration builder. Carries an `EditorKind` describing which panel control renders it, and optionally a `Property role`.

**EditorKind**:
The kind of control an editable property renders as in the property panel — a closed built-in set (Text, Color, Number, Checkbox, Dropdown, …) plus `Custom`, which takes an author-supplied `RenderFragment<CustomEditorContext>` (the property's current value plus a commit callback) for anything the built-ins can't express. A `Custom`-kind property can only be declared via the registration builder, never a `[PanelEditable]` attribute, since an attribute argument can't carry a RenderFragment.

**Pointer gesture**:
The single behaviour that owns a pointer press, from `pointerdown` to release. Chosen once, at press, from the `Hit target` plus the buttons, modifiers and current `Selection`; its identity never changes mid-press, only its phase — `pointing` until the `Drag threshold` is crossed, `active` after. Owned by one pointer *and one button*: only the claiming button's release ends it, and any other button going down or up meanwhile is dropped (ADR 0022). Only a primary press reads the `Hit target`'s role at all: a secondary or middle press is a `Pan` regardless of what it landed on — and on macOS a `Ctrl`+primary press *is* a secondary press, behind a platform check, because that is the system's own secondary click (ADR 0024). A closed set of eight (`Pan`, `MarqueeSelect`, `MoveSelection`, `ResizeSelection`, `DragEdgeEnd`, `SelectEdge`, `Native`, `MinimapPan`), all owned by `DiagramCanvas` — a `ComponentContainer` holds no gesture state at all, because a container is a box the pointer can leave and a gesture outlives the box. Held by pointer capture on a stable element so release is guaranteed wherever the pointer ends up (ADR 0018).
_Avoid_: confusing with `Gesture`, which is the *history* unit — four pointer gestures (`Pan`, `MarqueeSelect`, `SelectEdge`, `Native`) never produce one, and a `Gesture` can come from the keyboard or a paste with no pointer at all.

**Gesture**:
One completed user-facing action on the board — a drag from press to release, a resize, a prop edit committed on blur, a create, a delete, a group, an ungroup. The unit undo/redo operates on: a gesture becomes exactly one history entry, never one per intermediate frame. A `Pointer gesture` commits at most one of these, at release, in one place — never per intermediate frame, and never from the `pointing` phase.

**Wheel gesture**:
A run of wheel events bounded by a 300ms idle timeout, with `momentum === true` as an early terminator where the engine supplies one. Deliberately the *third* thing called a gesture here and the only one that touches neither of the others: it involves no press, so it is not a `Pointer gesture`, and it never produces a history entry, so it is not a `Gesture` — wheel zoom and pan stay out of undo entirely (ADR 0019). Its boundary exists solely to hold a `Wheel device profile` classification steady for the run.
_Avoid_: treating the idle timeout as an undo-granularity boundary. It was originally specified as one, that consumer no longer exists, and re-tuning it against undo would break classification stability instead.

**Wheel device profile**:
Which of `Auto`, `Mouse` or `Trackpad` a canvas is treating the wheel as coming from, naming one physical fact — **delta granularity**. A mouse notch arrives as a discrete 100px step, a trackpad fractional and fast. Decides three things together: whether a plain wheel zooms or pans, the ambient transform-transition duration (100ms against coarse input, 0ms against fine), and whether Shift binds to horizontal pan at all. `Auto` classifies at `Wheel gesture` start and holds for the run; `Mouse` and `Trackpad` pin it. The host owns any control and any persistence — the library renders neither (ADR 0019).
_Avoid_: reading `Auto` as a heuristic that merely correlates with the device. The integral-versus-fractional tell *is* granularity, and granularity *is* why the smoothing constant exists, so a misclassification still applies smoothing to exactly the input that needs it.

**Gesture preview**:
What the active `Pointer gesture` publishes once per frame while it runs — `Bounds` overrides keyed by component instance id, plus at most one pending edge line (two board points and the id of the edge whose own line is suppressed, absent while a brand-new edge is being drawn). The entities it overrides are that gesture's **participants**. Provisional by definition: `Board` is never written mid-gesture, so cancelling is discarding it and committing is writing it back verbatim, which is what makes a history entry record exactly what was on screen. Covers geometry only — a `Selection` replaced mid-gesture is not restored by discarding it (ADR 0020).
_Avoid_: reading it as a cache of `Board` — only the owning gesture writes it, and it holds only what that gesture changes.

**Live geometry**:
The single read surface that turns board entities into their current board-space points and rectangles, consulting the `Gesture preview` before committed state. It exists to make one rule sayable: read `Bounds` off an entity when you mean committed state — a `Command`'s before-value, persistence — and go through live geometry when you mean what is on screen now. Every derivation exists once with two named entry points, the committed one on `Board` and the live one here; windowed mounting and `Content extent` deliberately take the committed ones, and an `LOD placeholder` swap is frozen for a participant until its gesture releases. Committed mounting is stable across a gesture only because `Pan` and `MinimapPan` are the only pointer gestures that move the viewport — which is why a drag reaching the edge of the viewport stops there instead of scrolling the canvas (ADR 0029). Internal to the library: a host asking `Board` a question means the committed answer.
_Avoid_: effective bounds — names one mechanism's single consumer rather than the question every reader is actually asking.

**Command**:
A recorded, invertible board mutation produced by a gesture — knows how to apply and undo itself. The set is `AddEntity`, `RemoveEntity`, `AddEdge`, `RemoveEdge`, `AddCustomPort`, `ChangeBoundsCommand`, `ChangeEdgeStyleCommand`, `ChangeEdgeLabelCommand`, `ChangeZIndexCommand`, `ChangeLockedCommand`, `MutateEntity`, `Nudge`, `ResizeStep`, `GroupCommand`, `UngroupCommand` and `CompositeCommand`. Every one of them refuses a `Locked` entity — skipping it rather than failing, so a command over a mixed selection still acts on the rest.
_Note_: this list was five short of the code until ADR 0028 counted it, and the guidance below was written when the set was smaller. Whether it is still meaningfully closed is an open question rather than a settled claim.
_Avoid_: inventing a new command type per feature — a generic primitive (especially `MutateEntity` for opaque `Props`) should cover it first.

**History**:
The local, in-memory, session-scoped stack of `Command`s backing undo/redo for the current `Board` — capped at a fixed depth (a circular buffer), never persisted, and never tracked across a reload. Distinct from `Selection`, which is also transient view state but isn't tracked here at all.

**Paste anchor**:
The board point a pasted payload's bounding box is centred on — where the user last *indicated*. That is the pointer's board position when the pointer is over the canvas, the press point that opened a `Context menu` for a paste invoked from its row, and the viewport centre otherwise. A menu opened from the keyboard has no press point and takes the viewport centre, deliberately not a press point stored earlier in the session, which names somewhere the user has since navigated away from (ADR 0026). The payload translates as a rigid body relative to it, so internal relative geometry survives. Successive pastes onto an unchanged anchor cascade by a fixed offset; a changed anchor resets the cascade.

**Grid**:
The canvas's visual position/scale reference — concurrent layers stepping by 10x spacing, crossfading in and out as zoom crosses each layer's legibility threshold to simulate infinite depth in either zoom direction. Purely a `DiagramCanvas` rendering concern; not part of `Board`, not persisted.

**Snap-to-grid**:
An **on-by-default** toggle causing placement/move to snap to the currently-dominant `Grid` layer's spacing. Ephemeral view state, like `Selection` — never part of `Board`, never persisted, regardless of the grid layer it currently tracks. Anchors the selection's top-left corner, so an entity whose size is not a multiple of the step keeps its far edges off-grid (ADR 0024).

**Object snapping**:
An off-by-default toggle aligning a moved or resized `Selection`'s edges and centres to those of neighbouring entities, drawing an `Alignment guide` along whatever matched. Resolves **per axis** and beats `Snap-to-grid` on any axis where it fires, leaving grid to fill the other. Its tolerance is 8 *screen* pixels, because the number states how precisely a human can aim a pointer rather than anything about the board. Candidates are the on-screen entities — a guide to something invisible is a line to nowhere — including `Locked` ones, since snapping is reference rather than participation, and excluding `Group`s, whose every bounds edge already coincides with a member's so that only their centre would be new (ADR 0024).
_Avoid_: smart guides, snap lines — those name the visual, which is the `Alignment guide`.

**Alignment guide**:
The line drawn along a coordinate `Object snapping` matched, spanning the whole viewport rather than stopping at the outermost matched entity. Canvas-painted board-space chrome reading its own `Theme token`, deliberately never the accent, since a guide only ever appears while a selection is being dragged and would vanish against the selection's own box. Drawn at reduced intensity precisely because it is full-bleed: a viewport-length line puts far more colour on screen than one bounded by its matches.

**Equal-spacing snap**:
An `Object snapping` match that reproduces a gap already present elsewhere, so a new entity dropped beside a row lands at the row's own rhythm. A gap exists only between two entities overlapping on the perpendicular axis. Moves only, never resize — equal spacing is a claim about layout *between* entities and has no obvious meaning while one entity's size is changing.

**Axis lock**:
Holding `Shift` through a move, discarding motion on one axis so the `Selection` travels in a straight line. The axis follows the press-anchored delta and is re-read every move. The locked axis is exempt from all snapping, since rounding it to a grid line would move the selection along the very axis just locked. Moves only: `Shift` stays unbound on resize, where every tool a user has met makes it mean preserve-aspect-ratio (ADR 0024).

**Nudge**:
An arrow-key move of the `Selection`, one `History` entry per press-to-release burst however many repeat keydowns it spans. The step is one screen pixel divided by scale with `Snap-to-grid` off, and the distance to the **next** dominant `Grid` line with it on — next-line rather than plus-one-spacing, so an off-grid entity is repaired on the first press instead of jumping in grid-sized steps that never land. `Shift` multiplies by ten, which under grid snapping is one cell of the next coarser layer. Never object-snaps: unlike a drag there is no continuous motion to make the pull legible, and the align commands are the keyboard's exact answer (ADR 0026).
_Avoid_: arrow-key move, keyboard nudge — this is the only nudge there is.

**Shortcut hint**:
The chord rendered beside a `Context menu` row. Shown only while that binding is live, so a row whose chord a host has disabled shows none, and rows with no chord at all — the align strip, Unlock All, object snapping — show none by design rather than omission. Rendered from one platform boolean cached at init and shared with the pointer path's macOS check: Apple platforms concatenate `⌃⌥⇧⌘` symbols with no separator, everywhere else joins words with `+`. Two conventions, not one string with a modifier substituted (ADR 0026).

**Content extent**:
The smallest `Bounds` enclosing everything on a board — every component instance's bounds unioned with every resolvable edge endpoint, since an `Edge` with floating endpoints is content that no component's bounds covers. Derived on demand from `Board`, never stored, and null for a genuinely empty board. Restricting the same computation to a `Selection` is what zoom-to-selection frames.
_Avoid_: board extent — ADR 0011 abolished a fixed board size; this is measured from content, not a configured limit.

**Framing**:
Moving the viewport so a given board rect fills it — the shared operation behind zoom-to-fit, zoom-to-selection and the `Minimap`'s own scale, and the inverse of `ZoomPanTracker.Viewport`. Sets scale and pan together in one change, contain-not-cover, centred, inset by a fixed fraction, and never magnifying past 1.0. The destination is computed rather than interpolated: state arrives at once and the browser animates the transform, so nothing in the model has a notion of time.
_Avoid_: zoom — framing always sets pan as well, and is a discrete destination rather than an increment.

**LOD placeholder**:
The generic stand-in rendered for a component instance once its on-screen size (`Bounds` × current zoom scale) drops below a configurable threshold — swaps out the full Razor component tree for a plain box built from data the registration contract already requires (`DisplayName`/`Icon`), rather than mounting every instance at full cost regardless of how small it renders. Cheap to render, not inert: it is a full `Hit target` and an ordinary tab stop, since the cost it exists to avoid is *mounting*, and a board whose content stopped being selectable when zoomed out would be unusable at exactly the zoom where grabbing a cluster matters most (ADR 0017).
_Avoid_: reading it as non-interactive — ADR 0011 swaps the mounted component tree, never the entity's reachability.

**Hit target**:
What a pointer press resolves to — a `(role, entity, part)` classification produced once, at press, by walking up the DOM from the event's own target to the nearest marked element, carried alongside the press count, `pointerType`, buttons and modifiers. The role is a closed set (`instance`, `resize-handle`, `port`, `port-strip`, `edge`, `edge-endpoint`, `edge-label`, `selection-bounds`, `selection-handle`, `author-content`, `canvas`), and `canvas` means the walk found nothing rather than that nothing was there. Resolved in JavaScript because Blazor's `MouseEventArgs` carries no event target, which is why arbitration is otherwise forced out into per-element handlers. The role alone decides everything that must happen synchronously — `preventDefault`, `setPointerCapture`, focus transfer, the `Drag threshold`, and suppressing the browser's native context menu — so no selection state is ever mirrored into JavaScript; choosing the `Pointer gesture` needs the selection and therefore happens in C#, after the hop. Only a *primary* press consumes the role: a secondary or middle press is a `Pan` whatever it hit.
_Avoid_: hit result, pick — "target" is the reference-tool term and the one the classification's own role names describe.

**Hit region**:
The area that resolves to a `Hit target`, expressed as a real element and deliberately not the same shape as what is drawn — an edge's visible stroke is 2 units wide while its region is 20 screen pixels. Where the two differ, the region is the participating element and the visual is painted by a non-participant. Sized against `--d12-scale` so it stays constant in *screen* pixels at any zoom, and always attached to something visible: a region may exceed its visual, but nothing is hittable with no visual at all — a port span's visual is its dot, a resize span's is the instance's own border. Whether an entity has a region at all is one predicate in C#, read both by the markup that emits the region and by the marquee, so the two cannot drift. Affordance regions on an instance's border are additionally guaranteed **not to intersect**, at any zoom and for any port configuration, which is what the `Border partition` exists to produce and what replaced an earlier precedence rule declaring resize handles the winner over ports (ADR 0028).
_Avoid_: hit box — regions are not all rectangular (an edge's is a stroke).

**Drag threshold**:
The distance a press must travel before it counts as a drag rather than a click — 4 pixels, measured in *screen* pixels so the same physical hand movement crosses it at any zoom. It is what lets one press safely carry two meanings: a `Pointer gesture` releasing from its `pointing` phase runs the click outcome (select, or open the context menu), and crossing the threshold promotes it to `active`. One number for every role and both buttons; JavaScript owns it and does not call C# below it, so any move C# receives is already a real drag. There is deliberately no time-based equivalent: a stationary press never promotes, however long it is held (ADR 0022).
_Avoid_: treating it as per-gesture tuning — the only dimension it is ever expected to vary by is `pointerType`.

**Locked**:
An entity's opt-in protection from change — no `Command` modifies a locked `Component instance` or `Edge`, and it takes no part in primary-press hit-testing or marquee selection. A *secondary* press is the one pointer route that does reach it, so right-clicking a locked entity selects it and offers the way to unlock it. A `Group` holds no flag of its own: locking one is a bulk write across its members and "is this group locked?" is derived from them, exactly as its bounds are. Persisted, undoable, and absent by default. Deliberately orthogonal to keyboard reachability — a locked entity stays tab-reachable and appears in the `Property panel` with its fields disabled and an unlock control live, which is what stops it being locked forever.
_Avoid_: keying pointer participation and keyboard reachability off one condition — ADR 0017 separates them precisely so locking cannot become an accessibility failure.

**Theme token**:
A named CSS custom property in the shared set that everything the library itself paints reads for its default visual values (surface, border, accent, muted text, …), rather than each element declaring one-off properties. Every canvas-chrome element (`Grid`, `LOD placeholder`, `Palette`, selection marquee, connector drag-preview, context menu, `Alignment guide`) reads them, and so does library-painted *board content* — an `Edge`, which has no author component behind it. Declared independently on each chrome component's own root — not a single global `:root` — so every component works standalone and two canvas instances can carry different themes; a host overriding tokens on a shared ancestor gets one consistent theme across both via ordinary CSS inheritance.
_Avoid_: reading the boundary as chrome versus content — it is **who renders the pixels** (ADR 0016). A component instance's visual fields are ordinary business-data props with no theming model (ADR 0008) because an author's own component paints them and the library cannot reach inside, not because they are content.

**Edge colour**:
An `Edge`'s optional own colour, held as board data alongside its routing style and arrowheads. Absent by default, and absence means *no author opinion* rather than a value — it resolves to a `Theme token` at paint time, so an edge nobody has coloured is correct on both themes. An author's colour overrides the token for that edge only; `Selection` overrides both, because selection feedback is transient UI state the library paints.
_Avoid_: treating it as a theme setting — it is per-edge data that persists with the board, and the library makes no legibility guarantee about a value an author chose.

**Interaction probe**:
A test that drives a real browser and asserts DOM or component state rather than comparing pixels against a committed baseline. It shares a project and a fixture with the visual tests but has none of their font sensitivity, and it exists to reach what only the browser owns: the `Hit target` walk, the `Drag threshold`, the synchronous decisions taken before the interop hop, and the release channels. Its remit is strictly plumbing — that a modifier survived the trip, that a default was prevented, that ten pointer moves in one frame arrive as one call. It never establishes a magnitude, because synthetic input does not reproduce a real device's granularity.
_Avoid_: calling it a visual test — the whole project is named for those, and the distinction is what a failure means: a pixel diff asks a human to look, a probe names a broken invariant.

**Release-reliability case**:
A test that starts one `Pointer gesture`, ends it the way a user plausibly might, then moves the pointer with no button held and asserts nothing responded — a response to a buttonless pointer being the definition of a leaked gesture. One exists per member of the closed set of eight, enumerated by that set rather than written per ticket, so adding a ninth gesture fails the suite until its case exists. Blind by construction to `SelectEdge` and `Native`, which are the two members that hold no state and therefore cannot leak.
_Avoid_: asserting the leak by inspecting which gesture is live — there is no observation surface for that, deliberately, and the behavioural form needs none.
