# App test project, page harness and preferences seam

Status: resolved
Blocked by:

Spec: [App polish](../spec.md), "Preferences seam" and "Testing Decisions". Decision record: [App test coverage](10-app-test-coverage.md).

## What to build

Prefactoring so every later App ticket can test the behaviour it builds through one seam.

- A new `D12Canvas.App.Tests` project, set up like `D12Canvas.Tests`: xUnit v3 on Microsoft.Testing.Platform, bUnit 2.8.4-preview with the AngleSharp 1.5.2 override. It references the App and joins the solution.
- A test helper that renders a routed page as the body of `MainLayout`, so anything the layout renders (later: the app bar, sections, Suggest) is present. It registers a hand-written `InMemoryBoardStore` (a dictionary of saved boards, a settable clock, a record of each save), an `InMemoryPreferences`, bUnit's fake `NavigationManager`, and strict JS interop. No mocking library.
- A new App interface `IPreferences` with `GetAsync(key)` and `SetAsync(key, value)` over string values, one implementation over `localStorage`, registered in the App's DI. Nothing uses it yet.
- CI's unit-tests job runs `D12Canvas.App.Tests` and also `D12Canvas.App.Api.Tests`, which it doesn't run today.

## Acceptance criteria

- [x] `D12Canvas.App.Tests` builds and runs in the solution.
- [x] A smoke test renders the board list inside `MainLayout` with one board in `InMemoryBoardStore` and finds that board's name on the page.
- [x] A second smoke test renders the board editor for a stored board inside `MainLayout` and finds the board name.
- [x] `IPreferences` and its `localStorage` implementation exist and are registered; the App still runs.
- [x] CI's unit-tests job runs both App test projects, and they pass.
- [x] Tests use descriptive variable names, not `cut` or `sut`.

## Comments

Done. `AppTestContext` in `test/D12Canvas.App.Tests` is the base class every App test derives from. `RenderBoardList()` and `RenderBoardEditor(id)` navigate the fake `NavigationManager` and render the page as `MainLayout`'s body. `RenderPage<TPage>(uri, parameters)` does the same for any other page. The editor renders a real `DiagramCanvas`, so `CanvasJsInterop` sets up the library's JS modules in strict mode. That needs the library's internal `InitialFacts`, so `lib/D12Canvas` now has `InternalsVisibleTo D12Canvas.App.Tests`. `InMemoryBoardStore` stores `Board` objects by reference, with no serialisation round trip. `LocalStoragePreferences` calls `localStorage.getItem` and `setItem` directly, with no JS module.
