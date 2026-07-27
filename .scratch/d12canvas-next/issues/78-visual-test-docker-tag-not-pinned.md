# 78 — Visual-test baselines can silently invalidate because the Docker tag isn't pinned by digest

**What's wrong:** `README.md`'s documented baseline workflow pulls
`mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` by tag. Docker tags aren't immutable snapshots -
Microsoft can (and did, between whenever ticket 40's baselines were generated and this session) push an
updated image under the same tag with a newer bundled .NET/Razor SDK patch version. That SDK bump
changed the deterministic scoped-CSS class-name hash formula Blazor emits (e.g. `b-52l6hnzelf` became
`b-k7rv4lobv5` for the exact same `MainLayout`/`NavMenu` markup), which broke every single HTML-based
Playwright baseline in `D12Canvas.VisualTests` at once - not just the ones touched by whatever ticket is
in flight.

**Status:** needs-triage

**How it was found:** Regenerating Playwright visual-test baselines for ticket 41 (Text built-in). All
16 HTML/PNG baseline pairs failed on the first regeneration run, not just the 9 files whose
`.d12-palette-entry`/`.component-container` count assertions ticket 41 intentionally changed. Diffing
showed every failure carried the identical scoped-CSS hash substitution regardless of which test it was.
Reproduced identically across two independent from-scratch container runs (ruling out stale local build
artifacts - the same class of problem ticket 39 hit and fixed with `dotnet clean`). Confirmed as
environment drift rather than a ticket-41 regression by checking out ticket 40's unmodified commit into
a scratch worktree and running the exact same pinned tag against it: the unmodified code also fails its
own already-committed baseline, with the identical hash values.

**Why not fixed as part of ticket 41:** Out of scope - ticket 41 only adds the Text built-in. The actual
palette/component-container changes were confirmed correct (via hash-normalized diffing) and accepted
alongside the incidental hash churn, since there's no way to regenerate baselines through the pinned
tag while excluding an SDK-wide hash change baked into that tag's current contents.

**Possible fix:** pin the Docker image by digest (`@sha256:...`) instead of by tag in both `README.md`
and CI, so a `docker pull` of the same reference is guaranteed byte-identical over time. Would need
a companion process for deliberately bumping the pinned digest (and regenerating all baselines in the
same PR) when the team wants a newer Playwright/.NET SDK on purpose.
