# Hot Reload Support

To use Hot Reload with this Blazor WebAssembly project:

1. Run the app using Visual Studio 2022+ or `dotnet watch`:

# Theming

Everything the library paints itself (the grid, selection chrome, LOD placeholder, connector
drag-preview, edges, guides, the built-ins' themed defaults, and the palette, context menu, property
panel, property bar and minimap — see `docs/adr/0012-canvas-chrome-theming-contract.md` and
`docs/adr/0063-canvas-and-container-chrome-colours.md`) is styled through a small set of CSS custom properties rather than a C# theming API. Override them
with plain CSS; no host-side registration or parameter is required.

| Token | Used for |
| --- | --- |
| `--d12-surface` | Grid background, LOD placeholder fill, palette/context-menu background |
| `--d12-border` | Grid line color, LOD placeholder border, palette/context-menu border and hover state |
| `--d12-accent` | Selection: the marquee, the selected outline, the selection box and its handles, resize handles, the drag-over affordance, the edge halo and keyboard focus rings |
| `--d12-muted-text` | Muted/secondary chrome text (category titles, LOD placeholder label) |
| `--d12-text` | Primary chrome text (palette entry names, context-menu item labels) |
| `--d12-board-text` | A `Text` instance's colour when its `Color` prop is null (no author opinion) |
| `--d12-board-fill` | A `Rectangle` instance's fill when its `FillColor` prop is null |
| `--d12-board-stroke` | A `Rectangle` instance's stroke when its `StrokeColor` prop is null |
| `--d12-alignment-guide` | Alignment and equal-spacing guides drawn while object snapping matches, at half intensity; deliberately not the accent |
| `--d12-edge` | An edge's stroke and arrowheads when the edge has no colour of its own |
| `--d12-canvas-frame` | The canvas container's border; set it to `transparent` for a full-bleed canvas with no frame |
| `--d12-inline-edit-outline` | The dashed outline around a `Text` or `StickyNote` while it is being edited inline |
| `--d12-connector-preview` | The connector drag-preview line and the port dots it starts from |
| `--d12-shadow` | The drop shadow under the context menu and the property bar |

A colour an author or user has set on a component instance is never themed through this layer:
it stays an ordinary `TProps` value and is painted literally in both themes. The three
`--d12-board-*` tokens are what a built-in paints when that value is null, meaning nobody has
chosen one. Any registered component can fall back to the same public tokens in its own CSS.

Four elements use a one-off escape-hatch custom property instead of the shared set, because each
must diverge from a shared token. The connector drag-preview and the ports it starts from use
`--d12-connector-preview`, a deliberate departure from `--d12-accent` (which already means
"selected"). The canvas frame uses `--d12-canvas-frame`, because the canvas's `--d12-border` is the
grid-line colour. The inline editor's outline uses `--d12-inline-edit-outline`, four times stronger
than `--d12-border`. The drop shadow uses `--d12-shadow` on the context menu's and the property
bar's own roots. `--d12-connector-preview` is declared once and is the same in both themes; the
other three have a dark value of their own.

Three colours stay literals by decision rather than theme: the orange outline on a focused port and
the orange fill of a floating edge end (`#f39c12`), and the white ring around a port dot and a
floating edge end (`#ffffff`). They signal state and read on both backdrops. A test fails on any
other colour literal in the library's style blocks outside a token declaration.

`DiagramCanvas`, `Palette`, `ContextMenu`, `PropertyPanel`, `PropertyBar` and `Minimap` each declare
their own light and dark defaults for these tokens on their own root (`.diagram-container`,
`.d12-palette`, `.d12-context-menu`, `.d12-property-panel`, `.d12-property-bar` and `.d12-minimap`
respectively), not a global `:root`, so each renders correctly themed even
standalone, and independent instances on the same page can carry different themes.

- **Automatic**: `prefers-color-scheme: dark` switches to the dark defaults with no host code
  required.
- **Explicit override**: set `data-d12-theme="light"` or `data-d12-theme="dark"` on a chrome
  element's own container or any ancestor to force that theme regardless of the OS preference.
  Nesting two conflicting values (a `data-d12-theme="dark"` ancestor inside a
  `data-d12-theme="light"` one, or vice versa) is unsupported — the theme that wins is whichever
  value's CSS rule happens to be declared later, not necessarily the nearer ancestor.

# Testing

Two layers, per the project's [layered testing strategy](.scratch/d12canvas-next/issues/04-layered-testing-strategy.md):

- **bUnit** (`D12Canvas.Tests`) — the default for component logic, markup, event wiring, and state
  transitions. Run with `dotnet test --project D12Canvas.Tests/D12Canvas.Tests.csproj`.
- **Playwright for .NET** (`D12Canvas.VisualTests`) — screenshot-diff coverage of rendered visual
  states (layout, CSS positioning, zoom/pan) that bUnit can't see, driven against the real
  `D12Canvas.Demo` app. Baselines are the committed `*.verified.png`/`*.verified.html` files
  alongside the tests, taken of each demo page's content area with style elements scrubbed from
  the HTML, generated and diffed via
  [Verify.Playwright](https://github.com/VerifyTests/Verify.HeadlessBrowsers). The same project
  holds the interaction probes, which drive the browser and assert DOM or interop state rather
  than pixels; see `docs/agents/testing.md`.

### Standing rule

Any ticket that introduces or changes a rendered visual state on the canvas must add or update a
screenshot case in `D12Canvas.VisualTests`. Purely internal tickets (data shape, serialization,
non-visual state logic) don't need one — unless their resolution introduces a new visual state as
a side effect.

### Running the visual tests locally

Font/anti-aliasing rendering differs enough across OSes to produce false-positive diffs, so the
visual tests always run inside the official Playwright Docker image — the same image CI uses —
never directly on a dev machine. The SDK version is pinned in `global.json` to match what that
image bundles; install the same SDK locally if you ever need to run `D12Canvas.Tests` outside a
container.

Because the container bind-mounts your working directory, it inherits any stale `obj`/`bin`
build artifacts already sitting on the host - which can leave Blazor's scoped-CSS bundle out of
sync and produce spurious baseline diffs unrelated to any real change (see ticket 78). Always wipe
build artifacts first:

```bash
find . -type d \( -name obj -o -name bin \) -exec rm -rf {} +
docker run --rm -v "$PWD:/workspace" -w /workspace mcr.microsoft.com/playwright/dotnet:v1.61.0-noble \
  bash -c "dotnet tool restore && dotnet build D12Canvas.VisualTests/D12Canvas.VisualTests.csproj && ./D12Canvas.VisualTests/bin/Debug/net10.0/D12Canvas.VisualTests -parallel none"
```

Always pass `-parallel none`: the suite opens many Playwright browser contexts against one shared
`D12Canvas.Demo` process, and under default parallelism tests fail with symptoms that look like
real regressions (large pixel/HTML diffs, a Locator timing out at 0 elements, a click intercepted
by an overlapping element) but aren't. Reproduce any failure under `-parallel none` before trusting
it. See `docs/agents/testing.md` for the full CLI reference (including the Git-Bash-on-Windows
volume-path quoting this command needs, and a Podman fallback if Docker Desktop won't start).

### Updating baselines

When an intentional visual change breaks the diff:

1. Run the visual tests as above. A changed rendering fails the affected test and writes a
   `*.received.png`/`*.received.html` pair next to the existing `*.verified.*` files.
2. Inspect the `.received.*` output and confirm the new rendering is correct.
3. Overwrite the matching `.verified.*` file with the `.received.*` one (then delete the
   `.received.*` file) and commit both in the same PR.
4. Ordinary PR review is the approval gate — there's no separate baseline-approval tool.

