# 78 — Visual-test baselines drift because of local/container .NET SDK skew and un-cleaned scoped-CSS bundles, not a moving Docker tag

**What's wrong:** Every HTML-based Playwright baseline in `D12Canvas.VisualTests` can fail at once on a
scoped-CSS class-name mismatch (e.g. `b-52l6hnzelf` vs `b-k7rv4lobv5` for the exact same
`MainLayout`/`NavMenu` markup) with no source change involved. This ticket originally blamed Microsoft
silently repushing new content under the pinned `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` tag
between ticket 40 and ticket 41. `/prove-it-to-me` on that claim found it doesn't hold up, and surfaced
the real mechanism: **`global.json` doesn't pin a .NET SDK version, so the local host and the pinned
container build with different SDK feature bands**, and **`obj/`'s scoped-CSS bundle can go stale across
incremental rebuilds** (ticket 39's bug, recurring) — either of which independently reproduces this exact
symptom, no registry involved.

**Status:** ready-for-agent

**Why the original "Docker tag mutated" theory is wrong:** re-pulling
`mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` fresh and inspecting it shows `Created:
2026-06-23T21:29:56Z` and bundled SDK `10.0.301` - both over a month before ticket 40 (2026-07-27) or
ticket 41 (2026-07-27) ran, and identical to what ticket 30 measured on 2026-07-25. A tag that had been
silently repushed during the ticket-40-to-41 window would show a newer `Created` timestamp and/or a
different SDK version on a fresh pull; it shows neither. `git log -p` on `.github/workflows/ci.yml` also
shows this tag string was added once and never edited. The image's content has not changed at all during
the window this ticket blamed.

The symptom itself also predates ticket 40 by days: ticket 25 saw "scoped-CSS hash identifiers differing
between builds" on a plain rebuild, no Docker involved; ticket 29 (2026-07-25, two days before ticket 40)
already logged this as "a pre-existing environment/baseline-drift issue"; and ticket 39 diagnosed the
exact `b-k7rv4lobv5`/`b-52l6hnzelf` pair directly as a stale scoped-CSS bundle left behind by incremental
rebuilds, fixed (that one time) with `dotnet clean`. A registry silently drifting forward would produce a
new hash each time it happened, not the same two values recurring across five tickets over several days.

**What's actually going on:** two independently-confirmed, mundane causes, not registry tampering.

1. **SDK skew.** `global.json` has no `"sdk"` block. The local host currently runs SDK `10.0.101`; the
   pinned container currently runs `10.0.301` (confirmed twice, three days apart - ticket 30 and this
   investigation). Whichever SDK generates a build determines Razor's scoped-CSS hash, so a baseline
   captured under one and checked under the other mismatches deterministically - not randomly, which is
   why it's always the same two hash values rather than an ever-changing one.
2. **Stale scoped-CSS bundles.** Right now, on disk,
   `D12Canvas.Demo/obj/Debug/net9.0/scopedcss/bundle/D12Canvas.Demo.styles.css` is gitignored (never
   cleaned, never committed) and contains *two different* hashes in the same file, one of them
   `b-k7rv4lobv5` - this ticket's own "old" hash. The documented workflow's
   `docker run -v "$PWD:/workspace" ...` bind-mounts this directory straight into the container, so a
   "from-scratch container run" doesn't actually get a clean build unless `obj`/`bin` are removed first.
   The earlier investigation's "ruled out stale local build artifacts via two from-scratch container runs"
   didn't actually rule this out, because the bind mount inherits host-side staleness regardless of how
   fresh the container itself is.

**How it was found:** Regenerating Playwright visual-test baselines for ticket 41 (Text built-in); see
original write-up below for that half. The Docker-tag-mutation theory was then checked with
`/prove-it-to-me`: cross-referencing tickets 25/29/30/39/40/45 for chronology and recurring hash values,
then a live `docker pull` + `docker inspect` + `docker run ... dotnet --version` against the exact pinned
tag, plus a filesystem check of `D12Canvas.Demo/obj/` for the stale-bundle artifact described above.

**Original how-it-was-found (ticket 41, unchanged):** All 16 HTML/PNG baseline pairs failed on the first
regeneration run, not just the 9 files whose `.d12-palette-entry`/`.component-container` count assertions
ticket 41 intentionally changed. Diffing showed every failure carried the identical scoped-CSS hash
substitution regardless of which test it was. Confirmed as environment drift rather than a ticket-41
regression by checking out ticket 40's unmodified commit into a scratch worktree and running the exact
same pinned tag against it: the unmodified code also fails its own already-committed baseline, with the
identical hash values - consistent with SDK skew (the worktree's fresh build uses the container's SDK,
which differs from whatever SDK generated ticket 40's committed baseline) rather than with a mutated tag.

**Why not fixed as part of ticket 41:** Out of scope - ticket 41 only adds the Text built-in. The actual
palette/component-container changes were confirmed correct (via hash-normalized diffing) and accepted
alongside the incidental hash churn.

**Fix:**

1. Pin the .NET SDK in `global.json` (`"sdk": {"version": "10.0.301", "rollForward": "disable"}`, matching
   the pinned container) so local and container builds stop disagreeing on the scoped-CSS hash.
2. Add `dotnet clean` (or delete `obj`/`bin`) as a mandatory, scripted first step of the baseline
   generation/verification workflow in `README.md`, since the container run bind-mounts `$PWD` and
   inherits host-side staleness otherwise.
3. Once (1) and (2) are in place, regenerate every baseline in one deliberate, clean session and commit
   them together as a single reset, rather than accepting piecemeal hash churn per ticket.
4. Optionally, also pin the Docker image by digest (`@sha256:...`) as defense-in-depth against a genuine
   future MCR rebuild of the same tag (this does happen for base-OS security patching) - worth doing, but
   secondary: it would not have prevented this issue, since the tag's content never actually changed.

## Comments

- Investigated via `/prove-it-to-me`: the original "Microsoft mutated the pinned tag" theory is not
  supported by the evidence and is very likely wrong. Rewritten above with the SDK-skew /
  stale-bundle root cause and revised fix list. See conversation for full receipts (image
  `Created` timestamp, `dotnet --version` inside the container, the live stale `obj/` bundle
  contents, and the ticket 25/29/30/39/45 cross-references).
