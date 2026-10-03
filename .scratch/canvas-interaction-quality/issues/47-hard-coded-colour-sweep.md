# Enumerate every hard-coded colour left in the library

Type: task
Status: resolved
Blocked by:

## Question

Produce the complete list of colour literals still sitting in `D12Canvas`'s own CSS and markup, so the token-adoption sweep stops being finished twice and found incomplete twice.

Ticket 74 in `d12canvas-next` resolved as "token adoption across all remaining chrome" and said so in its title. It was wrong twice since:

- [Edge visibility and board-content theming](14-edge-visibility-and-board-content-theming.md) found `.selection-bounding-box`, `.drag-over-affordance` (a *third* hard-coded blue) and `.floating-endpoint` still carrying literals, and noted that the bounding box's byte-identical accent means a host retuning `--d12-accent` desynchronises it from the marquee drawn directly above it.
- [Themed visual defaults for built-in component types](25-built-in-themed-defaults.md) found four more, in the built-ins' own `<style>` blocks: the inline-edit outline in both `Text.razor` and `StickyNote.razor` (`rgba(0, 0, 0, 0.4)`, effectively invisible against the dark theme's backdrop, so the "you are editing this" affordance disappears) and `Image.razor`'s three-literal placeholder.

- [What remains of `ComponentContainer`'s edit mode](26-component-container-edit-mode-remnant.md) found two more in `ComponentContainer.razor`'s own `<style>` block: `#3498db` on `.resize-handle` and `#2f80ed` on `.selected`. That is a **fourth and fifth blue**, and the second one sharpens ticket 14's finding rather than repeating it: `.selected` is the per-instance outline sitting beneath the same `.selection-bounding-box` accent ticket 14 flagged, so a host retuning `--d12-accent` desynchronises *three* selection affordances from each other, not two. A third literal in the same block, `.edit-mode`'s `#3498db`, is deleted outright by ADR 0035 rather than tokenised.

None of the three was looking for them. All were working an unrelated question and tripped over one. That is a pattern rather than a run of accidents, and the next ticket will find a sixth.

This is a task rather than a decision: nothing here needs judgement until the list exists. What each literal *becomes* belongs to whichever ADR owns that element, and ADRs 0016 and 0034 have already assigned seven of them.

The work:

- **Enumerate**, across `D12Canvas`'s `.razor` files, `.css` files and any inline styles emitted from C#: every hex literal, `rgb()`/`rgba()`/`hsl()` call and named CSS colour keyword. Include `transparent` and `currentColor` in the listing but mark them, since neither is a theme decision.
- **Classify each** as already-tokenised, a deliberate escape hatch (`--d12-connector-preview` is the established precedent, and ADR 0034 adds `--d12-inline-edit-outline`), a literal that should read a shared token, or a literal with no theme question at all.
- **Flag every one whose light value would move if swapped**, since the convention across ADRs 0016 and 0034 is that a light value stays byte-identical so no light baseline moves and the diff is confined to the dark cases. A near-miss against a shared token is the interesting case and `Image.razor`'s placeholder is the known example.
- **Note which are unreachable from the current visual suite**, so a swap that moves pixels nobody screenshots is known to be unverified rather than assumed safe.

Deliberately not in scope: `D12Canvas.Demo` and `D12Canvas.App`, which are host code and own their own styling.

## Answer

Swept 2026-10-03 at `5b6bd17`. Method: regex over every `.razor`, `.css`, `.cs` and `.js` file in `D12Canvas/` (excluding `bin`/`obj`) for hex, `rgb()`/`rgba()`, `hsl()`/`hsla()` and named colour keywords, then read each hit in context. `wwwroot/` holds only `background.png`, which nothing references. The JS files contain no colours. Reachability comes from searching each baseline's `.verified.html` for the class in markup (the baselines embed the whole `<style>` block, so a plain search matches everything). Every visual test takes a full-page screenshot.

**The headline: `PropertyPanel` has no token layer at all.** Five literals, no `--d12-*` declarations, no dark block. It renders white in the dark theme. ADR 0012's list of chrome elements never named it, and `d12canvas-next` ticket 74 did not catch it. Ticket 39 now floats it over the full-bleed canvas in `D12Canvas.App`, so on a dark host it is the brightest thing on screen. Its light literals equal `Palette`'s light token values byte for byte, so copying `Palette`'s token block moves no light pixel.

### Classes

- **Tokenised.** Reads a `--d12-*` token, or is a token's declared default.
- **Escape hatch.** A per-element token by design (ADR 0012).
- **Decided.** An ADR has already said what it becomes.
- **Shared.** Should read an existing shared token.
- **No token.** A theme question with no existing token that fits.
- **Not theme.** `transparent`, `inherit`, or a false positive.

### Literals still in the library

| Where | Rule | Literal | Class | Light moves on swap | Baselines that render it |
|---|---|---|---|---|---|
| `DiagramCanvas.razor:265` | `.diagram-container` border | `#ccc` | Shared, `--d12-border` | **Yes.** The canvas's own `--d12-border` is `rgba(0,0,0,0.1)`, about `#e6e6e6` on white. Every canvas baseline moves. | All canvas pages, light and dark. Dark keeps a light-grey frame today. |
| `DiagramCanvas.razor:311-312` | `.drag-over-affordance` | `#4a90d9`, `rgba(74,144,217,0.08)` | Shared, `--d12-accent` | **Yes.** Near-miss against `#2f80ed`. | `DragAndDropPlacement.DragInProgress`, light only. |
| `DiagramCanvas.razor:325` | `.selection-bounding-box` | `#2f80ed` | Shared, `--d12-accent` | No, byte-identical. | Marquee, MultiSelection, GroupTabStop, Theme (light and dark), context menu two-instance. |
| `DiagramCanvas.razor:343` | `.group-resize-handle` | `#2f80ed` | Shared, `--d12-accent` | No, byte-identical. | Same as the bounding box. **Not in any earlier ticket.** |
| `DiagramCanvas.razor:408, 420` | `.edge-line`, `.edge-arrowhead` | `#4a4a4a` | Decided, ADR 0016: `--d12-edge` | No. | Seven edge baselines, **all light**. |
| `DiagramCanvas.razor:415, 424` | `.edge-line.selected`, `.edge-arrowhead.selected` | `#2f80ed` | Decided, ADR 0041: becomes an accent halo | Yes, by design. | `EdgeSelection.SelectedEdge`, light only. |
| `DiagramCanvas.razor:439-440` | `.floating-endpoint` | `#f39c12` fill, `#ffffff` stroke | No token | n/a | `FloatingEndpoint`, `ConnectorPaletteEntry`, light only. |
| `ComponentContainer.razor:95` | `.edit-mode` | `#3498db` | Decided, ADR 0035: deleted | n/a | None. |
| `ComponentContainer.razor:104` | `.selected` | `#2f80ed` | Shared, `--d12-accent` | No, byte-identical. | Most selection baselines, light and dark. |
| `ComponentContainer.razor:117-118` | `.port` | `#2ecc71` fill, `#ffffff` border | No token | n/a | Any baseline with a hovered or selected instance, light and dark. |
| `ComponentContainer.razor:132` | `.port.port-focused` | `#f39c12` outline | No token | n/a | **None.** ADR 0050 reuses this style for the provisional port. |
| `ComponentContainer.razor:195` | `.resize-handle` | `#3498db` | Shared, `--d12-accent` | **Yes.** Near-miss against `#2f80ed`, about 25 baselines. | About 25, light and dark. |
| `PropertyPanel.razor:90-91` | `.d12-property-panel` | `#fff` background, `#ccc` border | Shared, `--d12-surface` / `--d12-border` | No, if it copies `Palette`'s values. | Four `PropertyPanel` baselines, light only. |
| `PropertyPanel.razor:100, 121` | `-empty`, `-label` text | `#666` | Shared, `--d12-muted-text` | No, as above. | As above. |
| `PropertyPanel.razor:127` | `.d12-property-panel-input` border | `#ccc` | Shared, `--d12-border` | No, as above. | As above. |
| `SelectionContextMenu.razor:38` | `--d12-shadow` | `rgba(0,0,0,0.15)` | Escape hatch, but **no dark value** in either dark block | n/a | Context-menu baselines, light and dark. |
| `Text.razor:32`, `StickyNote.razor:41` | inline editor outline | `rgba(0,0,0,0.4)` | Decided, ADR 0034: `--d12-inline-edit-outline` | No. | Sticky note: `InlineTextEditing`, light only. Text editor: **none**. |
| `Image.razor:32-34` | `.d12-image-placeholder` | `#f0f0f0`, `#999999`, `#666666` | Decided, ADR 0034: surface / border / muted-text | Border and text are near-misses. | **None.** See below. |
| `BuiltInComponents.cs:19, 32, 45` | `DefaultProps` | `#FFFFFF`, `#333333`, `#FFEB3B`, `#000000` ×2 | Decided, ADR 0034: null and board tokens; sticky colours stay | No. | Many, light; PortDrag dark has all four built-ins. |
| `Rectangle.razor:15`, `StickyNote.razor:50`, `Text.razor:41` | `[Parameter]` fallback `Props` | Same literals again | Decided, ADR 0034, **same change** | No. | As above. |

`Rectangle`, `StickyNote` and `Text` each declare their defaults twice: in `DefaultProps` and again in the component's own `Props` initialiser. ADR 0034 names only the first. Change both, or a component mounted outside the registry keeps the frozen literal.

### Already tokenised

The token declarations on `.diagram-container`, `.d12-palette` and `.d12-context-menu`, four blocks each (default, `prefers-color-scheme: dark`, `data-d12-theme="light"`, `data-d12-theme="dark"`). Also `--d12-connector-preview: #2ecc71` (escape hatch, no dark value, which is fine for a green line). Every other colour in those three components reads a token.

The "copies" ADR 0012 describes are not copies. On light, the canvas has `--d12-surface: #f0f0f0`, `--d12-border: rgba(0,0,0,0.1)` and `--d12-muted-text: #6b6b6b`. The palette and menu have `#fff`, `#ccc` and `#666`. Dark values differ too (`#1e1e1e` against `#2a2a2a`). That may be deliberate, since a floating panel wants to stand off the board, but no ADR says so. It matters for the property panel: copy the palette's values, not the canvas's.

### Not theme

`transparent` ×10 (`.view-mode` border, two editor backgrounds, `color-mix` and grid gradients, palette and menu button resets), `inherit` ×7 (font and `color: inherit` resets), and `currentColor` ×0. The `white` hit at `DiagramCanvas.razor:471` is `white-space`, and the `blue` hit in `DiagramCanvas.razor.cs` is a comment.

### Blues

Three distinct values across eight declarations: `#2f80ed` (the accent, five literal uses), `#3498db` (`.edit-mode` and `.resize-handle`) and `#4a90d9` (`.drag-over-affordance`). Ticket 26's "fourth and fifth blue" counted declarations, not values. Once ADR 0035 deletes `.edit-mode`, two near-miss blues remain, and both move light baselines when swapped.

Swapping the five accent-identical literals moves **no pixel in either theme**, because `--d12-accent` is `#2f80ed` in dark too. The only gain is that a host retuning `--d12-accent` stops desynchronising the selection outline, the bounding box, its handles and the instance handles from the marquee. That is ticket 14's and ticket 26's complaint, and it is now five pieces of chrome, not three.

### Where the suite cannot see a swap

- **No baseline renders the image placeholder.** ADR 0034 says its swap "moves a light baseline slightly". It moves no baseline, because none exists. The change is unverified, not small.
- **No dark baseline contains an edge, a floating endpoint, the drag-over affordance, an inline editor or the property panel.** ADR 0016 and ADR 0034 both say their diffs are "confined to the dark cases that are currently broken", but none of those dark cases is screenshotted. The defects they fix (the invisible edge, the invisible editing outline) would pass the suite before and after.
- **Nothing renders `.port-focused` or the `Text` editor**, in either theme.
- The decisions that own these swaps should add dark-case demo pages when they are implemented. Otherwise ADR 0025's verification shape has nothing to assert against.

### What this leaves undecided

Two groups have no owning decision:

- the shared-token swaps and the no-token roles on the canvas and container (border, drag-over, bounding box, both handle sets, `.selected`, ports, port focus, floating endpoint, menu shadow in dark)
- the property panel, which needs a token block and dark values

Split out as [Tokens for the unowned canvas chrome literals](59-unowned-canvas-chrome-tokens.md) and [Theming the property panel](60-property-panel-theming.md).
