# 79 — Mid-resize screenshot baselines are pixel-nondeterministic even though the HTML snapshot is stable

**What's wrong:** `ResizeVisualTests.ResizeInProgress_MatchesBaseline` and
`MultiSelectionMoveResizeVisualTests.GroupResizeInProgress_MatchesBaseline` both take a full-page PNG
screenshot while the mouse button is still down mid-resize (matching `DragMoveVisualTests`' approach for
ticket 30). Their HTML snapshot is byte-identical on every run - the resize itself is fully deterministic
- but the PNG differs from *any* baseline, including one captured moments earlier from the exact same
code path. Re-running against a freshly-promoted baseline still fails on the next run.

**Status:** needs-triage

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
