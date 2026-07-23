# D12Canvas Next: Local-First Diagramming Platform

Status: ready-for-agent

Synthesized from the resolved `d12canvas-next` wayfinder effort ([map](map.md), tickets 01–18). The foundational decisions below are locked in ADRs 0001–0012 (`docs/adr/`); this spec is the feature-level view over them.

## Problem Statement

A Blazor developer who wants a Miro-like diagramming surface inside their app has no batteries-included option. Today's D12Canvas is a thin prototype — a pan/zoom viewport plus a draggable/resizable container for arbitrary content. There is no way to register component types and pick them from a palette, no data model for what's on the board, no way to save or load a board, no connectors, no multi-select or grouping, no undo, no property editing, no keyboard access, and hard-coded limits on canvas size and zoom. A host app that adopts it must build essentially the whole diagramming experience itself.

End users of such a host app correspondingly can't do the things they expect from a diagramming tool: place shapes from a picker, connect them, group them, edit their properties, undo mistakes, work at any scale, or use the board without a mouse.

## Solution

The next version of D12Canvas: a reusable, embeddable Blazor Razor-component library providing a complete local-first, single-user diagramming experience out of the box, while staying radically extensible.

Host developers register **component types** (their own Razor components plus a serializable **props** type) under stable string **keys**; a default **palette** chrome component lets end users place instances by drag-and-drop or click-to-add. Board content — component instances, **groups**, and **edges** — lives in a flat, ID-keyed **Board** model that a host can serialize to versioned JSON and store wherever it likes. End users get edges with **ports**, marquee/shift-click **selection**, persistent grouping, four built-in component types, gesture-level undo/redo, a generic **property panel**, layering commands, a context menu and keyboard shortcuts, full mouse-free operation, unbounded board extent and zoom with an **LOD placeholder** and adaptive **grid**, and light/dark theming of all library chrome via **theme tokens**.

Real-time collaboration is not built, but the state model, component contract, and persistence format deliberately keep it cheap to add later.

## User Stories

**Component registration & extensibility**

1. As a component author, I want to register a component type through DI under a stable string key, so that persisted boards survive renames and refactors of my .NET classes.
2. As a component author, I want to declare a required `DisplayName`, `AccessibleName`, and `DefaultProps` for my component type, so that the palette and canvas can present and instantiate it without bespoke wiring.
3. As a component author, I want to optionally declare an `Icon`, ARIA `Role`, `DefaultSize`, and `Category`, so that my type looks and behaves polished in the palette and on the board without those being mandatory.
4. As a component author, I want duplicate key registrations rejected at registration time, so that a misconfiguration surfaces at startup instead of as a corrupted board.
5. As a component author, I want my props type kept structurally separate from bounds, so that I model only my business data and the canvas handles position/size uniformly.
6. As a component author, I want my declared `AccessibleName` applied as ARIA metadata automatically, so that my component is screen-reader-announceable without me writing accessibility plumbing.
7. As a component author, I want the contract to assume skip-if-unchanged rendering as standard, so that my components stay cheap when mounted in large boards.

**Palette & placement**

8. As a host developer, I want a default palette chrome component that lists registered component types, so that the tool feels usable out of the box without me building a picker.
9. As a host developer, I want the palette (and any future chrome) to be a standalone component positioned by my own CSS, outside the pannable board surface, so that it fits my app's layout.
10. As an end user, I want palette entries showing each type's icon and display name, grouped by category, so that I can find the component I need quickly.
11. As an end user, I want to drag a type from the palette and drop it on the board at the drop point, so that placement feels direct.
12. As an end user, I want to click a palette entry to instantly add an instance at the viewport centre (with a cascading offset for repeats), so that I can place components without a drag.
13. As an end user, I want newly placed instances to use the type's registered default size and default props, so that every placement starts in a sensible state.

**Board & state model**

14. As a host developer, I want board content modeled as flat, independently-addressable entities with GUID identity (no ownership tree), so that future multi-user merging isn't foreclosed by the data model.
15. As a host developer, I want a viewport query with a tunable overscan margin, so that only components near the visible area are mounted and large boards stay responsive.
16. As an end user, I want boards with hundreds of components to pan and zoom smoothly, so that scale doesn't degrade the experience.

**Persistence & import/export**

17. As a host developer, I want to serialize a board to a versioned JSON envelope and deserialize it back via an injectable service, so that I can store boards in whatever medium my app uses.
18. As a host developer, I want a strict deserialize path that throws and a partial path that returns warnings (e.g. for unknown component-type keys), so that I choose how to handle imperfect input.
19. As a host developer, I want props round-tripped by registry-resolved binding rather than CLR-type discriminators, so that saved boards stay stable across refactors and don't embed .NET type names.
20. As a host developer, I want import/export to be exactly that JSON round-trip with no library-shipped trigger UI, so that I control storage, timing, and UX.
21. As an end user, I want a saved board to reload with all instances, groups, edges, and their properties intact, so that my work is durable.

**Edges & ports**

22. As an end user, I want every component instance to have four standard ports at its border centres, so that I can connect anything without setup.
23. As an end user, I want ports positioned fractionally within an instance's bounds, so that connections stay attached correctly through move and resize.
24. As an end user, I want to add custom ports to a specific instance at runtime, so that I can attach edges exactly where I need them.
25. As an end user, I want to draw an edge by dragging from a port to another port, so that connecting feels direct.
26. As an end user, I want a built-in "Connector" palette entry that drops a floating edge, so that I can also start from the palette.
27. As an end user, I want edge endpoints to be able to float unattached, so that I can sketch connections before both ends exist.
28. As an end user, I want routing style and arrowheads chosen per edge, so that one board can mix connection styles.
29. As an end user, I want edge labels that are rich content embedded on the edge (defaulting to text), so that I can annotate connections without managing a separate object.
30. As an end user, I want edges to attach only to component-instance ports (never to a group), so that grouping and ungrouping can never orphan a connection.

**Selection & grouping**

31. As an end user, I want to multi-select via an intersection-based marquee and shift-click toggling, so that I can operate on ad-hoc sets.
32. As an end user, I want to move and resize a multi-selection as one bounding-box unit, so that arrangements keep their relationships.
33. As an end user, I want an explicit "group" action that promotes a 2+ selection into a persistent group, so that deliberate structures survive beyond the current session.
34. As an end user, I want clicking any member of a group to select the whole group, so that grouped content behaves as one unit.
35. As an end user, I want an ungroup action that dissolves a group back into its members, so that grouping is reversible.
36. As an end user, I want selection to be transient view state — never saved with the board — so that loading a board doesn't resurrect someone's old selection.

**Built-in component types**

37. As an end user, I want Rectangle, Sticky Note, Text, and Image components available out of the box, so that I can diagram immediately without any host-side registration.
38. As an end user, I want each built-in to have its own sensible default size, default props, icon, and "Basic Shapes" palette category, so that the defaults feel considered.
39. As a component author, I want the built-ins registered through the same public mechanism as my own types, so that the extension API is proven by the library's own components.
40. As an end user, I want each built-in's visual settings (fill, stroke, font, …) to be ordinary editable props, so that styling a shape works like editing any other property.

**Undo/redo**

41. As an end user, I want undo/redo to operate on whole gestures — a completed drag, a resize, a committed prop edit, a create, a delete, a group/ungroup — so that one Ctrl+Z reverses one action, never one frame of it.
42. As an end user, I want multi-entity gestures (moving a selection, deleting a group) undone atomically, so that undo never leaves half an action applied.
43. As an end user, I want a deep but bounded history (capped, oldest dropped first), so that undo is generous without growing unbounded.
44. As a host developer, I want history kept in-memory and session-scoped, never persisted, so that saved boards stay clean data.

**Property panel & layering**

45. As an end user, I want a property panel showing the editable properties of my current selection, so that I can adjust a component without bespoke UI per type.
46. As a component author, I want to declare editable properties via attributes on my props type, with builder-level overrides, so that panel support is nearly free but still customizable.
47. As a component author, I want each editable property to carry an `EditorKind` from a built-in control set, plus a `Custom` render-fragment escape hatch, so that common cases are trivial and uncommon ones possible.
48. As an end user, I want a component's main text content edited inline on the canvas (WYSIWYG) rather than in the panel, so that writing feels direct.
49. As an end user, I want multi-selecting across component types to expose only explicitly-shared properties for bulk edit, so that cross-type editing is predictable.
50. As an end user, I want bring-to-front / send-to-back / forward / backward commands, so that I control stacking; and I want new instances to appear on top.
51. As an end user, I want layering applied to a group to move all members together while preserving their relative order, so that grouped stacks stay coherent.

**Interaction, shortcuts & context menu**

52. As an end user, I want placement, connecting, and selecting to work without switching persistent tool modes, so that I never have to remember which mode I'm in.
53. As an end user, I want keyboard shortcuts for delete, escape, undo/redo, group/ungroup, and layering, so that frequent actions don't need the mouse or menu.
54. As an end user, I want a right-click context menu on a selection mirroring those actions, so that they're discoverable; and no menu on empty canvas, so that right-click stays unsurprising.

**Keyboard accessibility**

55. As a keyboard user, I want to Tab through board content in reading order, with a group collapsing to a single stop, so that I can reach everything without a mouse.
56. As a keyboard user, I want focus to follow selection (and vice versa), so that keyboard and pointer interaction stay coherent.
57. As a keyboard user, I want arrow keys to move the focused instance by a zoom-relative step, so that nudging is precise at any magnification.
58. As a keyboard user, I want Alt+Arrow to grow/shrink the focused instance per-axis with stable anchoring, so that I can resize without a pointer.
59. As a keyboard user, I want Ctrl+Tab plus Space to build a multi-selection from the keyboard, so that group operations aren't mouse-only.
60. As a keyboard user, I want to place components and attach connectors entirely from the keyboard using the same focus and nudge mechanics, so that no core workflow requires a pointer.
61. As a screen-reader user, I want every instance announced by its accessible name and role, with selection state exposed via `aria-selected`, so that the board is comprehensible non-visually.

**Scale, zoom, LOD & grid**

62. As an end user, I want board extent and zoom range unbounded by default, so that the canvas never walls off my work; and as a host developer, I want optional configurable min/max zoom limits when my embedding needs them.
63. As an end user, I want distant/small components swapped for a generic placeholder (built from the type's display name/icon) past a configurable size threshold, so that zoomed-out views of dense boards stay fast and legible.
64. As an end user, I want an adaptive multi-layer grid whose layers step by 10x and crossfade as zoom crosses each legibility threshold, so that I always have a position/scale reference at any depth.
65. As an end user, I want an off-by-default snap-to-grid toggle (parameter and Ctrl+' shortcut) snapping to the currently dominant grid layer, so that alignment help is there when I want it and absent when I don't.

**Theming**

66. As a host developer, I want all library-owned chrome (grid, LOD placeholder, palette, marquee, connector drag-preview, context menu) styled through a shared CSS custom-property token set, so that I can retheme everything by overriding tokens in plain CSS with no C# API.
67. As an end user, I want default light and dark themes following `prefers-color-scheme`, so that the canvas matches my system without host effort.
68. As a host developer, I want a `data-d12-theme` attribute override, so that my app's own theme switcher wins over the OS preference.
69. As a host developer, I want token defaults declared on each chrome component's own root (not a global `:root`), so that every component works standalone and two canvas instances on one page can carry different themes.

## Implementation Decisions

All foundational decisions are locked as ADRs 0001–0012; the domain vocabulary is in the root `CONTEXT.md`. This is a rethink of the library, not a continuation — no behavior of the current prototype is contractual, though its pan/zoom interop and container mechanics are starting material.

- **Rendering** (settled): DOM/CSS with absolutely-positioned elements — the only approach that can host live, interactive Razor components as board content. Canvas/WebGL/SVG rejected.
- **Registration contract** (ADR 0001): DI-based registry keyed by stable string, decoupled from CLR type names. Required per type: `DisplayName`, `AccessibleName` (auto-applied as ARIA), `DefaultProps`. Optional: `Icon`, `Role` (default `"group"`), `DefaultSize`, `Category`. Duplicate keys rejected. `TProps` structurally separate from bounds.
- **Canvas chrome** (ADR 0002): palette (and future chrome like a minimap) is a separate component category outside the pannable board surface, reference-wired to the canvas and positioned by host CSS.
- **State model** (ADR 0003): `Board` = flat, ID-keyed collections of `Components`/`Groups`/`Edges`; GUID entity identity; references by ID only, no ownership tree (collaboration-readiness). `ComponentInstance` = component-type key + boxed props + bounds + explicit `ZIndex`. Entities are mutable. `Group` holds `MemberIds`; its bounds are computed, not stored. Viewport query surface is `Board.GetVisible(viewport, overscan)` — a linear scan now, with the spatial-index implementation deferred until profiling demands it (reasoned DOM ceiling: ~200–500 concurrently-interactive elements unmitigated; windowing measured at 8–12x fps gains).
- **Rendering discipline**: windowing recomputed on pan/zoom/resize (not per frame); components implement skip-if-unchanged `ShouldRender` as the standard contract; pan-driven re-render is throttled.
- **Persistence** (ADR 0004): format only — the host owns all storage I/O and all import/export trigger UI. Versioned JSON envelope (`SchemaVersion` + entity arrays) via System.Text.Json. Props round-trip by two-phase deserialize: generic parse leaving props as raw JSON, then registry-resolved bind by component-type key — no CLR-type discriminators. Injectable serializer service with strict (throws) and partial (returns warnings) paths. `SchemaVersion` reserved now; no migration pipeline until a real V2 exists.
- **Edges & ports** (ADR 0005, narrowed by ticket 15): four automatic border-centre ports per instance, fractional within bounds; user-added custom ports are instance-scoped runtime state (no registration-contract change). Endpoints may float. Routing style and arrows are per-edge. Labels are rich but embedded on the edge, not separate entities. Edges never attach to a `Group` — only to instance ports or floating.
- **Selection & grouping** (ADR 0006): selection is transient, unpersisted view state, never on `Board`, never in history. Marquee is intersection-based; shift-click toggles. Multi-selection moves and resizes as a bounding-box unit. An explicit group action promotes a 2+ selection to a persistent `Group`; thereafter clicking a member selects the group. Group-entry interaction (e.g. double-click into a group) is deferred to implementation — ungroup is the guaranteed exit.
- **Built-ins** (ticket 10, no new ADR): Rectangle, Sticky Note, Text, Image — each with its own props type carrying its visual fields as ordinary props, its own default size/props/icon, `Category: "Basic Shapes"`, registered through the public mechanism.
- **Undo/redo** (ADR 0007): gesture-level history via a small closed command set — `AddEntity`/`RemoveEntity`, `ChangeBoundsCommand`, `MutateEntity` (opaque props mutations), `GroupCommand`/`UngroupCommand`, `CompositeCommand` (atomic multi-entity gestures). New features reuse these primitives rather than adding bespoke command classes. Circular buffer capped at 1000 entries; in-memory, session-scoped, never persisted; no actor/origin field.
- **Property panel & layering** (ADR 0008): generic panel built from attribute-declared editable properties on the props record, overridable via the registration builder; each carries an `EditorKind` (closed built-in set + `Custom` taking an author-supplied render fragment). `Text`-type content is edited inline/WYSIWYG, excluded from the panel. Cross-type multi-select edits only explicitly-tagged shared properties. Layering = four `ZIndex` commands (front/back/forward/backward); new instances default on top; groups have no z-slot — group layering bulk-writes member `ZIndex` values preserving relative order.
- **Interaction model** (ADR 0009, shortcut table amended by ADR 0011): no persistent tool-mode state machine. Placement by palette drag-and-drop or click-to-add (viewport centre + cascading offset), both using registered `DefaultSize`. Connectors by drag-from-port or a built-in, non-registered "Connector" palette entry dropping a floating edge. Baseline shortcuts: delete, escape, undo/redo, group/ungroup, layering; a matching context menu on selection; no menu on empty canvas.
- **Keyboard accessibility** (ADR 0010): reading-order tab stops (a group is one stop), focus-follows-selection, zoom-relative arrow-key move, `Alt+Arrow` per-axis resize with stable anchoring, `Ctrl+Tab`+`Space` keyboard multi-select, and mouse-free placement/connector attachment via the same focus/nudge mechanics. `aria-selected` on selection.
- **Scale & grid** (ADR 0011): board extent and zoom unbounded by default (replacing the prototype's fixed 3000x3000 extent and 0.6x–6x zoom), with host-configurable min/max zoom. Host-configurable LOD cutoff swaps sub-threshold instances (bounds × zoom) for a generic placeholder built from `DisplayName`/`Icon` — windowing alone can't bound a dense zoomed-out board. Adaptive multi-layer grid: concurrent layers at 10x spacing steps crossfading across legibility thresholds. Snap-to-grid: off-by-default parameter + `Ctrl+'` toggle, snapping to the dominant layer; ephemeral view state, never persisted.
- **Theming** (ADR 0012): a shared theme-token layer (CSS custom properties) read by all library-owned chrome; no C# theming API. Default light + dark themes, `prefers-color-scheme` by default with a `data-d12-theme` host override. Each chrome root declares its own token defaults (no global `:root`) so components work standalone, two canvases can differ, and a host override on a shared ancestor themes both via ordinary CSS inheritance. Per-instance visuals are props (ADR 0008), not theme tokens.

## Testing Decisions

A good test exercises external behavior at a public seam — what a host developer or end user observes — never internal implementation details. Three seams, from the layered testing strategy locked in the wayfinder effort (ticket 04):

1. **Pure C# seam** — `Board`, the command/history model, the serializer, and the registry are plain classes/services; their behavior (entity CRUD, viewport queries, undo/redo semantics, round-tripping, duplicate-key rejection, strict-vs-partial deserialize) is tested with ordinary xUnit unit tests, no rendering involved. This is the highest seam and should carry most of the behavioral coverage. Prior art: the existing pure-logic tests for the zoom/pan tracker.
2. **bUnit component seam** — component logic and markup: palette rendering from the registry, property-panel control selection by `EditorKind`, ARIA attributes (`AccessibleName`, `Role`, `aria-selected`), keyboard event handling, `ShouldRender` skip behavior. Prior art: the existing bUnit test base and canvas tests.
3. **Playwright visual seam** — a small, curated screenshot-diff layer running against the real Demo app for rendered visual states only: edges and routing styles, zoom/pan states, mid-drag and mid-resize, grid crossfade, LOD placeholder, light/dark themes. Wired into CI from day one via the official Playwright Docker image. Percy/Chromatic explicitly skipped.

Standing rule: any ticket that renders a new visual state on canvas must add a screenshot case.

The virtualization stress-test Demo page is a permanent dev tool for performance verification, not part of the automated suite.

## Out of Scope

- Real-time multi-user collaboration — sync protocol, presence, conflict resolution, permissions, any hosted sync backend. The state model, component contract, and persistence format must merely not foreclose it.
- D12Canvas becoming a standalone hosted product or shipping any backend — it stays a component library; storage and backends are host-app concerns.
- Freehand Pen as a built-in component type — its drawn-path content and continuous drag-to-draw interaction don't fit the static-default-props shape of the other built-ins (ruled out in ticket 10).
- Image export or third-party diagram formats — import/export is the native JSON round-trip only (ticket 13).
- Library-shipped import/export trigger UI (menus, buttons, shortcuts, drag-drop targets) — host-wired, inseparable from host-owned storage.
- A schema migration pipeline — `SchemaVersion` is reserved, but no migration machinery until a real V2 format exists.
- Actor/origin attribution in undo history — undo stays local-user-only; collaboration attribution belongs to a future effort.
- Persisting any transient view state: selection, history, snap-to-grid, zoom/pan.

## Further Notes

- This is a ground-up rethink: the current prototype's public surface is not contractual. Its pan/zoom JS interop, container drag/resize mechanics, and the virtualization fixes (skip-if-unchanged rendering, throttled pan re-render) are proven starting material.
- A known prototype defect — the container's dynamic JS import 404ing in host apps because it uses a host-relative path instead of the RCL `_content/` static-asset path — is tracked separately in `.scratch/component-container-js-import-404/` and should be avoided by construction in the new work (use Blazor's colocated JS-isolation convention).
- Performance was treated as an early concern by design: the viewport-query surface and rendering discipline exist because the DOM ceiling was measured before the state model locked. Keep that stance — new interactive surfaces should state their expected element counts against the ~200–500 unmitigated ceiling.
- Non-foreclosure constraints to carry through implementation: collaboration-ready state model, screen-reader-friendly component contract, import/export-ready persistence.
- Vocabulary in this spec follows the root `CONTEXT.md` glossary; when implementation tickets are cut from this spec, they should keep using it ("component"/"component instance", never "shape"/"node"; "edge", never "connector"/"link"; "props", never "parameters").
