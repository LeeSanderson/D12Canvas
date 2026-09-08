# Enumerate every hard-coded colour left in the library

Type: task
Status: open
Blocked by:

## Question

Produce the complete list of colour literals still sitting in `D12Canvas`'s own CSS and markup, so the token-adoption sweep stops being finished twice and found incomplete twice.

Ticket 74 in `d12canvas-next` resolved as "token adoption across all remaining chrome" and said so in its title. It was wrong twice since:

- [Edge visibility and board-content theming](14-edge-visibility-and-board-content-theming.md) found `.selection-bounding-box`, `.drag-over-affordance` (a *third* hard-coded blue) and `.floating-endpoint` still carrying literals, and noted that the bounding box's byte-identical accent means a host retuning `--d12-accent` desynchronises it from the marquee drawn directly above it.
- [Themed visual defaults for built-in component types](25-built-in-themed-defaults.md) found four more, in the built-ins' own `<style>` blocks: the inline-edit outline in both `Text.razor` and `StickyNote.razor` (`rgba(0, 0, 0, 0.4)`, effectively invisible against the dark theme's backdrop, so the "you are editing this" affordance disappears) and `Image.razor`'s three-literal placeholder.

Neither was looking for them. Both were working an unrelated question and tripped over one. That is a pattern rather than two accidents, and the next ticket will find a fifth.

This is a task rather than a decision: nothing here needs judgement until the list exists. What each literal *becomes* belongs to whichever ADR owns that element, and ADRs 0016 and 0034 have already assigned seven of them.

The work:

- **Enumerate**, across `D12Canvas`'s `.razor` files, `.css` files and any inline styles emitted from C#: every hex literal, `rgb()`/`rgba()`/`hsl()` call and named CSS colour keyword. Include `transparent` and `currentColor` in the listing but mark them, since neither is a theme decision.
- **Classify each** as already-tokenised, a deliberate escape hatch (`--d12-connector-preview` is the established precedent, and ADR 0034 adds `--d12-inline-edit-outline`), a literal that should read a shared token, or a literal with no theme question at all.
- **Flag every one whose light value would move if swapped**, since the convention across ADRs 0016 and 0034 is that a light value stays byte-identical so no light baseline moves and the diff is confined to the dark cases. A near-miss against a shared token is the interesting case and `Image.razor`'s placeholder is the known example.
- **Note which are unreachable from the current visual suite**, so a swap that moves pixels nobody screenshots is known to be unverified rather than assumed safe.

Deliberately not in scope: `D12Canvas.Demo` and `D12Canvas.App`, which are host code and own their own styling.
