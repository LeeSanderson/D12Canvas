# 82 — D12Canvas.App: a showcase board-list example project

**What to build:** A new standalone Blazor WebAssembly project, `D12Canvas.App`, sibling to
`D12Canvas.Demo` and referencing the core `D12Canvas` library - a realistic example of what a host
app built on D12Canvas looks like, distinct in purpose from `D12Canvas.Demo`'s grab-bag of isolated
per-feature test pages (several of which are load-bearing for the Playwright visual-test suite).

A board-list page lists every saved board (name plus created/updated timestamps) with Create, Open,
Rename, and Delete (Delete confirms first - no undo/trash concept here). Opening a board navigates
to a page hosting `DiagramCanvas` bound to the loaded `Board`, with an explicit Save button.

Persistence goes through a new, app-level `IBoardStore` abstraction - not added to the core library,
since ADR 0004 already settled that storage I/O is host-owned:

```csharp
public interface IBoardStore
{
    Task<IReadOnlyList<BoardSummary>> ListAsync();
    Task<SavedBoard> CreateAsync(string name);
    Task<SavedBoard> LoadAsync(Guid id);
    Task SaveAsync(Guid id, Board board);
    Task RenameAsync(Guid id, string name);
    Task DeleteAsync(Guid id);
}
```

`BoardSummary` (`Id`, `Name`, `CreatedAt`, `UpdatedAt`) is the lightweight record the list page
renders without loading full board content; `SavedBoard` (`Id`, `Name`, `Board`) is what
`Create`/`Load` return.

One `IBoardStore` implementation, backed by hand-rolled IndexedDB JS interop (a single object store
keyed by board `Id`; no third-party package - matches this repo's own established colocated-JS
convention from ticket 19, and the surface needed here - list/get/put/delete, no schema migrations,
no cross-store transactions - is small enough not to justify a new dependency).

**Status:** resolved

- [x] `D12Canvas.App` project exists, wired into `D12Canvas.slnx`, references `D12Canvas`
- [x] Board-list page: Create, Open, Rename, Delete (Delete confirms first)
- [x] Board page: `DiagramCanvas` bound to the loaded `Board`, with a Save button
- [x] `IBoardStore` / `BoardSummary` / `SavedBoard` defined at the app level, not the core library
- [x] IndexedDB-backed `IBoardStore` implementation, hand-rolled JS interop, no third-party package
- [x] Registers only the built-in component types (`AddD12Canvas` already does this automatically)
      - no custom example component type; that story already belongs to `D12Canvas.Demo`
- [x] Save button is unconditionally enabled for this ticket - the disabled-when-unmodified
      refinement is ticket 83's, not this one's
- [x] No automated test suite for this project - playground code; must build, pass the repo's
      `dotnet csharpier --check .` formatting gate, and run

## Comments

Implemented alongside ticket 83 in the same pass, so the Save button lands directly in its final
disabled-when-clean/autosave shape rather than as an intermediate unconditionally-enabled state -
this ticket's own checklist item above is a scope clarification, not a literal end state, given both
tickets shipped together.

Also added a `Palette` (wired to the board's `DiagramCanvas` the same explicit-reference way
`PropertyPanelDemo.razor` in `D12Canvas.Demo` already does) to the board editor page - without one,
a created board could never gain any content, since `IBoardStore.CreateAsync` always starts from an
empty `Board`. Not called out explicitly in the ticket body, but required for the app to be usable
at all with only the built-in component types registered.

`IndexedDbBoardStore`'s JS module lives in `wwwroot/js/indexedDbBoardStore.js`, loaded via a plain
`./js/...` relative import rather than the `_content/{Assembly}/...` colocated-JS URL scheme ticket
19 established - that scheme solves a *library* being consumed by an unknown host with no path
configuration; `D12Canvas.App` is the host itself and owns its own `wwwroot`, so a plain relative
path is simplest and matches `D12Canvas.Demo`'s own precedent for its app-level
`virtualizationStressTest.js`. "Matches ticket 19's convention" (per this ticket's own body) reads
as reusing its hand-rolled-interop-over-third-party-package precedent, not literally reusing its URL
scheme.

Verified end-to-end with a headless-Chromium Playwright smoke run against the running dev server:
create → open → place a Rectangle → Save → back to list → Rename → Delete-confirms → reopen →
placed instance persisted via IndexedDB, with zero console errors.
