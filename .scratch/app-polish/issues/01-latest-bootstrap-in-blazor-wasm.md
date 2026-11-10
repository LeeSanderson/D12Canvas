# Latest Bootstrap and Bootstrap Icons in a Blazor WebAssembly app

Type: research
Status: resolved
Blocked by:
Research: .scratch/app-polish/research/01-latest-bootstrap-in-blazor-wasm.md

## Question

What are the latest stable releases of Bootstrap and Bootstrap Icons (as of 2026-11), and what does each need to run in a standalone Blazor WebAssembly app like D12Canvas.App?

Find out:

- Current versions, and any major-version changes since Bootstrap 5.3 that affect setup or class names.
- Ways to deliver them: vendored files in `wwwroot`, CDN, LibMan, npm. Note what each means for an app that should work offline (the App stores boards in IndexedDB).
- Colour modes (`data-bs-theme` or its successor): how light, dark and "follow the system" work, and whether "system" needs a script.
- Which components need Bootstrap's JavaScript (dropdowns, modals, collapse/offcanvas for a narrow-screen nav, tooltips), and known problems when Blazor re-renders DOM that Bootstrap JS manages.
- Bootstrap Icons: icon font versus SVG sprite versus inline SVG, with size and accessibility trade-offs.
- Conflicts with Blazor's template CSS or with a page that also hosts a canvas styled by its own CSS custom properties.

## Answer

Full findings with sources: `.scratch/app-polish/research/01-latest-bootstrap-in-blazor-wasm.md`.

- Latest stable releases: Bootstrap 5.3.8 (2025-08-26) and Bootstrap Icons 1.13.2 (2026-10-08). Bootstrap 6.0.0-alpha.1 was tagged on 2026-10-08 and has no stable release date. The .NET 10 Blazor template still ships Bootstrap 5.3.3.
- v6 renames a lot. Modal becomes Dialog on a native `<dialog>`, Offcanvas becomes Drawer, Dropdown becomes Menu, `.btn-primary` becomes `.btn-solid theme-primary`, and responsive classes become prefixes. Its JS is ESM only, and its CSS sits in `@layer`, so unlayered App and library CSS overrides it. The alpha post warns that details can change before beta.
- Colour modes: with 5.3, following the OS needs a `<head>` script that sets `data-bs-theme`. v6 follows the OS with no script. Remembering a user's choice needs a script on either version. The canvas reads `data-d12-theme`, so one App setting has to set both attributes.
- Bootstrap JS and Blazor: Microsoft Learn and Bootstrap's own docs both warn against letting Bootstrap JS change DOM that Blazor renders. In 5.3, collapse, offcanvas, dropdown, modal and tooltip all need that JS. `SuggestionBox` already uses a native `<dialog>`.
- Delivery: only files vendored in `wwwroot` would land in a future service-worker cache. The App has no service worker, npm or LibMan today.
- Clashes: the App's own `.btn*`, `.card`, `.modal-backdrop` and `.modal-title` rules clash with 5.3. Reboot changes the font that canvas `StickyNote` and `Text` inherit, and restyles the plain elements the library uses. A 5.3 dropdown and the library's `ContextMenu` both sit at z-index 1000.
- Icons: the font costs about 221 KB for every icon. The sprite is 1.1 MB and must be served from the App's own origin. Inline SVG costs only the icons used.
- Not settled: what actually breaks when Blazor re-renders an element Bootstrap JS has open. That needs a prototype.
