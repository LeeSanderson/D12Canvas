# App test coverage

Type: grilling
Status: resolved
Blocked by:

## Question

Does the spec add tests for D12Canvas.App, and if so which?

No test project covers D12Canvas.App today. The App now has settled behaviour worth pinning: the nav bar's sections and offcanvas ([Shared nav bar](07-shared-nav-bar.md)), autosave and its `localStorage` key, the theme switch ([App and canvas theme](05-app-and-canvas-theme.md)), the editor's View menu and status bar ([Editor toolbar](08-editor-toolbar.md)), and the board list's states ([Board list page](09-board-list-page.md)). Decide whether to add a bUnit project (xUnit, per the repo's habits), which components it covers, and how it fakes `IBoardStore` and JS interop. App visual tests stay out of scope. The library's board sketch component is covered by the library's own tests and the visual suite.

## Answer

Yes. The spec adds a bUnit project for the App.

**Project.** `test/D12Canvas.App.Tests`, set up like `test/D12Canvas.Tests`: xUnit v3 MTP, bUnit 2.8.4-preview and the AngleSharp 1.5.2 override. It references `app/D12Canvas.App` and is added to `D12Canvas.slnx`.

**Coverage.** Each implementation issue writes tests for the behaviour it builds. Nothing gets back-filled for code this effort doesn't touch.

- Autosave. It's on by default. The toggle writes `d12canvas.autosave`, and a stored value wins on load. While it's on, Save is hidden and "Saving…"/"Saved" shows. An edit calls `SaveAsync`.
- Theme switch. Light, Dark and System each write `d12canvas.theme` and set both `data-bs-theme` and `data-d12-theme`. System is the default.
- Nav bar. The brand links to `/`. The editor's page header shows Undo/Redo, View and Shortcuts, and the list's header doesn't. The offcanvas opens and closes from Blazor state.
- Editor status bar and View menu. The selection count, Grid and Snapping toggles, and zoom commands act on the canvas.
- Board list. The empty state shows one card. Cards show the name link and "Edited …". New board calls `CreateAsync` and navigates. Rename and Delete in the ⋮ menu call the store.

Not tested: focus return after renaming (bUnit can only check that `FocusAsync` was called), `IndexedDbBoardStore` (a thin JS wrapper), `SuggestionBox` beyond its move into the layout, and anything pixel-level.

**Store fake.** A hand-written `InMemoryBoardStore` in the test project. It holds a dictionary of `SavedBoard`s, has a settable clock so `UpdatedAt` and "Edited …" are deterministic, and records each save so autosave tests can count them. No mocking library.

**Preferences and JS.** The App gets an `IPreferences` interface (`GetAsync(key)` / `SetAsync(key, value)`) with one implementation over `localStorage`. Autosave and the theme read and write through it. Tests use an `InMemoryPreferences` fake. The theme's write to the `<html>` attributes stays a call into a JS module. Tests check it with bUnit `JSInterop` in strict mode and `SetupModule`, the same pattern `D12Canvas.Tests` uses, so a JS call a test didn't set up fails that test.

**CI.** The `unit-tests` job in `.github/workflows/ci.yml` runs `D12Canvas.App.Tests` and also `D12Canvas.App.Api.Tests`, which CI doesn't run today.
