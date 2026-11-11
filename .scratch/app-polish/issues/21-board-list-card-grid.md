# Board list card grid

Status: resolved
Blocked by: 14, 18

Spec: [App polish](../spec.md), "Board list page". Decision record: [Board list page](09-board-list-page.md).

## What to build

The board list becomes a grid of cards, each with a sketch of the board.

- `.row.row-cols-1.row-cols-sm-2.row-cols-lg-3.row-cols-xl-4.g-3` inside `.container-xl.py-4`. The old table and subtitle go.
- When boards exist, the first cell is a dashed New board tile (`plus-lg`) that creates a board.
- Each card: a 16:9 board sketch (the library component) on a dotted `bg-body-tertiary` background; the board name as a `stretched-link` to the board, truncated, full name in `title`; "Edited <relative time>" in small muted text, full date in `title`; a ⋮ menu (`three-dots-vertical`, `btn-link`, on the shared dropdown component) above the stretched link with Rename… (`pencil`) and Delete… (`trash`, `text-danger`). Hover raises the shadow and tints the border with `--bs-primary-border-subtle`.
- Relative time is a pure function taking "now": "just now", minutes, hours, "yesterday", days, then "On 12 Oct", adding the year outside the current year.
- Empty state: one centred list-group card, at most 480px wide, with `easel`, "No boards yet", "Create your first board to start diagramming." and a small New board button. No grid, no tile.
- Loading: four `.placeholder-glow` cards (a 16:9 block, a name line, a date line).
- After the list renders, load each board with `IBoardStore.LoadAsync` and fill its card's sketch when it arrives. The sketch area is blank until then.
- Rename opens the rename `<dialog>`. Delete opens a confirmation `<dialog>` on the shared dialog module: `Delete "<name>"? This cannot be undone.`

## Acceptance criteria

- [x] Boards show as cards, one column on phones and up to four on wide screens.
- [x] Clicking a card opens its board; the ⋮ menu doesn't.
- [x] Each card shows its sketch once loaded, and an empty board's card shows "Empty board".
- [x] The New board tile and the header's New board button both create a board and open it.
- [x] Rename from the ⋮ menu renames the board; Delete asks for confirmation and removes it only on confirm.
- [x] With no boards, only the empty-state card shows.
- [x] While the list loads, four placeholder cards show.
- [x] Unit tests for the relative-time function cover each step and the year rule.
- [x] App tests through the page harness: the empty state shows one card; cards show the name link and "Edited …" from the store's clock; New board calls `CreateAsync` and navigates; Rename and Delete in the ⋮ menu call the store; cancelling Delete leaves the board.

## Comments

Done. The board list is a card grid with a New board tile, placeholder cards while it loads, and the list-group empty state. Each card loads its board after the list renders and shows `BoardSketch` once it arrives. A board that fails to load keeps a blank sketch area and isn't retried, so one corrupt board can't break the page. The ⋮ menu is the shared `Dropdown`, and only its toggle button sits above the stretched link (`z-index: 2`). That keeps the open menu and its outside-click layer in the root stacking context, so the next card's ⋮ button can't paint over an open menu. Delete uses a new `DeleteBoardModal` on `ModalDialog`, and `confirm()` is gone.

`RelativeTime.Describe(when, now)` returns lower-case text ("just now", "5 min ago", "3 hours ago", "yesterday", "4 days ago", "on 12 Oct", "on 12 Oct 2025") so it reads as "Edited on 12 Oct". It reads the date in `now`'s offset, and the page passes `TimeProvider.GetLocalNow()`. The full date in `title` is "15 January 2026 at 09:30", invariant culture like the rest of the English UI.

Tests are in `BoardListTests` and `RelativeTimeTests`. `InMemoryBoardStore` gained `ListGate` and `LoadGate` for the loading and blank-sketch states. `RenameTests` now opens Rename from the ⋮ menu.
