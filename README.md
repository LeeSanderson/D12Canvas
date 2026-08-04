# Hot Reload Support

To use Hot Reload with this Blazor WebAssembly project:

1. Run the app using Visual Studio 2022+ or `dotnet watch`:

# Theming

Canvas chrome (the grid and the selection marquee, so far — see
`docs/adr/0012-canvas-chrome-theming-contract.md`) is styled through a small set of CSS custom
properties rather than a C# theming API. Override them with plain CSS; no host-side registration
or parameter is required.

| Token | Used for |
| --- | --- |
| `--d12-surface` | Grid background |
| `--d12-border` | Grid line color |
| `--d12-accent` | Selection marquee outline and fill |
| `--d12-muted-text` | Muted/secondary chrome text |

`DiagramCanvas` declares its own light and dark defaults for these tokens on its own root
(`.diagram-container`), not a global `:root`, so it renders correctly themed even standalone, and
two canvas instances on the same page can carry different themes.

- **Automatic**: `prefers-color-scheme: dark` switches to the dark defaults with no host code
  required.
- **Explicit override**: set `data-d12-theme="light"` or `data-d12-theme="dark"` on the canvas's
  own container element or any ancestor to force that theme regardless of the OS preference. Nesting
  two conflicting values (a `data-d12-theme="dark"` ancestor inside a `data-d12-theme="light"` one,
  or vice versa) is unsupported — the theme that wins is whichever value's CSS rule happens to be
  declared later, not necessarily the nearer ancestor.

Remaining chrome (palette, LOD placeholder, connector drag-preview, selection context menu) is
tracked separately for token adoption.

# Testing

Two layers, per the project's [layered testing strategy](.scratch/d12canvas-next/issues/04-layered-testing-strategy.md):

- **bUnit** (`D12Canvas.Tests`) — the default for component logic, markup, event wiring, and state
  transitions. Run with `dotnet test --project D12Canvas.Tests/D12Canvas.Tests.csproj`.
- **Playwright for .NET** (`D12Canvas.VisualTests`) — screenshot-diff coverage of rendered visual
  states (layout, CSS positioning, zoom/pan) that bUnit can't see, driven against the real
  `D12Canvas.Demo` app. Baselines are the committed `*.verified.png`/`*.verified.html` files
  alongside the tests, generated and diffed via
  [Verify.Playwright](https://github.com/VerifyTests/Verify.HeadlessBrowsers).

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

