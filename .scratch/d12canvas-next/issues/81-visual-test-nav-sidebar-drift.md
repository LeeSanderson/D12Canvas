# 81 — Playwright visual-test baselines miss the Demo app's nav sidebar entirely, on a clean checkout

**What's wrong:** On this dev machine, running `D12Canvas.VisualTests` renders every Demo page
without its left nav sidebar (`D12Canvas.Demo`'s standard Blazor-template layout chrome) at all -
not a collapsed/hamburger state, just absent - while every checked-in `.verified.png`/`.verified.html`
baseline shows it. This affects both the HTML snapshot and the pixel screenshot, and survives
ticket 80's fuzzy PNG comparer (this isn't sub-pixel jitter; it's a structural layout difference the
comparer correctly still flags).

**Status:** resolved

**How it was found:** While implementing ticket 52 (per-edge routing/arrowheads), several
pre-existing baselines needed regenerating because the ticket's new default arrowhead changes their
edges' appearance. The freshly-captured screenshots were missing the nav sidebar compared to the
committed baselines. To rule out ticket 52's own changes as the cause, ran a control test
(`PaletteVisualTests`, whose page never renders a `DiagramCanvas` at all) and it failed identically.
To rule out any uncommitted change at all, `git stash`-ed everything back to a clean checkout of
ticket 52's own starting commit (`6153c16`, "Resolve ticket 51: edges persist") and reran
`SelectionVisualTests`/`PaletteVisualTests` directly against that - both still failed the same way.
This confirms the drift predates ticket 52 entirely and isn't caused by any code in this repo's
working tree; it's specific to this machine/environment right now.

**Scope of impact:** Essentially every visual test in the suite fails on this machine right now for
this reason (confirmed via a full `dotnet test D12Canvas.VisualTests` run: ~20 of 24 baselines
mismatch). This makes local visual-test verification on this machine unreliable as a signal - a
result here shouldn't be trusted without independently confirming CI (or another clean environment)
passes too.

**Why not fixed as part of ticket 52:** Out of scope - ticket 52 only touches edge rendering
(`DiagramCanvas.razor`'s `.edges-layer` SVG and its own `<style>` block); it doesn't touch
`MainLayout.razor`/`NavMenu.razor`/`App.razor` or anything else that could plausibly cause a missing
nav sidebar. Fixing ~20 unrelated baselines under this ticket's diff would also make ticket 52's own
diff impossible to review cleanly against what it actually changed.

**Not yet investigated:** the actual root cause (a Blazor WASM cold-start timing issue where the
screenshot is captured before nav CSS/markup finishes hydrating; a stale `wwwroot` build artifact,
per ticket 78's "mandate clean baseline builds" precedent; a SDK/Playwright-browser version drift
since ticket 78 pinned things; or something else entirely). Same family of "this machine's local
render doesn't match CI/another host" issue as tickets 78-80, but a different symptom (missing
layout chrome, not color/AA drift) - worth checking whether a clean `dotnet build` + fresh
`D12Canvas.Demo` wwwroot output resolves it before assuming it's environmental.

**Resolution:** confirmed environmental, not a product bug - and specifically tested the "Not yet
investigated" hypothesis above rather than assuming it. Three runs, each wiping every `obj`/`bin`
directory first (ticket 78's own precedent):

1. Clean build inside the pinned `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` image via
   Podman: all 71 tests in the full suite passed, byte-identical, nav sidebar included - no
   baseline updates needed.
2. A clean `dotnet build` + fresh `D12Canvas.Demo` output, run directly on this host (no Docker at
   all) - exactly the hypothesis this ticket asked to check: `PaletteVisualTests` still failed, but
   *not* with a missing nav sidebar. The captured HTML has the full `<nav class="nav
   flex-column">...` sidebar markup, byte-for-byte identical to the baseline's, under a different
   scoped-CSS hash prefix (`b-52l6hnzelf` vs `b-k7rv4lobv5`) - i.e. ticket 78's already-diagnosed
   SDK-feature-band hash skew, not this ticket's claimed symptom.
3. A first attempt at (2) without wiping artifacts between an earlier Docker run and the host build
   produced the same hash-mismatch failure, confirming it's that contamination (also ticket 78's
   territory), not something new.

So a clean build - on this host, exactly as the ticket asked - does not reproduce "nav sidebar
entirely absent." The only failure mode reproducible on this machine at all is ticket 78's
already-fixed-elsewhere symptom, and running inside the pinned Docker image (as
`docs/agents/testing.md` already mandates) eliminates even that. Whatever produced the originally
reported symptom is not reproducible via any route tried here, including the specific one this
ticket named. No code change made - closing as a reinforcement of the existing "always run inside
Docker/Podman" rule rather than a new fix.
