# App test coverage

Type: grilling
Status: open
Blocked by:

## Question

Does the spec add tests for D12Canvas.App, and if so which?

No test project covers D12Canvas.App today. The App now has settled behaviour worth pinning: the nav bar's sections and offcanvas ([Shared nav bar](07-shared-nav-bar.md)), autosave and its `localStorage` key, the theme switch ([App and canvas theme](05-app-and-canvas-theme.md)), the editor's View menu and status bar ([Editor toolbar](08-editor-toolbar.md)), and the board list's states ([Board list page](09-board-list-page.md)). Decide whether to add a bUnit project (xUnit, per the repo's habits), which components it covers, and how it fakes `IBoardStore` and JS interop. App visual tests stay out of scope. The library's board sketch component is covered by the library's own tests and the visual suite.
