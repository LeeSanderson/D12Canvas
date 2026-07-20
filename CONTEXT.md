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

**Canvas chrome**:
UI anchored to a canvas's viewport rather than its pannable/zoomable board surface — it doesn't move when the board pans and isn't part of persisted board content. The palette is the first example; a minimap would be another.
_Avoid_: overlay, widget.

**Palette**:
The default canvas chrome component that lists registered component types for the user to pick from.

**Property panel**:
The canvas-chrome component that surfaces editable `TProps` fields for the current selection, built generically from each component type's declared editable properties rather than one bespoke panel per type. A component's `Text`-type content is excluded — edited inline/WYSIWYG on the canvas instead.

**Editable property**:
A `TProps` field exposed through the property panel, declared by default via an attribute on the `TProps` record and optionally overridden by the registration builder. Carries an `EditorKind` describing which panel control renders it.

**EditorKind**:
The kind of control an editable property renders as in the property panel — a closed built-in set (Text, Color, Number, Checkbox, Dropdown, …) plus `Custom`, which takes an author-supplied `RenderFragment<TProps>` for anything the built-ins can't express.

**Gesture**:
One completed user-facing action on the board — a drag from press to release, a resize, a prop edit committed on blur, a create, a delete, a group, an ungroup. The unit undo/redo operates on: a gesture becomes exactly one history entry, never one per intermediate frame.

**Command**:
A recorded, invertible board mutation produced by a gesture — knows how to apply and undo itself. A small closed set (`AddEntity`, `RemoveEntity`, `ChangeBoundsCommand`, `MutateEntity`, `GroupCommand`, `UngroupCommand`, `CompositeCommand`), not one bespoke class per gesture type.
_Avoid_: inventing a new command type per feature — a generic primitive (especially `MutateEntity` for opaque `Props`) should cover it first.

**History**:
The local, in-memory, session-scoped stack of `Command`s backing undo/redo for the current `Board` — capped at a fixed depth (a circular buffer), never persisted, and never tracked across a reload. Distinct from `Selection`, which is also transient view state but isn't tracked here at all.
