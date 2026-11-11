# Board list page

Type: prototype
Status: resolved
Blocked by: 07

## Question

What does the board list at `/` look like with Bootstrap?

Today it is a table (Name, Created, Updated, actions), a skeleton loader and an empty state with a hand-drawn SVG. Prototype it: table versus card grid, per-board actions (open, rename, delete) as buttons or a dropdown, the empty state with Bootstrap Icons, and the narrow-screen layout.

The nav bar is settled (see [Shared nav bar](07-shared-nav-bar.md)). The page's `h1` "Boards" and the New board button sit in the page header row, so the page body starts with the list itself. The old subtitle ("Create and manage your diagramming boards.") has no place yet. Decide whether to keep it.

## Answer

Decided with the user on 2026-11-11 after trying three variants. The prototype is on the `prototype/board-list` branch, built on top of `prototype/editor-toolbar`: variant A (a table with a ⋮ menu per row), B (a card grid) and C (a searchable list with inline Rename and Delete buttons). B won, with C's empty state and a board sketch on each card.

1. **Card grid.** `.row.row-cols-1.row-cols-sm-2.row-cols-lg-3.row-cols-xl-4.g-3` inside `.container-xl.py-4`. One column on phones, four on wide screens.
2. **New board tile.** When there are boards, the first cell is a dashed "New board" tile (`plus-lg` icon) that creates a board, alongside the New board button in the page header.
3. **Each card.**
   - Top: a 16:9 board sketch (point 7) on a dotted `bg-body-tertiary` background.
   - Body: the board name as a `stretched-link` to `/board/{id}`, truncated with the full name in `title`. Below it, "Edited 3 min ago" in small muted text, with the full date in `title`. The relative text steps through just now, minutes, hours, yesterday, days, then "On 12 Oct" (the year is added outside the current year).
   - A ⋮ menu (`three-dots-vertical`, `btn-link`) above the stretched link, with Rename… (`pencil`) and Delete… (`trash`, `text-danger`). Clicking the card opens the board, so the menu has no Open item. It uses the shared Blazor-state dropdown from [Editor toolbar](08-editor-toolbar.md).
   - Hover raises the shadow and tints the border with `--bs-primary-border-subtle`.
4. **Empty state.** One centred list-group card, at most 480px wide: the `easel` icon, "No boards yet", "Create your first board to start diagramming." and a small New board button. The grid and the dashed tile are not shown.
5. **Loading.** Four placeholder cards (`.placeholder-glow`): a 16:9 placeholder block, a name line and a date line.
6. **Subtitle.** Dropped. The page header already says Boards.
7. **Board sketch.** A new **library** component, so other hosts can reuse it. It takes a `Board` and draws an `<svg>`: one rectangle per component from its `Bounds`, in a sticky-note style for sticky notes and an outline for the rest, and one line per edge between the centres of the components it connects (a floating end uses its own point). Edge-label components are skipped. The `viewBox` is the union of the component bounds, so any board scales to fit, and `vector-effect="non-scaling-stroke"` keeps lines thin. A board with no components shows "Empty board". It uses `--d12-*` tokens, not Bootstrap's, so it follows `data-d12-theme`. Being a library change, it needs the full visual suite per `docs/agents/testing.md`.
8. **Loading the sketches.** The list page loads each board with `IBoardStore.LoadAsync` after the list renders and fills its card when it arrives. A card shows a blank sketch area until then. If this gets slow, the fix is to load only the sketches for cards in view and load more on scroll. That is not part of this effort (see the map's Out of scope).
9. **Rename and Delete.** Rename opens the existing rename dialog. Delete asks "Delete "<name>"? This cannot be undone." In the spec both are `<dialog>` modals per [Bootstrap delivery and JS](02-bootstrap-delivery-and-js.md). The prototype uses `confirm()`.

Prototype shortcuts the spec should not copy: the sketch is App code (`PrototypeBoardSketch.razor`) using `--bs-*` colours and finds sticky notes by searching the type key for "sticky", `?state=loading|empty|sample` fakes data, and variants A and C plus the switcher are throwaway.
