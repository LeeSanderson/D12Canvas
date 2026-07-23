# 23 — Playwright visual-test rig in CI

**What to build:** A small, curated screenshot-diff layer runs against the real Demo app — locally and in CI via the official Playwright Docker image. First cases cover the rendered board and zoom/pan states. This enables the standing rule from the layered testing strategy: any later ticket that renders a new visual state on canvas must add a screenshot case.

**Blocked by:** 22 (Canvas renders a Board)

**Status:** ready-for-agent

- [ ] A Playwright for .NET suite runs against the Demo app locally and in CI
- [ ] Baseline screenshots exist for a rendered board and at least one zoomed/panned state
- [ ] A screenshot diff failure fails the build; the baseline-update workflow is documented
- [ ] The standing rule (new visual state → new screenshot case) is recorded where contributors will see it
