# 65 — CI runs the visual job with `-parallel none`

**What to build:** The CI visual-test job runs the suite the same way the README and `docs/agents/testing.md` already require locally: inside the pinned Playwright image, invoking the built test executable directly with `-parallel none`. A green visual job then means the baselines match, and a red one means a real diff rather than a parallelism flake that looks like one (large pixel or HTML diffs, a locator timing out at zero elements, a click intercepted by an overlapping element).

Nothing else in this feature should gate on CI until this lands, because the interaction probes and every baseline change below run in that job.

**Blocked by:** None — can start immediately

**Status:** resolved

- [x] The visual job invokes the built visual-test executable with `-parallel none` rather than relying on `dotnet test` forwarding runner flags
- [x] The unit-test job is unchanged
- [x] A failing visual test still uploads the `.received.*` files as an artifact
- [ ] The job passes on `main` as it stands today, so the fix is verified against the current fifty baselines

## Comments

The visual job now has two steps where it had one: `dotnet build` of the visual-test project, then the built executable invoked directly with `-parallel none`, which is the exact command the README and `docs/agents/testing.md` give for a local run. The unit-test job and the artifact upload are untouched.

The last box is left open deliberately. The job runs on GitHub Actions and there is no way to run it from this machine. The command it runs is the same one every local baseline run has used, so the first push to `main` after this commit is the check.
