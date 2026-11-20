# The canvas draws by fixed rules, and every container parameter it draws is compared

`DiagramCanvas.razor` makes rendering choices that its markup shows but does not explain. `ComponentContainer` skips renders, and the skip is only correct if it compares the right things. Two pieces of chrome draw from numbers C# holds instead of measuring the browser. A page that wires chrome to a canvas by `@ref` gets that reference one render late.

**Edges, edge labels, group stops, the grid and the windowed instance set each follow one rule below. `ComponentContainer.ShouldRender` compares every parameter the container draws. `ContextMenuPlacement` and the menu's style block describe the same box. A page that hands an `@ref`'d `DiagramCanvas` to chrome renders once more when the reference changes.**

## Edges

One `<marker id="edge-arrow">` serves every edge. ADR 0016 covers its `context-stroke` fill. Its `orient="auto-start-reverse"` lets the same marker point the right way as `marker-start` or `marker-end`.

The `<defs>` block renders only when `Board.Edges.Count > 0`. A board with no edges then renders no marker markup at all, and every visual-test baseline without edges depends on that.

A line that takes no presses draws each edge. A wider transparent stroke over it takes the hits. ADR 0017 sets its width at 20 screen pixels at any zoom.

While a connector drag draws its line, `CanvasCssClass` adds `connecting`, which shows every port and makes it hittable. The drop has to find the port under the pointer, and hover cannot do that while the canvas holds pointer capture.

`RouteOf` caches each route on its inputs. A render that moved nothing the edge depends on, such as a pan, a zoom or a drag elsewhere, routes nothing. `PendingEdgeRoute` is the route a connector drag draws, between the end that stays put and the pointer. A new edge is straight. A carried end of an existing edge routes in that edge's own style, with the pointer as a floating end, and stands in for the edge while the drag lasts.

`PlaceConnector` places a palette connector as a segment, not a point. It runs `ConnectorDefaultHalfLength` (40) either side of the drop point, so the new edge is visible and can be grabbed.

## Edge labels

A label lives on its `Edge` and is not a `Board.Components` entry. The canvas renders it directly through a `DynamicComponent`, not through `VisibleComponents` and `ComponentContainer`, because nobody selects, moves or resizes a label on its own.

`EdgeLabelStyle` derives the label's box from the live route on every render. The canvas never reads the label's own `Bounds.X` and `Bounds.Y`, only its size. While a connector drag carries one of the edge's ends, the label follows the pending route, so it does not freeze mid-drag. The canvas skips the label when the edge has none or its line cannot be resolved.

`AddEdgeLabel` runs on a double-press on the line and never replaces an existing label.

## Group tab stops and the shared overlay

A persisted `Group` has one tab stop, `.group-tab-stop`. It is invisible, and ADR 0017 lists it as `pointer-events: none`. A mouse selects a group by pressing one of its members. The stop exists so `Tab` and `Shift+Tab` reach the group as one stop instead of one per member.

When the group is selected, the shared `.selection-bounding-box` overlay is its visible focus indicator. The canvas draws that overlay when `HasMultiMemberSelection` holds. A group that is focused but not selected, during `Additive traversal`, draws the dashed focus ring instead.

`IsMultiSelected` turns off a container's own resize handles in favour of the shared overlay's. A selected group of two or more members counts as a multi-selection.

## Asset references in props

`BoundProps` gives a component its props with every asset reference already replaced by the asset's `data:` URI. The same object goes to `ComponentContainer`'s `Props` parameter. An asset that arrives after the first render changes what the container compares, so the container re-renders its child. An unchanged props object that is already resolved is the same cached copy on both renders.

`UnresolvedForCommit` reverses this at the commit point. A component edits the props it was bound with, so a declared reference would come back as a `data:` URI unless the canvas puts the reference back.

## Windowing and level of detail

`DefaultOverscan` is 200 pixels, to absorb a fast pan. Moves reach the canvas once per animation frame, so the pointer can travel well past the margin before the next windowed render. A much larger margin adds mount and unmount cost and shows nothing more.

`DefaultLodSizeThreshold` is 32 screen pixels. ADR 0017 states the test: the larger of width and height, times scale, below the threshold. Measuring the larger side keeps a thin, wide shape such as a divider from becoming a placeholder at a normal zoom. No built-in's default size crosses 32 near a zoom of 1.0, so the threshold applies only once zooming out shrinks an instance past legibility.

## The grid

The grid layers sit outside `.canvas-content` so they cover the whole viewport at any pan. They therefore do not get its transform. `GridLayerStyle` uses `PositiveMod` on the pan to keep the pattern's phase locked to board coordinate 0.

## Every parameter that changes alone is compared

Blazor re-renders every child on the parent's `StateHasChanged`, so each pan of `DiagramCanvas` reaches every `ComponentContainer`. ADR 0015 and ADR 0020 record that `ShouldRender` skips those renders. A parameter missing from the comparison does not show until some unrelated parameter also changes, so `ShouldRender` compares every parameter that reaches the markup. Several change while `Bounds` and selection stay the same:

- `Focusable`, which Group or Ungroup flips by itself, since grouping keeps every member selected. Without it the `tabindex` attribute lags.
- `Props`, for an in-place props edit. The container never renders `Props` itself and `ChildContent` stays an opaque `RenderFragment`. `Props` is a parameter only so `ShouldRender` can see the edit. Props types are records, so `Equals` is a cheap structural comparison.
- The custom port count, for a port added, removed or undone.
- `ZIndex`, which a layering command changes and nothing else does. Bounds, selection, props and custom ports stay the same, and the new stacking must render at once.
- `FocusedPortId`, `FocusedCustomPortId`, `AutoPortFocused` and `ProvisionalPort`, which port picking and port placement change as the keyboard moves, arms or confirms.
- `Scale`, while ports show. The border partition is measured in screen pixels, so a zoom can add or drop a span.

The comparison also covers `Addressable`, `IsDropTarget`, `Locked` and `HasHitMarker`. A test runs a layering command alone and checks that the new `z-index` is drawn.

## An unaddressable member renders no tabindex

While an instance is inside a group that is not entered, its container renders no `tabindex` attribute at all, not `tabindex="-1"`. The markup writes `tabindex="@(Focusable ? "0" : null)"`. Such a member is reachable only through its group's tab stop. A `-1` would still accept a direct `.focus()` call, for example from stale JavaScript, and land focus on a member the keyboard should not reach.

## The context menu is placed from its declared size

The canvas places the context menu on the render that opens it, with no measuring round trip to the browser. `ContextMenuPlacement` holds a fixed `Width`, `RowHeight`, `SeparatorHeight` and `Frame` (the root's padding plus border), and `HeightOf` computes the height from the composed rows. The menu's style block must draw exactly that box. A test reads the style block and checks its width, row height, separator and frame against `ContextMenuPlacement`, so changing one without the other fails.

## Chrome wired by @ref gets its canvas one render late

ADR 0002 wires host-placed chrome such as `Palette` and `PropertyPanel` to a `DiagramCanvas` by reference. The host gets that reference through `@ref`, and Blazor assigns `@ref` only after the render that produced the element. On that render the chrome has already received its `Canvas` parameter, and the value it received is null.

When `DiagramCanvas` sits after `Palette` or `PropertyPanel` in the markup, as on `PlacementDemo` and `PropertyPanelDemo`, one `StateHasChanged` in `OnAfterRender(firstRender)` gives the chrome its canvas. `VirtualizationStressTest` does the same on its first render so its rendered-count label reads the canvas at once.

`BoardEditor` in the App is later still. It renders `DiagramCanvas` only after the board loads, so the reference arrives on some render after the page's first. The page checks on every `OnAfterRender` whether `_canvas` differs from `_subscribedCanvas`, which costs one reference comparison. When it differs, the page moves its `Changed` and `SelectionChanged` subscriptions to the new canvas and calls `StateHasChanged` so `Palette` picks it up. `PropertyPanel` subscribes to its own `Canvas` parameter the same way, whenever the reference changes.

## Consequences

Any change to the shared markup or CSS in this ADR moves every Playwright HTML snapshot that mounts a board, so the full visual suite runs before it lands.

A new parameter that changes `ComponentContainer`'s markup joins `ShouldRender`'s comparison, or it renders late.

A change to the context menu's box in CSS changes `ContextMenuPlacement` in the same commit.

Removing the extra `StateHasChanged` on a page that wires chrome by `@ref` looks harmless, and leaves the chrome holding a null canvas until something else re-renders the page. A new page or host that places chrome before its canvas, or renders the canvas late, needs the same step.
