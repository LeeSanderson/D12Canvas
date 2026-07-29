# 81 — Playwright visual-test baselines miss the Demo app's nav sidebar entirely, on a clean checkout

**What's wrong:** On this dev machine, running `D12Canvas.VisualTests` renders every Demo page
without its left nav sidebar (`D12Canvas.Demo`'s standard Blazor-template layout chrome) at all -
not a collapsed/hamburger state, just absent - while every checked-in `.verified.png`/`.verified.html`
baseline shows it. This affects both the HTML snapshot and the pixel screenshot, and survives
ticket 80's fuzzy PNG comparer (this isn't sub-pixel jitter; it's a structural layout difference the
comparer correctly still flags).

**Status:** needs-triage

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
