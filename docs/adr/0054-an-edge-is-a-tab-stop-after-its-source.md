# An edge is a tab stop, placed right after its source

ADR 0010 makes the board operable without a mouse by giving every entity a tab stop in reading order. `OrderedTabStops` enumerates visible instances and visible groups and nothing else, and ADR 0026, which settled the tab-stop model's other questions, says nothing about edges. ADR 0037 put edges in the `Selection` and left this silence standing as its own question. This decides it: **every edge enters the tab ring, directly after the stop its source resolves to.**

What the silence cost, as of ADR 0037:

- A keyboard user could delete an edge only by `Ctrl+A` then Delete, which deletes the board.
- ADR 0021's four edge roles reach the property bar through `Ctrl+Enter`, which acts on the selection, and a keyboard user could not put a single edge in it.
- A mixed selection could be built only by pointer or by select-all.

## An edge is content, so reading order governs it

ADR 0026 has a rule for whether a surface enters keyboard navigation, but that rule is written for chrome: a surface enters only if it is the only route, canvas-rendered chrome takes a chord, host-placed chrome takes its tab order. An edge is not chrome. It lives in `Board.Edges`, persists through ADR 0004, and is selected, moved and deleted like an instance. ADR 0002's split puts it on the board-content side, so ADR 0010's reading order applies and ADR 0026's chrome rule does not.

## In the ring, rather than a drill or a separate ring

Three routes were weighed.

**In the ring** reaches every edge and spends no binding. Its cost is ring length. ADR 0030's `Quick create` produces boards with about one edge per instance, so the ring roughly doubles on exactly the boards where it is already long. That cost belongs to how much is on the board, not to edges in particular, and anything that makes a long ring cheaper to move through will help every stop.

**Drilling in from an endpoint**, the way `Enter` steps into a group under ADR 0044, keeps the ring short. It cannot reach an edge with both ends floating, which is what ADR 0009's palette connector creates. That is a mouse-only entity, which is the gap ADR 0010 exists to close, so the drill would need a second route beside it anyway. It also needs a key the table does not have free.

**A separate edge ring on a chord** needs a binding from a table ADR 0026 found crowded, and every candidate would first need the probe carried by [Chord survival on macOS and in Firefox](../../.scratch/canvas-interaction-quality/issues/56-chord-survival-macos-firefox.md). [Whether Ctrl+Tab and Ctrl+Arrow survive the browser](../../.scratch/canvas-interaction-quality/issues/41-ctrl-tab-browser-reservation.md) has already shown that a chord chosen without measuring can be dead on arrival.

## The order key: the source's stop, then the target point

ADR 0037 declined tab stops partly because an edge spanning the board "sits nowhere meaningful in reading order". That is true of any position computed from the edge's own geometry: the source point, the midpoint and the union box all put a long edge somewhere unrelated to either thing it connects. It is not true of a position borrowed from an endpoint. **An edge's stop comes directly after the stop its source resolves to**, so a chain reads A, A→B, B, B→C, whichever way the edge runs across the board.

- **The source resolves the way a press does.** An instance resolves to its own stop. A grouped member has no stop of its own, so it resolves to its outermost group's stop, which is `EffectiveSelectionId`'s resolution with no group entered.
- **Several edges after one anchor sort by target point**, `Y` then `X`, the ring's own reading order applied to where each edge goes.
- **An edge with a floating source has no anchor**, so it sorts as a stop in its own right at its source point, `Y` then `X`, among the other stops.

## Which edges have a stop

**An edge has a stop exactly when its anchor is present.** For an attached source that means the source's stop is in `OrderedTabStops`. For a floating source it means the source point is inside the viewport plus overscan, which is the windowing rule instances already follow. An edge coming in from an off-screen source is not in the ring until the user pans to its source, which is the same limit an off-screen instance has. "Either end visible" was rejected because it needs the target as a second anchor for exactly the case where the source is off screen.

**An edge whose line cannot be resolved has no stop**, the rule its line and its label already follow. That covers ADR 0032's dangling endpoint, attached to a component that has been deleted.

**LOD does not remove an edge's stop.** Edges are never placeholdered, and a pointer can press one at a zoom where its shapes have become `LOD placeholder`s, so dropping the edge from the ring would make the keyboard weaker than the pointer at that zoom. The anchor is unaffected either way. The placeholder keeps its entry in `OrderedTabStops`, and `CONTEXT.md` records it as an ordinary tab stop under ADR 0017. Today's `FocusableTabStopIds` still skips a placeholdered instance because the placeholder renders without a `tabindex`. That is code that has not caught up with ADR 0017, not a rule this decision has to work around.

## Inside an entered group, edges have no stop

ADR 0044 says that while a group is entered, the selection holds only its direct members, and that focus landing outside the `Entered group` pops the scope until the target is a descendant. An edge is never a member of a group, so it is never a descendant. An edge stop inside the scope would therefore throw the user out of the group on the first `Tab` that reached it, and would unmount and move the stops around the element that had just taken focus.

**While a group is entered, the ring holds the scope's members and no edges.** Every edge stays reachable from the top level, anchored after the group's stop. ADR 0044 is unchanged. An entered group already shows a narrower ring than the top level, and an edge is not a member.

Admitting an edge with both ends on members of the entered group was rejected here. It would be better for editing a connected cluster inside a group, but it changes what an entered group's selection may hold, which needs a pointer answer ADR 0044 does not give. The reason to reopen that would come from the pointer, not from this decision.

## Landing on an edge's stop

An edge stop behaves exactly like an instance stop.

- **`FocusEntity` resolves the kind with `Board.GetEdge`.** An edge is hard-selected alone, both sets cleared: the keyboard form of a plain click on the line. Instance and group ids keep their current path. `TabStop` gains an `Edge` field beside `Instance` and `Group`, so `OrderedTabStops` stays the one list that the markup, `FocusableTabStopIds` and `focusTabStopAt` all read.
- **`Space` toggles the focused edge** in `_selectedEdgeIds`, through the same toggle a shift-press on an edge uses. That gives the keyboard a route to a mixed selection.
- **`Enter` does nothing on an edge stop.** ADR 0026 scopes it to instance stops, and ADR 0044 to group stops.
- **`FocusableTabStopIds` lists edge stops at their DOM positions.** `focusTabStopAt` counts `[tabindex="0"]` in document order, so the two lists must agree index for index.

**This assumes nothing about how [Keyboard multi-select without Ctrl+Tab](../../.scratch/canvas-interaction-quality/issues/55-keyboard-multi-select-without-ctrl-tab.md) settles.** An edge stop is an ordinary member of the ring, so whatever that ticket decides for stops applies to edges too. One thing is handed to it: if it unwelds focus from selection, focus without selection needs a visible indicator, and ADR 0036 records that this library draws none. That applies to every stop, not only edges.

## The stop element

The edge's `<line>` or `<path>` cannot be the stop. It renders inside `<svg class="edges-layer">`, which comes before every tab-stop div in the DOM, so `tabindex="0"` on it would put every edge before every entity in native `Tab` order. Each edge instead gets a proxy element, the same way `.group-tab-stop` stands in for a group:

- **An invisible `<div class="edge-tab-stop">`** with `pointer-events: none` and `tabindex="0"`, rendered inside the `OrderedTabStops` loop at the edge's position in the ring, and sized to the union box of its two resolved endpoints. The box only tells the browser where the element is. The visible indicator is the selected edge's halo (ADR 0041), as the selection box is for a group stop.
- **`aria-label` reads "Connector from {source} to {target}".** Each end is its instance's registered `AccessibleName`, or the group's label when the end resolves to a group, and a floating end reads "unattached end". "Connector" is the word the palette's own `aria-label` already shows the user. `CONTEXT.md` avoids it as a domain term, and that rule is about how the code and the docs name things, not about what a screen reader says.
- **`aria-selected`** when the edge is selected, as on the group stop.
- **No `role`.** ARIA has no role for a connector, and `group`, which the group stop carries, would describe it wrongly.

## The edge label's focus return

ADR 0051 sent focus to the canvas container when `Escape` ends an edge label's edit, because the label had nothing to return to. **It now returns to the edge's stop.** This is ADR 0036's third occasion, a command handoff that names its target, and it matches ADR 0051's own rule for an instance, whose edit returns focus to the instance's stop. The `@onfocus` hard-selects the edge. That is correct here: a label edit begins from the edge, so the user ends where they started, and the next `Tab` continues from the edge. If the edge's stop is not mounted when the edit ends, because the viewport was panned during the edit, focus falls back to the canvas container as before.

## How this is verified

Per ADR 0025:

- `OrderedTabStops` over a chain A→B→C yields A, A→B, B, B→C, C, whatever the screen positions of the edges.
- Two edges leaving one instance follow it in their targets' reading order.
- An edge whose source is a grouped member follows the group's stop. With that group entered, no edge stop renders.
- A floating-source edge sorts at its source point. An edge whose source is outside the viewport plus overscan has no stop.
- An edge whose source is placeholdered keeps its stop.
- `FocusableTabStopIds` and the rendered `[tabindex="0"]` elements agree in count and order on a board mixing instances, groups and edges.
- Focusing an edge stop selects that edge alone. `Space` on it toggles it into an instance selection.
- `Escape` out of an edge label's edit leaves focus on that edge's stop.
- An `Interaction probe` presses `Tab` in a real browser and asserts that focus reaches an edge stop and selects the edge. A bUnit test that invokes the `@onfocus` handler cannot stand in for it, because it supplies the focus move the browser was supposed to make.

Every `.verified.html` baseline whose board has an edge moves, since each gains an `.edge-tab-stop`. The `.png` baselines should not, since the element paints nothing.

## Amends, confirms

- **Amends ADR 0026.** The tab-stop enumeration gains edges, and its addendum's "silent on whether an edge is a tab stop" is answered here. The shortcut table gains no row.
- **Amends ADR 0051** in one place: an edge label's `Escape` returns focus to the edge's stop, with the canvas container only as the fallback.
- **Extends ADR 0010.** Reading order covers edges through their sources. Its weld of focus to selection is untouched.
- **Answers what ADR 0037 handed on.** An edge has a tab stop. ADR 0037's reason for declining, that a board-spanning edge has no meaningful reading-order position, holds for a position taken from the edge's own geometry and not for one borrowed from its source.
- **Confirms ADR 0044.** Edges stay outside the `Entered group`, and its focus rules are unchanged.
- **Confirms ADR 0017.** Pointer participation and keyboard reachability stay separate properties, and an edge now has both.

## Considered and rejected

- **ADR 0026's chrome rule.** An edge is board content, not chrome.
- **Drilling in from an endpoint.** Cannot reach an edge with both ends floating, and needs a key the table does not have.
- **A separate edge ring on a chord.** Needs a binding from a crowded table, unmeasured in two browsers and on one platform.
- **An order key from the edge's own geometry** (source point, midpoint, union box). Puts a long edge somewhere unrelated to either thing it connects.
- **A stop when either end is visible.** Needs the target as a second anchor, which breaks the one-anchor rule.
- **Dropping an edge's stop under LOD.** Leaves the keyboard weaker than the pointer at the zoom where the pointer can still press the edge.
- **`tabindex="0"` on the `<line>` or `<path>`.** The SVG sits before every other stop in the DOM, so every edge would come before every entity.
- **Edge stops inside an entered group.** Focus on one pops the scope, so the first `Tab` that reached it would leave the group.
- **Admitting interior edges to the entered group's selection.** Better for editing inside a group, but it reopens ADR 0044 for a reason that belongs to the pointer.
- **`role="group"` or an invented role on the edge stop.** Describes it wrongly.
