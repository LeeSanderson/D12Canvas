# Testing

Two layers, per the [layered testing strategy](../../.scratch/d12canvas-next/issues/04-layered-testing-strategy.md) - see the root `README.md`'s own Testing section for the fuller rationale and the baseline-update process. This doc is the command-line reference plus the commit-gating rule.

## Build

`dotnet build` builds the whole solution. A `Directory.Build.props` pre-build target runs `dotnet csharpier --check .` and fails the build on anything unformatted - run `dotnet csharpier .` first whenever you're not sure the tree is clean.

## bUnit (component logic, markup, state)

Full suite:

    dotnet test D12Canvas.Tests/D12Canvas.Tests.csproj

For a single class, or to pass other MTP runner flags (e.g. `-parallel none`): `dotnet test -- <args>` is unreliable for this project's xunit v3 MTP setup. Build once, then invoke the built exe directly:

    dotnet build D12Canvas.Tests/D12Canvas.Tests.csproj
    ./D12Canvas.Tests/bin/Debug/net10.0/D12Canvas.Tests.exe -class D12Canvas.Tests.SomeTestClass

Pointer gestures are driven through the canvas's four public entry points (`CanvasPointer` in the test project wraps them), never by dispatching mouse events at the canvas. `DiagramCanvasPointerGestureTests` enumerates the closed `GestureKind` set with one release-reliability case and one cancel case per member; adding a gesture fails that theory until its cases exist.

## Playwright visual tests (rendered visual states)

**Standing rule (unchanged from the README):** any change that adds or changes a rendered visual state on the canvas needs a new/updated screenshot case in `D12Canvas.VisualTests`.

**Commit-gating rule:** before committing any change of that shape - a `.razor`/`.razor.cs` edit that touches rendered markup or a shared `<style>` block, or a `ComponentContainer`/`DiagramCanvas` parameter that affects what's rendered - run the full visual-test suite and fold any resulting baseline updates into the same commit. Don't defer this to CI. This applies regardless of how the change was made - a `/implement` run, a direct prompt, anything - not just tickets. Pure logic changes with no rendering impact (services, models, commands, non-visual state) don't need this.

Always run visual tests inside the pinned Playwright Docker image, never directly on a dev machine (font/AA rendering differs enough to cause false-positive diffs). If Docker Desktop's own service won't start (e.g. no admin rights), Podman is a drop-in substitute:

    podman machine start
    docker context use default

From the repository root, wipe stale build artifacts first (a stale local `obj`/`bin` bind-mounted into the container produces spurious Blazor scoped-CSS diffs - see ticket 78):

    find . -type d \( -name obj -o -name bin \) -exec rm -rf {} +

Then build and run. On Git Bash for Windows, `-v $PWD:/workspace`-style volume arguments get mangled - use `pwd -W` for the host side and a double leading slash on the container-side workdir instead:

    docker run --rm -v "$(pwd -W):/workspace" -w //workspace mcr.microsoft.com/playwright/dotnet:v1.61.0-noble \
      bash -c "dotnet tool restore && dotnet build D12Canvas.VisualTests/D12Canvas.VisualTests.csproj && ./D12Canvas.VisualTests/bin/Debug/net10.0/D12Canvas.VisualTests -parallel none"

**Always pass `-parallel none`** (on the exe directly - `dotnet test -- -parallel none` doesn't forward reliably here either). The suite opens many Playwright browser contexts against one shared `D12Canvas.Demo` process; under default parallelism, tests fail with symptoms that look like real regressions - large pixel/HTML diffs, a Locator timing out at 0 elements, a click intercepted by an overlapping element - but aren't. Before trusting any visual-test failure (or spending time updating baselines over it), reproduce it under `-parallel none` first.

### Interaction probes

An interaction probe drives the browser and asserts DOM or interop state, never pixels, so it has no baseline. Probes live in `D12Canvas.VisualTests` beside the screenshot tests, run in the same container with the same `-parallel none` command, and share the demo-app and Playwright assembly fixtures. To run only the probes, build as above and pass one `-class` per probe class:

    ./D12Canvas.VisualTests/bin/Debug/net10.0/D12Canvas.VisualTests -parallel none -class D12Canvas.VisualTests.ReleaseChannelProbes

To add one, derive from `InteractionProbe`. It opens `/interaction-probe-demo`, a board seeded with fixed entity ids, and gives the probe four things:

- every call the page's JavaScript makes into a .NET object, recorded before dispatch, read with `CallsToAsync("OnPointerMoved")` after `SettleAsync()`, which waits for pending calls and two animation frames
- `ExpectNothingRespondsToAButtonlessMoveAsync()`, which sweeps an unpressed pointer over the board and fails if any pointer entry point is called or the viewport, selection or marquee changes
- a console-error trap that fails the test on dispose if the page logged a `console.error` or threw; a probe that provokes one on purpose asserts it and then calls `ConsoleErrors.Clear()`
- `PagePointOnCanvasAsync`, which turns container pixels into page coordinates, and `StartMiddlePanAsync`, which leaves a pan live on empty canvas

A probe asserts what the user would see or what crossed the interop boundary. It never asserts which gesture is live, and it never establishes a magnitude: synthetic input does not have a real device's granularity, so a distance or delta in a probe sits well clear of the constant it is near rather than measuring it. Before trusting a new probe, break the thing it guards in the listener and watch it fail.

### Updating baselines

See the root `README.md`'s "Updating baselines" section for the full step-by-step. In short: a real diff writes `*.received.png`/`*.received.html` next to the existing `*.verified.*` files - inspect the `.received.*` output, confirm the new rendering is correct, then overwrite the matching `.verified.*` file with it (delete the `.received.*` after) and commit both.

Two things are scrubbed out of the HTML snapshot before comparison (see `ModuleInitializer`): entity ids, which appear as `Guid_1`, `Guid_2` and so on in order of first appearance because a demo page seeds fresh ids on every load, and Blazor's `_bl_N` element-reference attributes, whose numbering is not stable on a page with two canvases. Verify writes a received copy of every target when a test fails, so a `.received.png` beside a changed `.received.html` is not by itself evidence that the screenshot changed; compare it before folding it.
