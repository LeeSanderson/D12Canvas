# 28 — Click-to-add placement

**What to build:** An end user clicks a palette entry and the component instance appears instantly at the viewport centre — no drag needed. Repeated click-adds cascade with an offset so successive instances don't stack invisibly on top of each other. (ADR 0009.)

**Blocked by:** 22 (Canvas renders a Board), 26 (Palette lists registered types)

**Status:** resolved

- [x] Clicking a palette entry places an instance at the current viewport centre in board coordinates
- [x] Consecutive click-adds apply a cascading offset
- [x] The new instance uses the registered `DefaultSize` and `DefaultProps`
- [x] bUnit/xUnit coverage including the cascade behaviour

## Comments

`Palette` gains a plain `@onclick="() => HandleClick(entry.Key)"` on the same entry button that already carries `@ondragstart` (ticket 27) - the two gestures coexist on one element with no conflict, since a native HTML5 drag session and a `click` are mutually exclusive at the browser level (a completed drag never also fires `click`). `HandleClick` mirrors `HandleDragStart`'s shape exactly: `Canvas?.ClickToAdd(entry.Key)`, a safe no-op when `Canvas` is unset.

`DiagramCanvas` gains the other half: a public `ClickToAdd(string componentTypeKey)`. Per ADR 0009, the default position is the *current viewport's* center in board coordinates, not a fixed board-space origin - `_zoomPanTracker.Viewport` already exposes exactly this rect (it's the same inverse pan/scale transform ticket 27's drop handling and `GetVisible` both already rely on), so no new coordinate math was needed. `DefaultSize` (falling back to `ComponentContainer`'s own 200x150 default, same as ticket 27) centers the new instance on that point, and `DefaultProps` is used directly, consistent with ticket 27's reasoning (every shipped `TProps` is an immutable positional record, so sharing the reference across placements is harmless).

**Cascading offset**: a single `_clickToAddCascadeCount` field increments on every `ClickToAdd` call and contributes `count * 20` px to both X and Y of the placement point. The counter is global rather than per-component-type - the requirement is only that successive instances don't visually stack, which holds regardless of whether the same or different palette entries were clicked - and it never resets, since there's no natural "end of a placement session" event in this app's momentary-gesture-only interaction model (ADR 0009) to reset it on.

Unlike ticket 27's `HandleDrop`, `ClickToAdd` needs no JS interop call to fetch fresh container dimensions - the viewport rect is already maintained by `ZoomPanTracker` independent of any DOM measurement at click time - so the method is synchronous. The two methods otherwise shared an identical shape (resolve registration, fall back to `FallbackWidth`/`FallbackHeight`, build a `Bounds` centered on a point, add to `Board`), so that shape was pulled out into a private `PlaceComponent(componentTypeKey, centerX, centerY)` both now call - `HandleDrop` and `ClickToAdd` differ only in how they arrive at the center point (drop point vs. viewport center plus cascade) and in their own `StateHasChanged()`/`Board is null` handling either side of it.

bUnit coverage (`DiagramCanvasClickToAddTests.cs`): viewport-center placement using `DefaultSize`/`DefaultProps`, fallback size when unregistered, the viewport center under active pan and zoom, the cascading offset across three consecutive calls, and a no-op when no `Board` is wired. Calls to `ClickToAdd` are wrapped in `canvas.InvokeAsync(...)` rather than called directly on `canvas.Instance` - unlike `BeginPaletteDrag` (ticket 27), `ClickToAdd` calls `StateHasChanged()` itself, which throws if invoked off the renderer's dispatcher thread. `PaletteTests.cs` gained a click-wiring round trip and a safe no-op when no `Canvas` is wired, mirroring the existing drag-start cases.

New Playwright visual coverage (`ClickToAddPlacementVisualTests.cs`, reusing the existing `/placement-demo` page from ticket 27): a single click-to-add result, and two consecutive clicks showing the cascade offset. Both drive the palette button via a real `Locator.ClickAsync()` - no synthetic-event workaround was needed here, unlike ticket 27's drag-affordance case, since a click is a single instantaneous event rather than a held gesture Chromium needs to keep repainting through.
