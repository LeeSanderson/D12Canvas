# D12Canvas Next: Local-First Diagramming Platform

Wayfinder map.

## Destination

A written spec (PRD) for the next version of D12Canvas, backed by a set of locked ADRs for the foundational architectural decisions beneath it. D12Canvas remains a reusable, embeddable Blazor component library. Scope is the local-only, single-user diagramming experience — a Miro-like canvas of extensible, developer-registered components. Real-time multi-user collaboration is out of scope for this map, but the foundational decisions (state model, component contract, persistence format) must not foreclose it.

## Notes

- Domain: a Blazor Razor-component-based diagramming canvas library. Current base framework: `DiagramCanvas` (pan/zoom via JS interop) + `ComponentContainer` (draggable/resizable, arbitrary `RenderFragment` content). No prior decision here needs to hold — this map is a rethink, not a continuation.
- Consult `domain-modeling` when naming things — pin "component" vs "shape/node", "palette", "board" vs "canvas". Consult `codebase-design` when shaping the public extensibility API (it's a deep-module candidate).
- **Settled, not open for re-litigation:** rendering approach is DOM/CSS (absolutely-positioned elements) — a direct consequence of supporting arbitrary developer-defined components as canvas content. Canvas/WebGL and SVG can't host live, interactive Razor components as node content.
- **Settled, not open for re-litigation:** component delivery ships both a registration API *and* a default palette UI component (not registry-only) — the tool should feel usable out of the box, not require every host app to build its own picker chrome.
- **Non-foreclosure constraints** — bind on the component registration ticket, the state/data model ticket, and the persistence format ticket: the state/data model must stay collaboration-ready; the component contract must stay screen-reader-friendly (full keyboard navigation can lag); persistence design must stay import/export-ready. None of these features are being built now — only kept cheap to add later.
- Performance is being treated as an early, not deferred, concern: DOM-based rendering has a real ceiling, and the state/data model should be informed by that ceiling before it locks, to avoid costly rework later.

## Decisions so far

- [Blazor/bUnit visual & render-verification tooling options](issues/03-visual-testing-tooling-research.md) — bUnit stays for logic/markup; add Playwright for .NET as a small, curated screenshot-diff layer against the real Demo app. Skip Percy/Chromatic. No comparable Blazor OSS library does this today.
- [DOM-rendering performance ceiling for Blazor components](issues/01-performance-ceiling-research.md) — reasoned ceiling of ~200–500 concurrently-interactive elements without mitigation; design the state model's viewport-query *surface* now (à la Blazor.Diagrams' linear scan), defer the spatial-index *implementation* until profiling demands it.
- [Virtualization/windowing mitigation stress test](issues/02-virtualization-prototype.md) — windowing (recomputed on pan/zoom/resize, not per frame) confirmed decisive at scale (12x–8x fps gains measured); real bottleneck was Blazor re-rendering every mounted child regardless of its own state, fixed directly (`ShouldRender` skip-if-unchanged + throttled pan `StateHasChanged`, commit `c3d8b19`). State/data model needs a viewport query surface with a *tunable overscan margin*, and the component contract should expect skip-if-unchanged rendering as standard, not optional. Stress-test kept as a permanent dev tool (`/virtualization-stress-test`), not a throwaway.
- [Layered testing strategy (unit + visual verification)](issues/04-layered-testing-strategy.md) — bUnit stays default for logic/markup; Playwright for .NET added for rendered visual states only (nodes/edges, zoom/pan, mid-drag, mid-resize), wired into CI from day one via the official Playwright Docker image. Standing rule: any future ticket that renders a new visual state on canvas must add a screenshot case.
- [Component registration & palette API contract](issues/05-component-registration-api.md) — DI-based registration keyed by a stable string decoupled from the CLR type; `TProps` (business data) kept structurally separate from bounds. Required: `DisplayName`, `AccessibleName` (auto-applied as ARIA by `ComponentContainer`), `DefaultProps`. Optional: `Icon`, `Role` (default `"group"`), `DefaultSize`, `Category`. Palette (and future chrome like a minimap) is a new "canvas chrome" category — a standalone, reference-wired component outside `DiagramCanvas`'s pannable content, positioned by the host's own CSS. Seeded `CONTEXT.md` and two ADRs (`0001`, `0002`).

## Not yet specified

- Tool modes & interaction details (keyboard shortcuts, context menus, how "drawing" a shape/connector actually feels) — graduates once the component registration contract and the selection model resolve.
- Undo/redo history model — graduates once the state/data model resolves.
- Styling/theming (fill/stroke/colors/fonts, z-order/layers) — graduates once the component registration contract and the state/data model resolve.
- Full keyboard-accessibility interaction (beyond the baseline screen-reader-friendly component contract) — graduates once the component registration contract and tool modes are specifiable.
- Import/export feature design (formats, triggering UI) — graduates once the persistence/serialization format resolves.

## Out of scope

- Real-time multi-user collaboration (sync protocol, presence, conflict resolution, permissions, any hosted sync backend) — a future effort, once the local tool and its collaboration-ready state model exist.
- D12Canvas becoming a standalone hosted product/backend — it stays a component library; any backend is host-app-provided.
