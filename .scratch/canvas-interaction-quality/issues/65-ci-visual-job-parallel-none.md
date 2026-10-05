# 65 — CI runs the visual job with `-parallel none`

**What to build:** The CI visual-test job runs the suite the same way the README and `docs/agents/testing.md` already require locally: inside the pinned Playwright image, invoking the built test executable directly with `-parallel none`. A green visual job then means the baselines match, and a red one means a real diff rather than a parallelism flake that looks like one (large pixel or HTML diffs, a locator timing out at zero elements, a click intercepted by an overlapping element).

Nothing else in this feature should gate on CI until this lands, because the interaction probes and every baseline change below run in that job.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] The visual job invokes the built visual-test executable with `-parallel none` rather than relying on `dotnet test` forwarding runner flags
- [ ] The unit-test job is unchanged
- [ ] A failing visual test still uploads the `.received.*` files as an artifact
- [ ] The job passes on `main` as it stands today, so the fix is verified against the current fifty baselines
