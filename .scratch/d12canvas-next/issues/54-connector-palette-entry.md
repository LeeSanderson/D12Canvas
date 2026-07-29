# 54 — "Connector" palette entry

**What to build:** An end user who prefers starting from the palette finds a built-in "Connector" entry there — present without being in the component-type registry, since an edge is not a component type. Activating it drops a floating edge (both endpoints floating) onto the board, ready for its ends to be dragged onto ports. (ADR 0009.)

**Blocked by:** 26 (Palette lists registered types), 49 (Floating endpoints)

**Status:** resolved

- [x] A "Connector" entry appears in the palette without any registry registration
- [x] Activating it creates an edge with both endpoints floating, placed in view
- [x] Both floating ends can then be attached to ports
- [x] Dropping the connector is an undoable gesture
- [x] Screenshot case for the palette entry and the dropped floating edge

## Comments

`DiagramCanvas` gains a public `const string ConnectorPaletteKey = "__d12-connector__"` — a
sentinel, not a registry key, since `Edge` is its own entity (ADR 0003), not something ADR 0001's
`RegisterComponent` covers. It flows through the *exact same* `BeginPaletteDrag`/`ClickToAdd`
gesture plumbing tickets 27/28 already built for every other palette entry; `PlaceComponent`
(shared by `HandleDrop` and `ClickToAdd`) branches on the sentinel and routes to a new
`PlaceConnector(centerX, centerY)` instead of resolving it through `Registry.Resolve` — so the
sentinel never reaches the registry at all. `PlaceConnector` creates a new `Edge` with both
endpoints as `FloatingEndpoint`s, offset `±40px` horizontally from the drop/click-to-add center
point (a fixed-length segment rather than a single point, so the dropped edge is immediately
visible and grabbable, not a zero-length line) — routed through the existing `AddEdgeCommand`
(ADR 0007), identical to how `CompletePortDrag` (ticket 48) already creates edges, so undo/redo
needs no new command. Every other mechanic — rendering, pan/zoom tracking, and dragging a floating
end onto a port to attach it — is ticket 49's existing, untouched machinery; a connector-dropped
edge's endpoints are ordinary `FloatingEndpoint`s indistinguishable from ones a port-drag created.

**Palette.razor**: the Connector button is rendered *after* the registered-types `@foreach`
loop, as a direct child of `.d12-palette` rather than nested inside the
`.d12-palette-category`/`.d12-palette-entry` grouping markup — it's a "built-in, fixed entry" per
ADR 0009, not a registered type, so it always renders regardless of what's registered (including
nothing at all) without perturbing the category count or entry ordering those registered types'
own tests rely on. It's wired through the *same* existing `HandleDragStart`/`HandleClick` methods
every other entry uses, just called directly with `DiagramCanvas.ConnectorPaletteKey` instead of a
loop-bound `entry.Key` — no new Palette.razor.cs code needed at all.

**Existing PaletteTests.cs fallout**: an always-present extra button broke every test that found
"the" `.d12-palette-entry-button` by a bare singular `Find`/count — updated each to scope past the
Connector entry (by `aria-label`) rather than assume there's only one button on the page; category-
count tests (`GroupsEntriesUnderTheirRegisteredCategoryHeading`,
`RendersNoCategoriesWhenNothingIsRegistered`) needed no changes at all, confirming the Connector
entry's placement outside the category markup does what it's meant to.

**Visual tests**: new `ConnectorPaletteEntryVisualTests` (reusing `/placement-demo`, ticket 27's
existing combined Palette+DiagramCanvas page) drags the Connector entry onto the board and asserts
one `.edge-line` plus two `.floating-endpoint` markers before screenshotting — the drop point
(`TargetPosition`) is deliberately centered rather than near the container's edge, since the nav
sidebar + palette panel together leave `.diagram-canvas`'s own viewport narrower than the full
page, and a drop too close to that edge would push one of the ±40px endpoints off-frame.

The Connector button appearing in the palette changes the full-page screenshot of every visual
test that reuses `/placement-demo` or `/palette-demo` (11 pre-existing test methods across
`PaletteVisualTests`, `ClickToAddPlacementVisualTests`, `DragAndDropPlacementVisualTests`,
`DragMoveVisualTests`, `ResizeVisualTests`, `MarqueeVisualTests`,
`MultiSelectionMoveResizeVisualTests`, `InlineTextEditingVisualTests`, `PortsVisualTests`,
`SelectionVisualTests`) — the same kind of shared-page ripple ticket 27/28 hit when a new nav link
changed `BoardRenderingVisualTests`' baselines. Regenerating these on this machine collided with
the pre-existing, already-filed environment issue (ticket 81: this dev machine's local Docker runs
produce different `MainLayout`/`NavMenu` scoped-CSS hashes than whatever produced the currently
committed baselines, unrelated to any code change, confirmed there via `git stash` to a clean
historical commit). Rather than blindly promote `.received.*` wholesale (which would have silently
baked this machine's own drifted hashes into the committed baselines, risking a *new*, unrelated
CI mismatch), each affected `.verified.html` was patched surgically: the one substring that
actually changed (`</ul></div></div><!--!-->` → `...</ul></div><button ...>Connector</button>
</div><!--!-->`) was replaced in place, leaving every pre-existing hash token untouched. Diffing
the patched files against a fresh `.received.html` with hash tokens normalized confirmed zero
remaining differences, i.e. the patch captures exactly and only this ticket's real content change.
PNGs were promoted wholesale (unlike HTML, scoped-css hash tokens are invisible plumbing that
doesn't affect rendered pixels, so the received screenshots are accurate regardless of the hash
drift). Re-running the full suite after this confirmed all 17 affected tests plus the one new test
pass; the only remaining failures are 4 pre-existing, unrelated `/board-demo`/`/edge-styles-demo`
tests (`FloatingEndpointVisualTests`, `PortDragVisualTests`, `EdgeSelectionVisualTests`,
`EdgeRoutingAndArrowheadsVisualTests`) — pages this ticket's code never touches — left untouched as
ticket 81's own existing, out-of-scope drift.

Test coverage: `D12Canvas.Tests/DiagramCanvasConnectorPaletteTests.cs` (new) — drop and click-to-add
both producing a floating-to-floating edge centered on the right point (including the shared
cascade counter applying across component *and* connector placements together), a drop-with-no-
preceding-drag no-op, undo/redo, and dragging one of a connector-dropped edge's floating ends onto
a port end-to-end. `D12Canvas.Tests/PaletteTests.cs` — the Connector entry's unconditional presence,
and its own drag/click wiring round-trips.

Full `D12Canvas.Tests` suite (415 tests, 1 pre-existing skip) and `D12Canvas.VisualTests` (41 tests)
run in the pinned Playwright Docker image, sequentially per the documented parallel-flakiness
workaround; `dotnet csharpier --check .` clean.
