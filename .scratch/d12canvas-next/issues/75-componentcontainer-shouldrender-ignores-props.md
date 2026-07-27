# 75 — `ComponentContainer.ShouldRender()` ignores Props changes

**What's wrong:** `ComponentContainer.ShouldRender()` (`D12Canvas/ComponentContainer.razor.cs`) only compares `X`/`Y`/`Width`/`Height`/`_editMode`/`IsSelected`/`IsMultiSelected`. If a host swaps `DiagramCanvas.Board` for a new `Board` whose instances keep the same `Id` (so `DiagramCanvas`'s `@key="instance.Id"` reuses the same `ComponentContainer`) and the same `Bounds`, but different `Props`, the container's `ShouldRender()` returns `false` and the new Props never reach the nested `<DynamicComponent>` - the old content stays on screen even though `Board`/`ComponentInstance.Props` is correct in memory.

**Status:** resolved

**How it was found:** Building ticket 35's save/load demo page. Save → edit the JSON's `Text` only (same `Id`, same `Bounds`) → Load reproduced this exactly: the underlying `Board` held the new `Props`, but the rendered note kept showing the old text. Editing a `Bounds` coordinate instead (which *is* compared) made the container refresh correctly and pull the new Props along with it - confirming the geometry check, not the Props swap itself, is what's silently skipping the render.

**Why not fixed as part of ticket 35:** Out of scope for JSON round-trip. A correct fix needs to distinguish "the Board reference changed and this instance's Props changed" from "the parent just re-rendered for an unrelated reason" without regressing the render-skip optimization tickets 01/02/25 measured real perf gains from (naively comparing `ChildContent` by reference would always differ, since `DiagramCanvas.razor` constructs a fresh `RenderFragment` every render regardless of whether Props changed). Likely needs either a Props-equality check (structural, so records compare cheaply) or an explicit per-instance revision counter threaded from `Board`/`ComponentInstance` down through `DiagramCanvas` to `ComponentContainer`. Also directly relevant to the future property-panel editing ticket (56+), where editing an existing instance's Props in place at unchanged Bounds would hit the exact same gap.

**Workaround used in ticket 35's demo page:** none needed once the verification script edited a Bounds coordinate alongside the Props - not a real fix, just avoids exercising the gap.

## Comments

Fixed as a necessary side effect of ticket 43 (inline WYSIWYG text editing), which hit this exact
gap immediately on its most basic flow: edit a Sticky Note's text, blur, `Board`/`ComponentInstance.Props`
updates correctly but the screen keeps showing the old text at unchanged `Bounds`.

Took the first suggested approach above - a structural Props-equality check, not a revision counter.
`ComponentContainer` gained a `Props` parameter (`object?`) used *only* for change detection in
`ShouldRender()`, never rendered directly (`ChildContent` stays opaque). Compared via the plain
`Equals(Props, _lastRenderedProps)` static call: Props types are C# records, so this is already a
cheap, correct structural comparison with no new type needed. `DiagramCanvas.razor` passes
`Props="instance.Props"` on `<ComponentContainer>` alongside its existing `X`/`Y`/`Width`/`Height`
bindings. Existing usages that don't set `Props` (e.g. `ComponentContainerDemo.razor`) are unaffected
- it defaults to `null` on both sides of the comparison.

Regression test: `DiagramCanvasInlineTextEditingTests.BlurAfterEditingUpdatesTheRenderedText` /
`UndoAfterEditingRevertsTheRenderedText` (`D12Canvas.Tests`) assert on the *rendered* `<p>` text, not
just `instance.Props`, so this can't silently regress again unnoticed.
