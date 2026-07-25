# 26 — Palette lists registered types

**What to build:** An end user sees what they can place: the palette — canvas chrome, not board content — lists every registered component type with its icon and display name, grouped by category. It renders standalone, is reference-wired to the canvas, and is positioned entirely by the host's own CSS outside the pannable surface. (ADR 0002.)

**Blocked by:** 20 (Component-type registration contract & registry)

**Status:** resolved

- [x] Every registered type appears with `DisplayName` and `Icon`, grouped by `Category`
- [x] The palette renders standalone and is positioned by host CSS — it does not pan or zoom with the board
- [x] Palette entries carry accessible names
- [x] bUnit coverage; screenshot case for the rendered palette

## Comments

Implemented as `D12Canvas/Palette.razor` (+ `Palette.razor.cs`), an ordinary standalone component (no `ParentCanvas`/cascading wiring — it isn't nested inside `DiagramCanvas`, and neither drag-and-drop placement (ticket 27) nor click-to-add (ticket 28) exists yet to need a canvas reference; that wiring lands with whichever of those tickets first needs it) that injects `IComponentRegistry` and renders every registration grouped by `Category`, with entries lacking a `Category` bucketed under a fallback "Uncategorized" heading. Each entry is a `<button>` (focusable/keyboard-reachable, ready for tickets 27/28 to attach placement behavior) carrying `aria-label="@entry.AccessibleName"`, an optional icon `<span>` (omitted entirely when `Icon` is null/empty, rather than rendering an empty element), and the `DisplayName` as visible text.

**Registry gained an enumeration surface:** `IComponentRegistry`/`ComponentRegistry` previously only supported `Resolve(key)` — nothing could list every registration, which the palette needs. Added `IReadOnlyList<ComponentRegistration> All { get; }`, backed by a `List<ComponentRegistration>` populated alongside the existing `Dictionary` in `Register` (rather than relying on `Dictionary` enumeration order, which .NET doesn't contractually guarantee) so registration order is deterministic and covered by its own `ComponentRegistryTests` cases.

**Styling is plain hardcoded CSS** (inline `<style>` block, matching `DiagramCanvas`/`ComponentContainer`'s existing convention), not the `--d12-*` theme-token layer ADR 0012 describes — that layer doesn't exist in code yet (tracked separately as ticket 73, still open) and ticket 74 (blocked by this ticket) is explicitly where the palette adopts tokens once 73 lands. No positioning CSS (`position`, `top`/`left`, etc.) is set on the root, so a host is free to place it however it likes.

**Demo & visual test:** new `/palette-demo` page in `D12Canvas.Demo` renders `<Palette />` standalone inside a host-styled wrapper div; the existing `demo-note`/`stress-item` registrations in `Program.cs` gained `Icon`/`Category` values (`"Notes"`/`"Stress Test"`) purely for this demo — harmless to `/board-demo`'s existing rendering since `Icon`/`Category` aren't used by `DiagramCanvas`. Added `D12Canvas.VisualTests/PaletteVisualTests.cs` (`RenderedPalette_MatchesBaseline`) as the new screenshot case. Adding the new demo page to `NavMenu.razor` (for consistency — every other demo page is listed there) changed the shared layout's rendered markup, which cascaded into the two pre-existing `BoardRenderingVisualTests` baselines (`RenderedBoard_MatchesBaseline`, `ZoomedAndPannedBoard_MatchesBaseline`); both were regenerated and re-verified — the only diff in each was the added nav link.

bUnit coverage (`PaletteTests.cs`): every registration rendered with display name/icon, icon element omitted when absent, grouping by category (including the uncategorized fallback), accessible names as `aria-label`, standalone rendering with no registrations and with no `DiagramCanvas` ancestor.
