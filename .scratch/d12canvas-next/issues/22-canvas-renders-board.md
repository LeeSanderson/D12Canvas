# 22 — Canvas renders a Board

**What to build:** An end user sees board content on screen: `DiagramCanvas` accepts a `Board` and renders each component instance's registered Razor component at its bounds on the pannable/zoomable surface, resolving each instance's component-type key through the registry. The registered `AccessibleName` and `Role` are applied as ARIA on each instance's container automatically. A Demo page seeds a board programmatically to prove the path. This is the first visible tracer through the new architecture.

**Blocked by:** 19 (Prefactor: colocated JS isolation), 20 (Component-type registration contract & registry), 21 (Board model with component instances)

**Status:** resolved

- [x] Instances render at their bounds and remain correctly positioned through pan and zoom
- [x] Rendering is registry-resolved: component-type key → registered component + typed props
- [x] `AccessibleName` is auto-applied as ARIA on each instance's container; `Role` is applied
- [x] `ZIndex` is respected in visual stacking
- [x] A Demo page shows a programmatically seeded board
- [x] bUnit coverage of registry-resolved markup

## Comments

`DiagramCanvas` now takes a `[Parameter] public Board? Board` and injects `IComponentRegistry`. When `Board` is set, it loops `Board.Components` and, for each `ComponentInstance`, resolves the registration and renders a `ComponentContainer` (positioned at `instance.Bounds`) wrapping a `DynamicComponent` (`Type` = the registration's `ComponentType`, `Parameters` = `{ "Props": instance.Props }`). This lives inside the same `CascadingValue Name="ParentCanvas"` block as the existing `ChildContent` path, so board-rendered containers get zoom-relative drag/resize scaling for free; `ChildContent` is untouched and still works for manually-composed content (stress test / component-container demo pages).

Positioning through pan/zoom needed no new logic: `ComponentContainer` already renders at plain `left/top` inside `.canvas-content`, and it's `.canvas-content` itself that carries the pan/zoom `transform`. Instance bounds are canvas-space coordinates, so they're correct under any pan/zoom by construction — verified live (Playwright) by panning and confirming each container's `style` attribute is untouched while `.canvas-content`'s `transform` changes.

Established a new contract addendum (ADR 0001): a registered `TComponent` receives its props via a fixed `[Parameter] public TProps Props { get; set; }` — the name `DynamicComponent`'s `Parameters` dictionary binds by. Nothing before this ticket specified how a `TComponent` actually receives `Props` at render time.

`ComponentContainer` gained three new optional parameters to support this: `ZIndex` (applied as inline CSS `z-index`, defaulting to 0 so all pre-existing manual usages are unaffected), `AccessibleName` (rendered as `aria-label`), and `Role` (rendered as `role`) — both omitted from the DOM entirely when null, per ordinary Blazor attribute-binding behavior, so unrelated existing tests/pages needed no changes.

`ComponentTestBase` now registers a default empty `ComponentRegistry` as `IComponentRegistry`, since `DiagramCanvas`'s injected dependency must resolve in every bUnit test that renders it, Board or not; individual tests override it with a populated registry via `Services.AddSingleton` where they need specific registrations resolved.

**Known limitation, not acted on:** `ComponentContainer.ShouldRender()` still gates on `X`/`Y`/`Width`/`Height`/edit-mode only. If a future ticket starts mutating an already-rendered instance's `Props` or `ZIndex` at runtime (property panel, z-index commands), the container won't know to re-render on those alone — out of scope here since nothing mutates a live instance's `Props`/`ZIndex` yet; flagged for whichever ticket introduces that first (property panel, ticket 56; z-index commands, ticket 60).
