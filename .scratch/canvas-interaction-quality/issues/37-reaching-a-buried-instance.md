# Reaching a buried instance

Type: grilling
Status: open
Blocked by: 11

## Question

Decide how a user selects an instance that is completely covered by another, and pick between the two candidate routes rather than shipping both.

Two decisions have now pushed this question at each other and neither took it. ADR 0022 recorded press-through with the platform accelerator modifier as a real hole, noting that ADR 0017 *widened* it by making the multi-selection box a solid hit target that consumes presses aimed beneath it, and rejected the modifier only because Ctrl is the reference bar's snapping-suppression key and the alignment-guides work has the better claim on it — explicitly "askable again once that decision lands". Ticket 10 was handed Figma's `Select layer` menu item by the reference-tool teardown and handed it back, because the menu route turned out to be the most expensive item on that ticket by a wide margin.

So both candidates are live and they answer the same user question. Deciding them separately risks shipping two routes to one thing, which is the objection ADR 0015 and ADR 0022 each used to reject a feature.

Decide:

- **Which route, or both.** A modifier on the press is nearly free once ticket 11 has settled the constrain vocabulary and said whether Ctrl is spent. A menu item is discoverable without knowing a modifier exists, and it is the only route that shows the user what is down there rather than cycling them through it blind.
- **Whether an instance needs an identity a user can read.** This is the menu route's real blocker. Figma's version works because every layer carries a user-visible name; `DisplayName` in D12Canvas lives on the *registration*, so three overlapping rectangles produce three rows reading "Rectangle". Fixing it by naming instances touches ADR 0001 and ADR 0003. Fixing it by hover-highlighting the board content under each row instead needs a highlight affordance that does not exist, and makes a menu row reach out and change what is drawn on the board.
- **Whether the modifier route needs a platform check.** ADR 0022 flagged that `DiagramCanvas.razor.js`'s existing `(event.ctrlKey || event.metaKey)` is right for keyboard shortcuts and wrong for a pointer modifier, because Ctrl+click is the macOS secondary click. Whatever modifier is chosen inherits that split.

**Bank this before implementing either route, so it is not re-derived.** ADR 0017 rejected a C#-side ranked hit list because it would compete with the DOM's own order, so the user clicks the top thing and something beneath wins. That objection kills the obvious implementation — a `Board` scan over `Bounds` sorted by `ZIndex` — and it does **not** kill the feature. `document.elementsFromPoint` returns every element at a point in the DOM's own paint order, topmost first; running ADR 0017's existing marker walk over that list and deduplicating by entity yields the stack from the same authority that classified the press, with no geometry in C# at all. It is one JS call, made when the surface opens rather than per frame. Either route can use it: the menu to list the stack, a modifier to cycle it.

Note also that ADR 0017 fixed hit *order* independently — instances always beat edges, regardless of the `PreviousZIndex()` bug — while leaving the paint arithmetic alone, so hit order and paint order already disagree in one case. A stack read from the DOM inherits paint order, which is the order the user sees, and that is the right one to expose.

Amends ADR 0017 or ADR 0022 depending on the route chosen, and touches ADR 0001/0003 only if instances gain readable identity.
