# 39 — Rectangle built-in

**What to build:** An end user opens the palette and can immediately place a Rectangle — the first built-in component type, shipped by the library but registered through the same public mechanism as any custom type. It has its own props type carrying its visual fields (fill, stroke, …) as ordinary props, a distinct default size, default props, an icon, and `Category: "Basic Shapes"`. This ticket establishes the pattern the other built-ins follow.

**Blocked by:** 22 (Canvas renders a Board), 26 (Palette lists registered types)

**Status:** resolved

- [x] Rectangle appears in the palette under "Basic Shapes" with its icon
- [x] It registers through the public registration mechanism — no private shortcuts
- [x] Its visual fields are ordinary props on its own props type — no separate theming model
- [x] Placed instances render with the default props and default size
- [x] Screenshot case for a rendered Rectangle

## Comments

New `D12Canvas/BuiltIns/` folder (parallel to `Model`/`Registration`/`Persistence`/`History`):
`RectangleProps.cs` (`FillColor`, `StrokeColor`, `StrokeWidth` - exactly ticket 10's answer) and
`Rectangle.razor` (a plain `[Parameter] public RectangleProps Props` component rendering
background/border from those fields, matching `DemoNoteComponent`'s pattern), plus
`BuiltInComponents.cs` - an internal `RegisterAll(D12CanvasOptions)` that calls
`options.RegisterComponent<Rectangle, RectangleProps>("rectangle", ...)`, the *exact* same builder
call any host uses (ADR 0001), just invoked by the library itself.

`ServiceCollectionExtensions.AddD12Canvas` calls `BuiltInComponents.RegisterAll` inside its existing
`existingRegistry is null` branch, **after** the host's own `configure` callback runs. Two
consequences of that ordering: built-ins register exactly once regardless of how many times
`AddD12Canvas` is called (same guard that already deduplicates the DI singletons), and Rectangle is
always appended *after* any host-registered types - so a host's own palette entries keep their
existing DOM order/`.First` semantics. Confirmed via `HostRegisteredComponentsPrecedeRectangleInPaletteOrder`
in the new `BuiltInComponentsTests.cs`, and by the Demo app's existing gesture-test baselines
(`ClickToAddPlacementVisualTests` etc.) still placing the pre-existing "Demo Note" first after
Rectangle joined the registry - only the palette's entry *count* changed (2 → 3).

Because the Demo app's `Program.cs` already calls `AddD12Canvas`, Rectangle now shows up for free on
every page that renders `<Palette>`, and the Playwright visual-test suite's shared `/placement-demo`
test bed picks up a new "Basic Shapes" category. Updated the `.d12-palette-entry` count assertion in
every affected visual-test class (`PaletteVisualTests`, `ClickToAddPlacementVisualTests`,
`DragAndDropPlacementVisualTests`, `DragMoveVisualTests`, `MarqueeVisualTests`,
`MultiSelectionMoveResizeVisualTests`, `ResizeVisualTests`, `SelectionVisualTests`) and regenerated
their baselines. Screenshot case: `BoardDemo.razor`'s seeded `Board` gained a fourth instance (a
`rectangle`), and `BoardRenderingVisualTests`' `.component-container` count moved from 3 to 4.

**Baseline-regeneration detour (not Rectangle's fault, but blocked getting clean baselines):**
regenerating 16 screenshot baselines surfaced two pre-existing, unrelated problems, both diagnosed
by comparing a from-scratch `git stash` run of the *unmodified* codebase against the new one:
1. A stale scoped-CSS build artifact: repeated incremental rebuilds this session left
   `D12Canvas.Demo`'s compiled CSS bundle (`obj/.../scopedcss/bundle/D12Canvas.Demo.styles.css`)
   targeting an older component-hash (`b-k7rv4lobv5`) while the freshly-served markup had already
   moved to a new one (`b-52l6hnzelf`) - so none of the Demo app's own scoped CSS (nav sidebar
   gradient, top bar) applied, though the DOM and all library-owned rendering were unaffected. A
   `dotnet clean`+rebuild of `D12Canvas.Demo` fixed it; no source change needed.
2. A pre-existing timing race in `DiagramCanvas`'s click-to-add path on a cold WASM boot - filed as
   ticket 77, since it's a real (if narrow) product gap, not a test artifact.
Every regenerated baseline was individually re-inspected (visually, plus an HTML grep for clipped/
negative coordinates) against the original before being accepted, to keep this ticket's diff to
exactly Rectangle's addition rather than silently baking in either of the above.

xUnit coverage: `RectangleTests.cs` (bUnit render test - fill/stroke/default-props) and
`BuiltInComponentsTests.cs` (registry gets `"rectangle"` automatically from a bare `AddD12Canvas`,
registered exactly once across repeat `AddD12Canvas` calls, registered after host components).
