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
A component instance's position and size, tracked uniformly across every component type independent of its props — what lets the canvas query "what's on screen" without knowing any specific component type's shape.

**Entity**:
Any board-content item addressable by a stable GUID assigned at creation — a component instance, a group, or an edge. Entities reference each other only by ID, never by direct ownership, so board content stays flat and independently mergeable.
_Avoid_: node, object — ambiguous with terms already avoided for component instance.

**Group**:
A named collection of component instances and/or nested groups, treated as one movable/resizable unit. Membership is a reference list (`MemberIds`) held by the group, not a back-pointer on each member; a group's bounds are computed from its members on demand, not stored. Layering commands (ADR 0008) applied to a group are bulk writes across member `ZIndex` values, preserving members' relative order — a group has no z-position field of its own.

**Selection**:
The transient, unpersisted set of component instances (and/or groups) currently chosen by the user. Distinct from `Group`: selecting 2+ instances and invoking an explicit "group" action promotes that selection into a `Group`, but selection itself is never serialized, tracked in undo/redo, or part of `Board`.
_Avoid_: conflating with Group — a selection is ephemeral view state, a Group is persisted board content.

**Edge**:
A connection between two ports (or a floating point), rendered per its own routing style and arrow settings. An entity in its own right (`Board.Edges`), but its label — when present — is embedded on the edge rather than a separate entity, since a label has no existence independent of the edge that owns it.
_Avoid_: connector, link — "edge" is the established term (already anticipated in `Board`'s and `Entity`'s definitions before it had content of its own).

**Port**:
A named attachment point on a component instance that an edge can connect to, positioned as a fraction of the instance's bounds so it stays correct across resize. Every instance gets four standard ports (top/right/bottom/left, at border centers) automatically; an end user may add further custom ports to a specific instance at runtime — this is instance-scoped runtime state, not something a component type's developer declares at registration.

**Interior edge**:
An edge both of whose endpoints resolve to component instances inside a given set of entities — the test for whether an edge belongs to that set rather than merely touching it. Applied when a selection is copied (only interior edges travel) and again when a payload is pasted (against what actually materialised, so an edge that lost an endpoint to an unresolvable component type is dropped rather than left dangling).

**Canvas chrome**:
UI anchored to a canvas's viewport rather than its pannable/zoomable board surface — it doesn't move when the board pans and isn't part of persisted board content. The palette and the minimap are the examples; each is a standalone component the host places and positions itself, so `DiagramCanvas` stays ignorant of both.
_Avoid_: overlay, widget.

**Palette**:
The default canvas chrome component that lists registered component types for the user to pick from.

**Minimap**:
The canvas chrome component showing the whole board at a glance plus a rect marking the current viewport, so content that has been panned away from stays locatable. Renders one plain box per component instance — never a mounted component or an `LOD placeholder`, and never edges — and maps the union of `Content extent` and the current viewport, so it shows both where content is and where the user is even when those no longer overlap. Holds its own `ZoomPanTracker` and reaches its scale through the same `Framing` computation the viewport commands use. Navigation only: clicking or dragging pans, and never selects or zooms.

**Property panel**:
The canvas-chrome component that surfaces editable `TProps` fields for the current selection, built generically from each component type's declared editable properties rather than one bespoke panel per type. A component's `Text`-type content is excluded — edited inline/WYSIWYG on the canvas instead.

**Editable property**:
A `TProps` field exposed through the property panel, declared by default via an attribute on the `TProps` record and optionally overridden by the registration builder. Carries an `EditorKind` describing which panel control renders it.

**EditorKind**:
The kind of control an editable property renders as in the property panel — a closed built-in set (Text, Color, Number, Checkbox, Dropdown, …) plus `Custom`, which takes an author-supplied `RenderFragment<CustomEditorContext>` (the property's current value plus a commit callback) for anything the built-ins can't express. A `Custom`-kind property can only be declared via the registration builder, never a `[PanelEditable]` attribute, since an attribute argument can't carry a RenderFragment.

**Gesture**:
One completed user-facing action on the board — a drag from press to release, a resize, a prop edit committed on blur, a create, a delete, a group, an ungroup. The unit undo/redo operates on: a gesture becomes exactly one history entry, never one per intermediate frame.

**Command**:
A recorded, invertible board mutation produced by a gesture — knows how to apply and undo itself. A small closed set (`AddEntity`, `RemoveEntity`, `ChangeBoundsCommand`, `ChangeEdgeStyleCommand`, `ChangeEdgeLabelCommand`, `ChangeZIndexCommand`, `ChangeLockedCommand`, `MutateEntity`, `GroupCommand`, `UngroupCommand`, `CompositeCommand`), not one bespoke class per gesture type. Every one of them refuses a `Locked` entity — skipping it rather than failing, so a command over a mixed selection still acts on the rest.
_Avoid_: inventing a new command type per feature — a generic primitive (especially `MutateEntity` for opaque `Props`) should cover it first.

**History**:
The local, in-memory, session-scoped stack of `Command`s backing undo/redo for the current `Board` — capped at a fixed depth (a circular buffer), never persisted, and never tracked across a reload. Distinct from `Selection`, which is also transient view state but isn't tracked here at all.

**Paste anchor**:
The board point a pasted payload's bounding box is centred on — the pointer's board position when the pointer is over the canvas, the viewport centre otherwise. The payload translates as a rigid body relative to it, so internal relative geometry survives. Successive pastes onto an unchanged anchor cascade by a fixed offset; a changed anchor resets the cascade.

**Grid**:
The canvas's visual position/scale reference — concurrent layers stepping by 10x spacing, crossfading in and out as zoom crosses each layer's legibility threshold to simulate infinite depth in either zoom direction. Purely a `DiagramCanvas` rendering concern; not part of `Board`, not persisted.

**Snap-to-grid**:
An optional, off-by-default toggle causing placement/move to snap to the currently-dominant `Grid` layer's spacing. Ephemeral view state, like `Selection` — never part of `Board`, never persisted, regardless of the grid layer it currently tracks.

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
What a pointer press resolves to — a `(role, entity, part)` classification produced once, at press, by walking up the DOM from the event's own target to the nearest marked element. The role is a closed set (`instance`, `resize-handle`, `port`, `port-strip`, `edge`, `edge-endpoint`, `edge-label`, `selection-bounds`, `selection-handle`, `author-content`, `canvas`), and `canvas` means the walk found nothing rather than that nothing was there. Resolved in JavaScript because Blazor's `MouseEventArgs` carries no event target, which is why arbitration is otherwise forced out into per-element handlers.
_Avoid_: hit result, pick — "target" is the reference-tool term and the one the classification's own role names describe.

**Hit region**:
The area that resolves to a `Hit target`, expressed as a real element and deliberately not the same shape as what is drawn — an edge's visible stroke is 2 units wide while its region is 20 screen pixels. Where the two differ, the region is the participating element and the visual is painted by a non-participant. Sized against `--d12-scale` so it stays constant in *screen* pixels at any zoom, and always attached to something visible: a region may exceed its visual, but nothing is hittable with no visual at all. Whether an entity has a region at all is one predicate in C#, read both by the markup that emits the region and by the marquee, so the two cannot drift.
_Avoid_: hit box — regions are not all rectangular (an edge's is a stroke).

**Locked**:
An entity's opt-in protection from change — no `Command` modifies a locked `Component instance` or `Edge`, and it takes no part in pointer hit-testing or marquee selection. A `Group` holds no flag of its own: locking one is a bulk write across its members and "is this group locked?" is derived from them, exactly as its bounds are. Persisted, undoable, and absent by default. Deliberately orthogonal to keyboard reachability — a locked entity stays tab-reachable and appears in the `Property panel` with its fields disabled and an unlock control live, which is what stops it being locked forever.
_Avoid_: keying pointer participation and keyboard reachability off one condition — ADR 0017 separates them precisely so locking cannot become an accessibility failure.

**Theme token**:
A named CSS custom property in the shared set that everything the library itself paints reads for its default visual values (surface, border, accent, muted text, …), rather than each element declaring one-off properties. Every canvas-chrome element (`Grid`, `LOD placeholder`, `Palette`, selection marquee, connector drag-preview, context menu) reads them, and so does library-painted *board content* — an `Edge`, which has no author component behind it. Declared independently on each chrome component's own root — not a single global `:root` — so every component works standalone and two canvas instances can carry different themes; a host overriding tokens on a shared ancestor gets one consistent theme across both via ordinary CSS inheritance.
_Avoid_: reading the boundary as chrome versus content — it is **who renders the pixels** (ADR 0016). A component instance's visual fields are ordinary business-data props with no theming model (ADR 0008) because an author's own component paints them and the library cannot reach inside, not because they are content.

**Edge colour**:
An `Edge`'s optional own colour, held as board data alongside its routing style and arrowheads. Absent by default, and absence means *no author opinion* rather than a value — it resolves to a `Theme token` at paint time, so an edge nobody has coloured is correct on both themes. An author's colour overrides the token for that edge only; `Selection` overrides both, because selection feedback is transient UI state the library paints.
_Avoid_: treating it as a theme setting — it is per-edge data that persists with the board, and the library makes no legibility guarantee about a value an author chose.
