# Research: latest Bootstrap and Bootstrap Icons in a Blazor WebAssembly app

Ticket: `.scratch/app-polish/issues/01-latest-bootstrap-in-blazor-wasm.md`
Researched: 2026-11-10

## Short answer

- Latest stable Bootstrap is **5.3.8**, published 2025-08-26. No stable release has come out since.
- **Bootstrap 6.0.0-alpha.1** came out on 2026-10-08. It is a pre-release with no stable date. It renames a lot: Modal becomes Dialog, Offcanvas becomes Drawer, Dropdown becomes Menu, `.btn-primary` becomes `.btn-solid theme-primary`, and responsive classes move to a prefix form such as `md:d-none`. Its JS is ESM only.
- Latest stable Bootstrap Icons is **1.13.2**, published 2026-10-08. It has no breaking changes.
- None of the three needs a build step. Copying the dist files into `wwwroot` is enough.
- Bootstrap 5.3 does not follow the OS colour scheme unless a script sets `data-bs-theme`. Bootstrap 6 follows it with no script.
- Microsoft's Blazor docs name Bootstrap JS as a library that must not change DOM that Blazor renders.

## 1. Current versions and major-version changes

### Versions

| Package | Latest stable | Published | Newer pre-release |
|---|---|---|---|
| Bootstrap | v5.3.8 | 2025-08-26 | v6.0.0-alpha.1 (tag only, 2026-10-08) |
| Bootstrap Icons | v1.13.2 | 2026-10-08 | none |

Sources:
- Bootstrap releases, with v5.3.8 marked "Latest": https://github.com/twbs/bootstrap/releases. Dates come from the API: https://api.github.com/repos/twbs/bootstrap/releases
- Bootstrap tags. `v6.0.0-alpha.1` is the newest tag, but it has no GitHub Release entry: https://api.github.com/repos/twbs/bootstrap/tags
- Icons releases, with v1.13.2 marked "Latest": https://github.com/twbs/icons/releases and https://api.github.com/repos/twbs/icons/releases
- icons.getbootstrap.com shows v1.13.2 in its header: https://icons.getbootstrap.com/

The 5.3.8 post says "The plan is for this to be the last patch release before v5.4.0 drops." No v5.4.0 exists yet. https://blog.getbootstrap.com/

The v6 alpha post says v5 "remains supported" and that v5.4.0 will ship before an end-of-life date for v5 is announced. It gives no date for v6 stable. More alphas come first, then beta and RC. https://blog.getbootstrap.com/2026/10/08/bootstrap-6-alpha/

The .NET 10 Blazor WebAssembly template still ships Bootstrap **v5.3.3** in `wwwroot/lib/bootstrap`. Its `bootstrap.min.css` header says so. https://github.com/dotnet/aspnetcore/tree/release/10.0/src/ProjectTemplates/Web.ProjectTemplates/content/ComponentsWebAssembly-CSharp/wwwroot

### What changed in 5.3.x after 5.3.0

These are patch releases. None of them renames a class or an attribute. From the release notes at https://github.com/twbs/bootstrap/releases:

- 5.3.3 fixed a colour-mode bug in the Sass build, where `variables-dark.scss` had to be imported by hand. This matters only if we compile Sass.
- 5.3.6 moved the docs to Astro. It also stopped children of `.visually-hidden` from becoming focusable.
- 5.3.7 fixed tooltip and popover behaviour with `trigger: "hover click"`.
- 5.3.8 fixed `color-contrast()` for WCAG 2.1. It also reverted a dropdown focus bug (https://blog.getbootstrap.com/).

### What v6 alpha changes

Source: the migration guide at https://getbootstrap.com/docs/6.0/guides/migration/ and the alpha post linked above.

**Browser floor.** Chrome and Edge 130, Firefox 132, Safari 18. v6 ships no fallbacks below these versions.

**JavaScript.** v6 ships ESM only. The script tag needs `type="module"`, and there is no `window.bootstrap` global. Popper is replaced by Floating UI, and `popperConfig` becomes `floatingConfig`. jQuery support is gone.

**Components renamed.**
- Modal becomes Dialog, built on native `<dialog>`. `.modal` becomes `.dialog` and `data-bs-toggle="modal"` becomes `"dialog"`. `.modal-backdrop` is replaced by `::backdrop`.
- Offcanvas becomes Drawer: `<dialog class="drawer">` with `data-bs-toggle="drawer"`.
- Dropdown becomes Menu. `.dropdown-menu` becomes `.menu`, `.dropdown-item` becomes `.menu-item`, and `data-bs-toggle="dropdown"` becomes `"menu"`.
- The navbar's narrow-screen region is now a Drawer, not a Collapse. `.navbar-expand-md` becomes `.md:navbar-expand`.

**Buttons.** `.btn-primary` becomes `class="btn-solid theme-primary"`. `.btn-outline-primary` becomes `class="btn-outline theme-primary"`.

**Utilities and breakpoints.**
- Responsive and state classes take a prefix: `.d-md-none` becomes `.md:d-none` and `.col-md-6` becomes `.md:col-6`.
- `.text-*` colours become `.fg-*`.
- `.rounded-*` and spacer numbers map to new values. Spacer 3 is now `0.5rem`, not `1rem`.
- Breakpoints move: `lg` goes to 1024px, `xl` to 1280px, and `xxl` becomes `2xl` at 1536px.

**Forms.** `.form-select` is removed. Put `.form-control` on the `<select>` instead.

**CSS.** All Bootstrap CSS sits in cascade layers, declared as `@layer colors, config, root, reboot, layout, content, forms, components, custom, helpers, utilities;`. I checked this in `dist/css/bootstrap.css` at the `v6.0.0-alpha.1` tag: https://github.com/twbs/bootstrap/blob/v6.0.0-alpha.1/dist/css/bootstrap.css. Colour modes use `light-dark()`.

**Size.** v6 alpha is larger than 5.3.8. Byte counts are from the GitHub contents API at each tag.

| File | 5.3.8 | 6.0.0-alpha.1 |
|---|---|---|
| `bootstrap.min.css` | 232,111 | 381,199 |
| `bootstrap.bundle.min.js` | 80,496 | 219,860 |

The v6 bundle includes Floating UI and Vanilla Calendar Pro, the datepicker dependency. See https://getbootstrap.com/docs/6.0/guides/migration/.

The alpha post says "details can change before beta". Class names learned against alpha.1 may change again before v6 is stable.

## 2. Ways to deliver the files

D12Canvas.App has no `package.json`, no `libman.json` and no service worker. I checked `git ls-files`. Its `index.html` loads only `css/app.css` and `_framework/blazor.webassembly.js`.

| Option | What it takes | Offline |
|---|---|---|
| Vendored files in `wwwroot/lib/...`, committed | Copy `dist/css` and `dist/js` from the release zip or npm tarball. This is what the .NET template does. | Same origin. If the App later becomes a PWA, these files go into `service-worker-assets.js` automatically. |
| CDN (jsDelivr) | One `<link>` and one `<script>` with SRI hashes. | Not cached by the Blazor PWA service worker, which caches only what is in `wwwroot` or listed by hand. The first load needs the network. |
| LibMan | `libman.json` plus the `Microsoft.Web.LibraryManager.Build` package to restore on build. | Same as vendored once restored. The build itself needs the network, or a warm LibMan cache. |
| npm | Node toolchain plus a copy step into `wwwroot`. | Same as vendored after the copy. It adds Node to a repo that has none. |

Sources and details:

- CDN tags for 5.3.8 with SRI hashes: https://getbootstrap.com/docs/5.3/getting-started/introduction/
  - CSS: `https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/css/bootstrap.min.css`, integrity `sha384-sRIl4kxILFvY47J16cr9ZwB07vP4J8+LH7qKQnuqkuIAvNWLzeN8tE5YBujZqJLB`
  - JS bundle with Popper: `https://cdn.jsdelivr.net/npm/bootstrap@5.3.8/dist/js/bootstrap.bundle.min.js`, integrity `sha384-FKyoEForCGlyvwx9Hj09JcYn3nv7wiPVlz7YYwJrWVcXK/BmnVDxM+D2scQbITxI`
- Icons CDN: `https://cdn.jsdelivr.net/npm/bootstrap-icons@1.13.2/font/bootstrap-icons.min.css`. npm package: `bootstrap-icons`. https://icons.getbootstrap.com/usage/ and https://github.com/twbs/icons/blob/main/README.md
- LibMan supports cdnjs, jsDelivr, unpkg and the file system. Microsoft says "LibMan isn't a package management system." https://learn.microsoft.com/en-us/aspnet/core/client-side/libman/?view=aspnetcore-10.0
- Restore on build is off by default. Turning it on adds the `Microsoft.Web.LibraryManager.Build` NuGet package. The page's sample manifest pulls `bootstrap@5.3.6` from unpkg. https://learn.microsoft.com/en-us/aspnet/core/client-side/libman/libman-vs?view=aspnetcore-10.0
- Blazor PWA offline caching: `service-worker-assets.js` lists the Blazor runtime files and "all resources for publishing to the app's `wwwroot` directory". Other files must be added with `ServiceWorkerAssetsManifestItem`. Offline support works only in published builds. https://learn.microsoft.com/en-us/aspnet/core/blazor/progressive-web-app/?view=aspnetcore-10.0
- The same page says offline support is relevant "if the primary data store is local to the browser", for example IndexedDB. That describes this App.

The App has no service worker today. So "works offline" depends on whatever the browser's HTTP cache keeps, for every delivery option. Vendoring is the only option that will work offline as-is once a service worker is added. The Microsoft docs don't say whether a CDN file in the HTTP cache is reliable offline. I'd not count on it.

## 3. Colour modes

### Bootstrap 5.3.8

- `data-bs-theme="light"` or `"dark"` on `<html>` sets the mode for the page. On any element it sets the mode for that subtree. https://getbootstrap.com/docs/5.3/customize/color-modes/
- **Following the system needs a script.** The docs say Bootstrap "does not yet ship with a built-in color mode picker". Their sample toggler:
  - stores `light`, `dark` or `auto` in `localStorage` under the key `theme`;
  - resolves `auto` with `matchMedia('(prefers-color-scheme: dark)')`;
  - re-applies on a `change` event when nothing explicit is stored.
- The docs suggest loading the toggler "at the top of your page to reduce potential screen flickering".
- The only no-script option is to recompile the Sass with `$color-mode-type: media-query`. That removes per-element `data-bs-theme` switching.
- In the compiled 5.3.8 CSS, `:root, [data-bs-theme=light]` sets no `color-scheme`. `[data-bs-theme=dark]` sets `color-scheme: dark`. I checked `dist/css/bootstrap.css` at tag v5.3.8.

Without a script, an OS in dark mode gets a light Bootstrap chrome. The D12Canvas canvas, by contrast, goes dark (see section 6).

### Bootstrap 6 alpha

- `:root` sets `color-scheme: light dark` and tokens use `light-dark()`. The docs say "By default Bootstrap already follows the visitor's system preference, so you don't need to do anything to get automatic dark mode." https://getbootstrap.com/docs/6.0/customize/color-modes/
- `data-bs-theme="light"` and `"dark"` still force a mode, by setting `color-scheme` on that subtree. The compiled alpha.1 CSS matches this.
- `auto` is not a CSS value. It exists only in the docs' toggler, which uses the same `theme` localStorage key.
- A script is needed only to let the user override the system choice and remember it.

### What this means for an App light/dark/system choice

On both versions, a remembered user choice needs a small script. It reads `localStorage` and sets `data-bs-theme` on `<html>`. It goes in `<head>` before Blazor starts, so the first paint has the right mode. The difference: on 5.3 the "system" option also needs that script to watch `prefers-color-scheme`. On v6 the attribute can simply be removed.

## 4. Which components need Bootstrap JS, and Blazor re-render problems

### Components that need JS (5.3.8)

The docs list these components as needing JS: accordion, alerts (dismiss), buttons (toggle), carousel, collapse, dropdowns, modals, navbar, tabs, offcanvas, scrollspy, toasts, tooltips and popovers. Only dropdowns, tooltips and popovers need Popper. `bootstrap.bundle.min.js` includes Popper. https://getbootstrap.com/docs/5.3/getting-started/introduction/

For the components this App might use:

- **Navbar on a narrow screen** extends Collapse or Offcanvas. Responsive offcanvas exists (`.offcanvas-lg` and so on). Above the breakpoint it shows inline. Below it, it needs JS to open. https://getbootstrap.com/docs/5.3/components/offcanvas/
- **Modal** removes body scroll and supports one modal at a time. The docs advise top-level markup because `position: fixed` misbehaves inside other fixed elements. `autofocus` has no effect, so focus needs JS on `shown.bs.modal`. https://getbootstrap.com/docs/5.3/components/modal/
- **Tooltips** "are opt-in for performance reasons, so you must initialize them yourself". They "must be hidden before their corresponding elements have been removed from the DOM". https://getbootstrap.com/docs/5.3/components/tooltips/
- In v6, tooltips initialise automatically from `data-bs-toggle="tooltip"`. A `selector` option on a parent covers elements added later. https://getbootstrap.com/docs/6.0/components/tooltip/

### Known problems with Blazor (or any framework that owns the DOM)

**Microsoft's guidance.** Learn says: "Only mutate the DOM with JavaScript (JS) when the object doesn't interact with Blazor." It adds that this "applies to ... any JS libraries that the app uses, including anything provided by a third-party framework, such as Bootstrap JS and jQuery." The result is "undefined behavior". https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0#interaction-with-the-dom

**Bootstrap's own warning.** Bootstrap says its JS "is not fully compatible with JavaScript frameworks like React, Vue, and Angular". It names the failure: "dropdowns that are stuck in the 'open' position". It recommends a framework-specific package instead. https://getbootstrap.com/docs/5.3/getting-started/javascript/. The v6 docs keep the same warning, with "menus" in place of dropdowns.

**What the 5.3.8 source does.** I checked https://github.com/twbs/bootstrap/tree/v5.3.8/js/src.
- The click handlers for dropdown, modal, collapse and offcanvas are delegated on `document`. So triggers that Blazor renders later still work without re-initialising.
- The plugins toggle classes such as `.show` and attributes such as `aria-expanded` on elements Blazor rendered. That is exactly the mutation Microsoft warns about.
- The modal adds `modal-open` to `<body>` and appends a `.modal-backdrop` div to `<body>`. Both sit outside Blazor's `#app` root.
- An open dropdown, on touch devices, adds no-op `mouseover` listeners to every child of `<body>`.

**Not settled by the sources.** I found no Bootstrap or Microsoft page that describes exactly what happens when Blazor re-renders, or removes, an element that Bootstrap has open. My inference from the source: if Blazor removes an open modal, `modal-open` and the backdrop stay on `<body>`. If Blazor rewrites a `class` attribute that Bootstrap changed, the `.show` class is lost. I have not tested either case.

**Relevant to the App's existing modals.**
- `SuggestionBox` already uses a native `<dialog>` element. That is the same base as v6's Dialog and Drawer, which rely on the browser for backdrop, focus trap and top layer (https://getbootstrap.com/docs/6.0/guides/migration/).
- `RenameBoardModal` is a Blazor `@if` block with an App-owned `.modal-backdrop` div. It uses no JS.

## 5. Bootstrap Icons: font vs sprite vs inline SVG

Sizes are from the twbs/icons repo at tag v1.13.2 (https://github.com/twbs/icons/tree/v1.13.2):

| Form | Files | Size |
|---|---|---|
| Icon font | `font/bootstrap-icons.min.css` + `font/fonts/bootstrap-icons.woff2` | 87,008 + 134,012 bytes (woff 180,288 as fallback) |
| SVG sprite | `bootstrap-icons.svg` | 1,123,048 bytes, all icons |
| Inline or individual SVG | one file per icon in `icons/` | per icon only |

Usage, from https://icons.getbootstrap.com/usage/ and https://icons.getbootstrap.com/sprite/:

- **Font.** `<i class="bi bi-alarm" aria-hidden="true"></i>`. The icon inherits text size and colour. One CSS file and one font file give access to every icon.
- **Sprite.** `<svg class="bi"><use href="/bootstrap-icons.svg#heart-fill"/></svg>`. The docs say to "serve the sprite from the same origin as the page", because browsers can block cross-origin `<use>`. A CDN sprite won't work.
- **Inline SVG.** Set width and height to `1em` to scale with text. The SVG uses `currentColor`. It adds no extra request, and the markup lives in the component.
- **Accessibility.** Decorative icons get `aria-hidden="true"`. "Meaningful standalone SVGs need an accessible name, and icon-only controls need a label on the control." The docs' example is `aria-label="Mute"` on the button. Icons inside `<img>` need `alt` text, or an empty `alt` if decorative.

The docs don't compare rendering quality or load cost between the forms. The trade-off in bytes is plain from the table. The font costs about 221 KB up front for every icon. Inline SVG costs only the icons used, but each one sits in the markup. The sprite is 1.1 MB unless trimmed. The icons docs give no step for trimming the sprite or font.

The icons package declares no dependency on Bootstrap CSS. It works the same with 5.3 or v6.

## 6. Conflicts with Blazor template CSS and with the canvas

### Blazor template CSS

The .NET 10 WASM template `app.css` (https://github.com/dotnet/aspnetcore/blob/release/10.0/src/ProjectTemplates/Web.ProjectTemplates/content/ComponentsWebAssembly-CSharp/wwwroot/css/app.css) does these things:

- overrides `.btn-primary` colours;
- sets a custom focus `box-shadow` on `.btn`, `.form-control` and `.form-check-input`;
- sets `h1:focus { outline: none; }`;
- adds `.valid.modified`, `.invalid` and `.validation-message` outlines.

D12Canvas.App's `app.css` keeps only the template's `#blazor-error-ui`, `.blazor-error-boundary` and `.loading-progress` rules. The rest is App-owned. So the template overrides above are not present.

The real overlap is the App's own class names. Bootstrap 5.3.8 defines `.btn`, `.btn-primary`, `.btn-secondary`, `.btn-danger`, `.btn-sm`, `.card`, `.modal-backdrop` and `.modal-title`. The App defines all of them too, in `app.css`. Load order decides which wins.

Bootstrap's `.modal-backdrop` sets `position: fixed`, `width: 100vw`, `height: 100vh`, `z-index: 1050` and `background-color: #000`. `RenameBoardModal` would pick these up.

In v6 alpha, `.btn-primary`, `.btn-secondary`, `.btn-danger`, `.modal-backdrop` and `.modal-title` don't exist. `.btn`, `.btn-sm` and `.card` do. I checked the compiled CSS at the tag.

### Reboot side effects

Reboot ships inside `bootstrap.min.css` and is global. From https://getbootstrap.com/docs/5.3/content/reboot/ and the compiled 5.3.8 CSS:

- `* { box-sizing: border-box }`
- `body` font, size, line-height (1.5), colour and background come from `--bs-body-*`. The font stack is `system-ui, -apple-system, "Segoe UI", Roboto, ...`.
- `p` gets `margin-bottom: 1rem`. Headings get `margin-bottom: .5rem` and a tighter line-height.
- `label { display: inline-block }`
- `button { border-radius: 0 }`
- `input, button, select, textarea` get `font-family/size/line-height: inherit`.
- `textarea` resizes vertically only.

### The canvas (`lib/D12Canvas`)

Here is what I found in the library source:

- Its class names are its own: `diagram-container`, `container-content` and so on. None matches a Bootstrap class.
- Its custom properties are `--d12-*` and Bootstrap's are `--bs-*`, so they don't collide.
- Its components set `box-sizing: border-box` already.
- `StickyNote` and `Text` use `font-family: inherit`. Under Reboot they would inherit Bootstrap's body font, not the App's current Helvetica stack. A text box on the canvas would render in a different font.
- The library uses `<button>`, `<input>`, `<select>`, `<textarea>`, `<label>`, `<p>`, `<h3>` and `<ul>`. Reboot touches all of them. Where a library `<style>` block doesn't set a property (margins, `border-radius`, line-height), Reboot's value applies.
- z-index: the library's `ContextMenu` uses 1000 and `PropertyBar` uses 900. Bootstrap 5.3.8 uses dropdown 1000, offcanvas 1045, backdrop 1050, modal 1055, popover 1070, tooltip 1080 and toast 1090. A Bootstrap dropdown and the canvas context menu tie at 1000.
- In v6 the scale moves to menu 1000, drawer 1030 and dialog 1040. Native `<dialog>` opened modally renders in the browser's top layer, above all z-index.

**Theme attributes.** The canvas themes itself with `[data-d12-theme="light|dark"]` on itself or any ancestor, and with `@media (prefers-color-scheme: dark)` when there is no attribute. See `lib/D12Canvas/DiagramCanvas.razor` and `RaisedSurface.cs`. `BoardEditor.razor` sets `data-d12-theme` only for an explicit light or dark choice. Bootstrap reads `data-bs-theme`. These are two separate attributes, so an App-level choice must set both. With "system" and Bootstrap 5.3, the canvas follows the OS but Bootstrap stays light unless a script sets `data-bs-theme`. With v6, both follow the OS without an attribute.

**v6 cascade layers.** In v6 all Bootstrap rules sit in `@layer`. Under standard CSS cascade rules, unlayered styles beat layered ones whatever their specificity. The App's `app.css` and the library's `<style>` blocks are unlayered, so they would override Bootstrap v6, Reboot included. Bootstrap's docs don't state this consequence. It follows from the CSS cascade spec, not from a Bootstrap source.

## Open questions the sources did not settle

- The v6 stable date, and how many class names will still change after alpha.1. The alpha post says "details can change before beta".
- When v5.4.0 ships, and whether it changes anything we would use.
- What happens when Blazor re-renders or removes an element that Bootstrap JS currently holds open. Microsoft says "undefined behavior". Neither project documents the specific failures beyond "stuck open". This needs a prototype to observe.
- Whether a CDN copy in the browser HTTP cache is reliable offline without a service worker. The Microsoft PWA docs cover only service-worker caching of `wwwroot` files.
- Whether .NET 10 Blazor WebAssembly's supported browsers match v6's floor (Chrome 130, Firefox 132, Safari 18). I did not check Blazor's supported-browser list.

## Implications for ticket 02 and 05

These are the facts those decisions hinge on. I have not made the decisions.

- 5.3.8 is the only stable Bootstrap. v6 is alpha.1, its class names may change before beta, and it has no stable date.
- Class names differ a lot between 5.3 and v6: modal/dialog, offcanvas/drawer, dropdown/menu, `.btn-primary`/`.btn-solid theme-primary`, and infix/prefix responsive classes. Markup written for one needs rework for the other.
- The App's `.btn*`, `.card`, `.modal-backdrop` and `.modal-title` rules collide with Bootstrap 5.3 classes. Fewer collide with v6.
- In 5.3, "system" colour mode needs a head script. In v6 it needs none. Remembering a user's choice needs a small head script on either version.
- The canvas reads `data-d12-theme` and Bootstrap reads `data-bs-theme`. One App setting has to drive both.
- Microsoft says not to let Bootstrap JS mutate Blazor-rendered DOM. Bootstrap says its JS isn't fully compatible with DOM-owning frameworks. Navbar collapse/offcanvas, dropdowns, modals and tooltips all depend on that JS in 5.3. v6 Dialog and Drawer sit on native `<dialog>`, which `SuggestionBox` already uses.
- Only vendored or LibMan-restored files in `wwwroot` end up in a future Blazor service-worker cache. A CDN does not. The App has no service worker today.
- Reboot changes the font that canvas text inherits (`StickyNote`, `Text`). It also restyles the bare `button`, `label`, `p` and `textarea` elements the library uses. v6's layers would let the library's unlayered styles win. 5.3 has no layers.
- Icons 1.13.2: the font costs about 221 KB for all icons. The sprite is 1.1 MB and must be same-origin. Inline SVG costs only what is used.
