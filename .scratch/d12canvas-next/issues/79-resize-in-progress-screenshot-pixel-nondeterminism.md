# 79 — Mid-resize screenshot baselines are pixel-nondeterministic even though the HTML snapshot is stable

**What's wrong:** `ResizeVisualTests.ResizeInProgress_MatchesBaseline` and
`MultiSelectionMoveResizeVisualTests.GroupResizeInProgress_MatchesBaseline` both take a full-page PNG
screenshot while the mouse button is still down mid-resize (matching `DragMoveVisualTests`' approach for
ticket 30). Their HTML snapshot is byte-identical on every run - the resize itself is fully deterministic
- but the PNG differs from *any* baseline, including one captured moments earlier from the exact same
code path. Re-running against a freshly-promoted baseline still fails on the next run.

**Status:** resolved

**How it was found:** While implementing ticket 78 (SDK-skew fix), regenerating baselines under the newly
SDK-pinned, cleaned environment left 22 of 23 HTML+PNG pairs passing - confirming the SDK-skew root cause
was the only thing wrong with the other 21. These two remained the sole holdouts. Promoted a fresh
baseline for `ResizeInProgress_MatchesBaseline` and reran: failed again, HTML equal, PNG not. Added a
200ms `WaitForTimeoutAsync` after the mouse move (in case it was a render-settle race) and reran both
tests: both still failed against their own just-promoted baselines. Since HTML is stable across every
run, this rules out a state/timing race in the app or test setup - the DOM the screenshot captures is
identical every time. What's left is the raster itself: most likely sub-pixel anti-aliasing/compositing
jitter around the resize handles' circular markers and/or the resized text label's reflow, specific to a
live in-progress resize (not move, not marquee, not drag-and-drop - those all pass reliably with the same
`FullPage`/mouse-down-during-screenshot pattern).

This exact symptom was already flagged, unresolved, by ticket 33 (`GroupResizeInProgress`'s baseline) and
ticket 45 ("a 1-byte, visually-identical PNG re-encoding... the rest are ticket 78's to fix") - both
attributed it to ticket 78 before ticket 78's actual root cause (SDK skew) was known. Now that SDK skew is
fixed and confirmed unrelated to this specific failure, it needs its own ticket.

**Why not fixed as part of ticket 78:** Out of scope - ticket 78 is about SDK-version skew and stale
build artifacts, both fully addressed and verified independently of these two tests. This is a distinct
screenshot-capture nondeterminism bug; forcing a fix in here would risk an unprincipled, unverified
change (e.g. an untested image-fuzzy-diff dependency) just to make the ticket 78 diff look complete.

**Workaround used in ticket 78:** Both tests quarantined with `[Fact(Skip = "...")]` pointing at this
ticket, so CI stays green. Baselines were left at their last-captured (SDK-consistent) state for whoever
picks this up, though the value itself is close to arbitrary given the nondeterminism.

**Possible fix (untried):** a tolerant/fuzzy image comparison (e.g. `Verify.ImageMagick` or an explicit
`maxDiffPixelRatio`-style threshold) rather than Verify's default byte-exact PNG comparison, since a
render-settle wait already demonstrably doesn't help. Alternatively, capture only the resized element's
own bounding box (`Locator.ScreenshotAsync()`) instead of a full-page screenshot, in case cropping out the
rest of the page removes whatever's actually jittering.

**Addendum - the same class of problem is bigger than "mid-resize":** pushing ticket 78's fix to GitHub
Actions surfaced 5 *more* PNG-only failures that all pass reliably in local container runs (same pinned
image, same digest) but fail on the GitHub-hosted runner: `ClickToAddPlacementVisualTests.
ClickToAddedInstance_MatchesBaseline` (not even a drag/resize - a single click, no gesture in flight),
`ClickToAddPlacementVisualTests.ConsecutiveClickToAdds_CascadeWithAnOffset_MatchesBaseline`,
`DragAndDropPlacementVisualTests.DroppedInstance_MatchesBaseline` (post-drop, settled),
`DragMoveVisualTests.DragInProgress_MatchesBaseline`, and `DragMoveVisualTests.
ReleasedInstance_MatchesBaseline` (post-release, settled). HTML matched exactly in every case, same as
the two resize tests above. Since `ClickToAddedInstance_MatchesBaseline` involves no drag/mid-gesture
timing at all, this can't be purely a "screenshot during an active gesture" problem - it's at minimum
partly a genuine **cross-machine PNG rendering difference between two hosts running the identical pinned
image/digest** (my local podman-backed container vs. the GitHub Actions Linux runner), which contradicts
README's "the same image... neutralizes cross-platform font/rendering drift" claim. Worked around for
ticket 78's landing by downloading the actual `.received.png` files GitHub produced (via the workflow's
own `visual-test-diffs` artifact upload) and promoting those as the new baselines, rather than trusting
a local container run as proof of what CI will do. Whether these 5 are now stable long-term (deterministic
per-machine, just different across machines) or will drift again on a future CI run (genuinely
nondeterministic per-capture, like the two resize tests) is unconfirmed - worth checking after a few more
merges to main before considering this closed.

**Resolved via ticket 80.** A second GitHub Actions run made the picture clearer still: 8 tests failed
that time, including 3 (`MarqueeVisualTests.BothInstancesSelected_AfterMarqueeRelease_MatchesBaseline`,
`MultiSelectionMoveResizeVisualTests.BoundingBoxVisible_MatchesBaseline`,
`MultiSelectionMoveResizeVisualTests.PersistedGroupBoundingBoxVisible_MatchesBaseline`) that had *passed*
in the first run with baselines nobody touched in between - proof this isn't "different but stable per
machine," it's genuinely nondeterministic across runs on GitHub's own infrastructure (almost certainly
different underlying host hardware per run affecting sub-pixel rasterization), the same class of problem
as the two resize tests, just wider than "mid-resize."

Ticket 80 ported pixelmatch's OKLab/HyAB comparison to .NET and registered it with Verify as a tolerant
PNG comparer, replacing byte-exact comparison for every screenshot baseline in the suite. Both this
ticket's two Skip-quarantined tests were un-skipped and now pass reliably (verified across repeated
clean-container runs), and calibration against 13 real "should match" noisy pairs pulled from actual CI
runs showed each one measuring an *exact* 0.0% diff under the ported algorithm's own default settings -
strong evidence the fuzzy comparer resolves this whole class of failure, not just the two tests this
ticket started with.
