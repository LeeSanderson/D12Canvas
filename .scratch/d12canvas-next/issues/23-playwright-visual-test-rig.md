# 23 — Playwright visual-test rig in CI

**What to build:** A small, curated screenshot-diff layer runs against the real Demo app — locally and in CI via the official Playwright Docker image. First cases cover the rendered board and zoom/pan states. This enables the standing rule from the layered testing strategy: any later ticket that renders a new visual state on canvas must add a screenshot case.

**Blocked by:** 22 (Canvas renders a Board)

**Status:** resolved

- [x] A Playwright for .NET suite runs against the Demo app locally and in CI
- [x] Baseline screenshots exist for a rendered board and at least one zoomed/panned state
- [x] A screenshot diff failure fails the build; the baseline-update workflow is documented
- [x] The standing rule (new visual state → new screenshot case) is recorded where contributors will see it

## Comments

New `D12Canvas.VisualTests` project drives the real `D12Canvas.Demo` app: an `IAsyncLifetime` assembly
fixture (`DemoAppFixture`) boots it via `dotnet run` on a fixed port and polls until it responds; a
second assembly fixture (`PlaywrightFixture`) launches one shared headless Chromium instance, with each
test getting its own `IBrowserContext`/`IPage`. Two baseline cases: the seeded board rendered as-is, and
a zoomed+panned variant driven through `DiagramCanvas`'s existing keyboard shortcuts (`PageUp`/arrow
keys) rather than raw mouse coordinates, for determinism.

**Correction to ticket 03's research:** .NET Playwright has no `ToHaveScreenshotAsync` — that API is
JS-only (`@playwright/test`'s own snapshot runner); `IPageAssertions`/`ILocatorAssertions` in
`Microsoft.Playwright` 1.61.0 have no screenshot method at all. Used
[Verify.Playwright](https://github.com/VerifyTests/Verify.HeadlessBrowsers) instead — it fills the same
gap for .NET (screenshot capture + committed baseline diffing) and produces both a `.verified.png` and a
`.verified.html` markup dump per case, which makes reviewing *why* a diff changed much easier than a
pixel image alone. `docs/adr` untouched — this is tooling, not an architectural decision.

**Test framework changed mid-ticket:** started on NUnit (`Microsoft.Playwright.NUnit` is Microsoft's only
official Playwright test-framework adapter besides MSTest), then switched to xUnit v3 at the user's
request for consistency with `D12Canvas.Tests`, hand-rolling the browser/page lifecycle via xUnit v3's
new `IAsyncLifetime` + `[assembly: AssemblyFixture]` support (also new in v3) instead of a
`PageTest`-style base class. This pulled `D12Canvas.Tests` along too: bumped to net10.0, `xunit` 2.9.2 →
`xunit.v3.mtp-v2`, `bunit` 1.39.5 → `bunit` 2.8.4-preview (renames `TestContext`→`BunitContext`,
`RenderComponent<T>`→`Render<T>`). Both test projects now run on the `Microsoft.Testing.Platform`
`dotnet test` runner (`global.json` pins this repo-wide). Confirmed via bUnit's own migration guide that
the `TestContext` rename exists specifically because xUnit v3 introduced its own ambient
`Xunit.TestContext` — the two would otherwise collide.

**Known gap, not acted on:** `coverlet.collector` was dropped from `D12Canvas.Tests.csproj` rather than
carried forward — it hooks into the legacy VSTest data-collector protocol, which the MTP runner doesn't
use, so it would have been dead weight. No coverage tool was added in its place (e.g.
`Microsoft.Testing.Extensions.CodeCoverage`); nothing was collecting coverage before this ticket either
(no CI existed), so this isn't a regression, but a future ticket wanting coverage numbers will need to
wire that up fresh under MTP rather than assume coverlet still works.

**CI (`.github/workflows/ci.yml`):** first workflow in this repo (`.github/workflows/` was empty before
this ticket). `unit-tests` job runs bUnit on a plain `ubuntu-latest` runner; `visual-tests` job runs
inside `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` (`container:`), uploading `*.received.*` as a
build artifact on failure. Also added `.config/dotnet-tools.json` pinning `csharpier` (0.30.6) as a local
tool — it was previously only a global tool on the dev machine, which would have made every CI build
fail on the `Directory.Build.props` formatting check.

**Baseline generation:** generated inside the real `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble`
container (not on the dev machine) per ticket 04's own reasoning — cross-platform font/rendering
differences make host-machine-generated baselines unreliable against CI. Docker Desktop's backend
couldn't start in this sandbox (no admin rights to control its Windows service); used the machine's
existing Podman installation instead (`podman machine start`, then pointed the `docker` CLI at its
forwarded `docker_engine` pipe via `docker context use default`) — functionally identical to Docker
Desktop for this purpose.
