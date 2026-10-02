# Selection chrome hides while the property bar is in use, and a selected edge keeps its own look

While the property bar has `:hover` or `:focus-within`, every piece of selection chrome on the canvas is hidden, instantly, by one CSS rule. A selected edge stops repainting itself in the accent colour and shows a halo under its own stroke instead.

The question was whether chrome should get out of the way while the user judges the result of a change. The reference answer is tldraw's `isChangingStyle`, a one-second flag set on a style change, a nudge or a paste, cleared by any pointer move. It is not adopted. Two of its three triggers do harm here, its end condition does not fit this library's bar, and the case that most needed it turned out to be a rendering defect no suppression could fix.

## The edge case was never about timing

`.edge-line.selected` sets `stroke` to the accent and `stroke-width` to 3, and the arrowheads follow through `context-stroke`. So a selected edge shows none of its own colour, and changing `EdgeColour` or an arrow role from the bar has no visible effect until the edge is deselected. Suppression would show the new colour for as long as it lasted and then cover it again. That is a flash, not a chance to judge.

So selection on an edge becomes a second stroked path under the edge's own: wider than it, translucent, painted with `--d12-accent`. The edge keeps its colour, width and arrowheads whether selected or not. This is the alternative ADR 0016 named as better and deferred "with the affordance work", and it removes the limit that ADR recorded, where an author who coloured an edge accent blue saw no change on selection. Whether the 2-to-3 stroke-width bump survives is an implementation detail, since the halo is now the non-colour cue.

ADR 0037 lets several edges be selected at once, and each selected edge carries its own halo.

## No timer and no trigger, only the bar's own state

The library has no timers. ADR 0026 declined one for keyboard alignment guides as "the first, for a feature nobody asked for", and ADR 0029 declined one for edge auto-scroll. A self-expiring flag would be that first timer.

Without a timer, the only ends available are a pointer move, a press or a selection change, and none of them fits. A commit from the bar happens with the pointer already on the bar, and reaching for the next swatch is a pointer move, so chrome would return before anything could be judged.

The bar's own state already marks the period that matters. While the pointer is over it or focus is inside it, the user is choosing a value and looking at the result. So the rule is `.diagram-container:has(.property-bar:hover, .property-bar:focus-within)` and nothing else: no C#, no flag, no event. Leaving the bar by pointer, `Escape` or `Tab` brings the chrome back.

It hides chrome on hover even before any value changes. That is accepted, because nothing in the hidden chrome can be used while the pointer is on the bar.

It needs no composition rule with ADR 0021. The bar is hidden during a pointer gesture and while a context menu is open, and a hidden bar cannot be hovered or hold focus, so the two rules cannot disagree. There is one question, "is the bar in use", and the browser answers it.

## Nudge and paste are not triggers

Without a timer, a nudge-triggered hide could only end on a pointer move. A keyboard user nudging never makes one, so their selection outline would stay hidden for the whole session. Since `.component-container:focus` is `outline: none`, that outline is also their only sign of where focus is. tldraw avoids this only because its flag expires. Paste fails the same way.

## What hides

All of it, leaving the bar as the only selection surface on screen:

- `.selection-bounding-box` and its eight group-resize handles
- per-instance resize handles and the four port strips
- port circles, which are visible on `.selected`
- the `.selected` outline
- the edge halo

The outline matters most. It sits 2px outside the border at 2px wide, so it is the piece that hides a `Stroke` or `StrokeWidth` change.

The accepted cost: a user with keyboard focus on an instance and the pointer resting on the bar loses the focus indicator until the pointer leaves. Under `:focus-within` nothing is lost, since focus is in the bar.

## Instant, with no transition

The chrome switches to `visibility: hidden` with no transition. With no motion, `prefers-reduced-motion` has nothing to change, ADR 0025's suite-wide reduced-motion setting has nothing to strip, and no baseline can catch a half-faded frame. The cost is a small pop as the pointer reaches the bar.

## What this amends and confirms

**ADR 0016 is amended in one section.** "A selected edge is always the accent" is replaced: selection is a halo under the edge, never a repaint, and the rejected alternative "keeping the edge's own colour when selected, with a halo or dash as the cue" becomes the decision.

**ADR 0021 is amended by addition.** The bar now also decides when the rest of the selection chrome is visible, by its own hover and focus state. Its gesture and context-menu rules are unchanged.

**ADR 0010's focus model is untouched.** No focus moves and no tab stop changes. Only the outline's visibility does, under the condition above.

**Nothing here is board state** and nothing is C#-side state.

## Considered and rejected

- **tldraw's self-expiring flag**: the library's first timer, state that outlives the interaction that set it.
- **Ending suppression on any pointer move**: the next swatch is a pointer move away, so chrome returns before the change can be judged.
- **Nudge and paste as triggers**: with no timer, a keyboard user's focus indicator stays hidden indefinitely.
- **Suppression standing in for the edge fix**: a flash of the true colour, then the accent repaint covers it again.
- **Keeping the outline while the instance itself holds focus**: a second selector condition for a narrow mixed-input case, and it leaves the piece that most hides a stroke change.
- **A fade, with a reduced-motion query**: motion to plumb and baselines to time, for a transition nobody needs.
- **A C# flag on `DiagramCanvas`**: a second answer to "is selection chrome visible right now", which is how the nine-flag smear ADR 0018 dismantled began.
