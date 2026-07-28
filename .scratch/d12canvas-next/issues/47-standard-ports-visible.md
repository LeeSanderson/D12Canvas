# 47 — Standard ports visible

**What to build:** Every component instance automatically exposes four standard ports at its border centres (top/right/bottom/left) — positioned as fractions of the instance's bounds so they stay correct through move and resize, with no change to the registration contract. The end user sees them as attachment affordances on hover/selection, hidden otherwise. (ADR 0005.)

**Blocked by:** 22 (Canvas renders a Board)

**Status:** resolved

- [x] All four standard ports exist on every instance automatically — component authors do nothing
- [x] Ports are fractionally positioned and stay at border centres through move and resize
- [x] Port affordances appear on hover/selection and are hidden otherwise
- [x] Screenshot case for visible ports
- [x] bUnit coverage of port presence and positioning

## Comments

`ComponentContainer.razor` renders four always-present `<div class="port port-{top,right,bottom,left}">`
elements per instance, unconditionally — no registration/model change, matching ADR 0005's "no
opt-in needed" framing. Positioning is plain CSS percentages of the container's own box (`top:
-10px; left: calc(50% - 10px);` etc., mirroring the existing resize-handle idiom), not a value
computed from `Bounds` in C#: since `ContainerStyle` already sets the container's `width`/`height`
directly from `Bounds`, a CSS percentage of that box *is* a fraction of `Bounds`, for free, with
zero recompute and no drift possible on move/resize. Visibility (hover/selection, hidden
otherwise) is CSS-only (`.component-container:hover .port, .component-container.selected .port {
opacity: 1; }`) — no new C# state, no extra render.

New classes are `.port-top`/`.port-right`/`.port-bottom`/`.port-left`, not the bare `.top`/`.right`/
etc. the existing resize-handle CSS already uses — reusing those would have collided (same class
name, later-declared rule wins) and mis-sized/mis-positioned the ports. Ports are sized larger
(20px) than the coincident resize handle (10px) and rendered before it in markup (so the handle
paints on top): since `box-sizing: border-box` is applied app-wide (Bootstrap's reboot), a port
exactly the resize handle's own size would have its entire fill hidden behind the handle, showing
only a white rim — sizing it up so the green fill forms a visible ring around the handle was
discovered and fixed by inspecting the actual rendered screenshots pixel-by-pixel, not by reasoning
about the CSS alone.

Ports have no interactivity (`pointer-events: none`, no mousedown handlers) — dragging from a port
is ticket 48's concern, which is blocked by this one. `IsMultiSelected` (which already suppresses
an instance's own resize handles during a multi-selection, ticket 33) does *not* suppress its
ports — the ticket text only says "hover/selection", and a multi-selected instance is still
individually selected, so its ports staying visible is a defensible reading, left as-is.

Test coverage:
- `D12Canvas.Tests/ComponentContainerTests.cs` — two bUnit tests: port presence + directional-class
  check on a fresh render, and the same check after re-rendering the *same* instance at a different
  Width/Height (exercises the actual update path, including `ComponentContainer`'s own
  `ShouldRender` override, rather than just two independent fresh renders).
- `D12Canvas.VisualTests/PortsVisualTests.cs` — `PortsVisibleOnHover_MatchesBaseline` (screenshot:
  hovering alone, unselected, reveals ports) and `PortsSitAtEachInstancesOwnBorderCenters` (no
  baseline — a geometric assertion placing a Rectangle (160x100) and an Image (240x180, a
  different `DefaultSize`) and checking each instance's four ports against its *own*
  browser-measured bounding box, proving the fractional positioning holds for more than one
  arbitrary size).
- Regenerated ~15 other visual-test baselines across the suite: the new port markup appears in
  every rendered instance regardless of visibility, so any test capturing a `.component-container`
  changed its HTML snapshot; several also changed their PNG where the test's own interaction left
  the cursor hovering a shape (revealing ports) or an instance was selected (the port halo around
  the coincident resize handle). Inspected representative before/after crops of each category
  before promoting — all changes are exactly this ticket's intended new affordance, nothing else
  moved.

An earlier version of the second Playwright test tried to prove the "stays through resize" half of
the acceptance criteria with a live handle-drag (mirroring `ResizeVisualTests.ResizeInProgress`'s
own mousedown/mousemove/mouseup sequence) and a before/after position-delta assertion. That
uncovered two things, neither of which is this ticket's to fix: dragging past the *original*
(pre-resize) edge intermittently lands the mouse over the instance's own text content instead of
canvas background, and — more notably — inspecting `ResizeVisualTests.ResizeInProgress`'s own
long-standing, already-passing baseline HTML shows its captured instance never actually grew past
its default 200x150 size either. That test's baseline has apparently encoded a resize that doesn't
visibly apply in this headless/Docker rig since ticket 31, and the screenshot-diff has been quietly
passing against itself either way. Given that's a pre-existing rig characteristic affecting resize
tests generally (not something this diff introduced), building a new test on the same shaky
mechanism would only add another test with the same silent gap. The two-differently-sized-instance
check above proves the same underlying fractional-positioning claim deterministically instead,
without depending on a live drag simulation at all.

`/code-review` (Standards + Spec sub-agents, run before commit) findings and how they were
addressed:
- **Standards**: confirmed the `.port-top` vs. bare `.top` collision concern above is real (not
  paranoia) and correctly avoided. Flagged the first draft's three bUnit tests as functionally
  duplicate (all reducible to the same `.port` count assertion, one with a misleading name
  claiming to check "positions" it couldn't actually check) — trimmed to the two described above.
- **Spec**: flagged that no test exercised "stays correct through resize" with real measurements,
  only code-comment reasoning — addressed by the geometric two-instance test and the resize-rig
  finding above. Also flagged the lack of a queryable port model (`PortId`, `FractionX/Y`) for
  ticket 48 to build on; per ADR 0005 the four standard ports are automatic/derived rather than
  declared, and ticket 47's own checklist doesn't ask for one, so this is left for ticket 48 to
  introduce whatever it actually needs rather than speculatively adding one now.

Full `D12Canvas.Tests` suite (284 tests) and the full `D12Canvas.VisualTests` suite (21 tests, run
in the pinned Playwright Docker image per README) pass.
