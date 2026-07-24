# Hot Reload Support

To use Hot Reload with this Blazor WebAssembly project:

1. Run the app using Visual Studio 2022+ or `dotnet watch`:

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
never directly on a dev machine:

```bash
docker run --rm -v "$PWD:/workspace" -w /workspace mcr.microsoft.com/playwright/dotnet:v1.61.0-noble \
  bash -c "dotnet tool restore && dotnet test --project D12Canvas.VisualTests/D12Canvas.VisualTests.csproj"
```

### Updating baselines

When an intentional visual change breaks the diff:

1. Run the visual tests as above. A changed rendering fails the affected test and writes a
   `*.received.png`/`*.received.html` pair next to the existing `*.verified.*` files.
2. Inspect the `.received.*` output and confirm the new rendering is correct.
3. Overwrite the matching `.verified.*` file with the `.received.*` one (then delete the
   `.received.*` file) and commit both in the same PR.
4. Ordinary PR review is the approval gate — there's no separate baseline-approval tool.

