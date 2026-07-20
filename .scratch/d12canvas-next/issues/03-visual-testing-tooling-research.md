# Blazor/bUnit visual & render-verification tooling options

Type: research
Status: resolved

## Question

The current test suite (bUnit component tests) verifies component logic but not visual/rendered output. What tooling exists for layered testing of Blazor apps that includes visual/render verification — e.g., spinning up the Demo host app and asserting on rendered output (screenshot diffing, Playwright-driven browser assertions, Percy/Chromatic-style visual regression, or bUnit's own rendered-markup assertions)? What do comparable Blazor OSS component-library projects use? Surface concrete, evaluable options — with setup cost and CI implications — for the layered testing strategy ticket to decide between.

## Answer

Layered approach, findings support:

- **Layer 1 (existing): bUnit** stays for logic/markup assertions. Microsoft's own Blazor testing docs (written by bUnit's creator, Egil Hansen) state plainly that bUnit doesn't execute JS/CSS/real rendering, and name Playwright for .NET as the tool for that gap.
- **Layer 2 (new): Playwright for .NET**, driving the real `D12Canvas.Demo` WASM app in headless Chromium, using `Expect(page).ToHaveScreenshotAsync(...)` for a small, curated set of visual states (rendered nodes/edges, a zoomed/panned view, mid-drag) — not a full pixel-diff suite per interaction. Run via Playwright's official Docker image (`mcr.microsoft.com/playwright/dotnet`) to neutralize cross-platform font/rendering noise; baselines committed to the repo, no external service needed.
- **Skip Percy and Chromatic.** Percy has a genuine first-party .NET Playwright SDK (`PlaywrightPercySDK`) but adds a paid SaaS dependency with no benefit over Playwright's free local diffing at this scale. Chromatic requires Storybook, which has no mature Blazor integration.
- No comparable Blazor OSS project inspected (Blazor.Diagrams, MudBlazor, Radzen Blazor) runs any real-browser/visual-regression testing today — all are bUnit-only in CI, confirmed by direct inspection of their CI workflows and a code search for "playwright" (zero hits in all three). This is a genuine gap to fill, not a wheel to reinvent — MudBlazor's own community has an open discussion asking for exactly this.

Full findings, source citations, and setup-cost/CI detail: `research/visual-testing-tooling` branch, `.scratch/d12canvas-next/research/visual-testing-tooling.md` (commit `7c20538`).
