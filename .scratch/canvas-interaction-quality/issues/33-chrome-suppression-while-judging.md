# Chrome suppression while a property is being judged

Type: grilling
Status: resolved
Blocked by: 08

## Question

Decide whether selection chrome hides while the user is judging the *result* of a change, not only while a pointer gesture is running.

Graduated out of the map's fog by ADR 0021, which supplies the surface this was waiting on. ADR 0021 already decided that the property bar hides for the duration of a pointer gesture and while a context menu is open. This asks the adjacent question it deliberately did not answer: after a property changes, should the rest of the selection furniture get out of the way so the change can actually be seen?

The reference implementation is tldraw's `isChangingStyle`, which ticket 03's teardown called the cheapest good trick in the whole document: a self-expiring one-second flag, set on a style change, a keyboard nudge or a paste, which suppresses *all three* canvas overlays (shape indicators, shape handles, and the selection box with its handles). Any pointer move clears it. The comment on the nudge case says it plainly: "Hide the selection overlay while nudging, same as when changing styles."

Decide:

- Which surfaces the suppression covers. This is the reason it is not ADR 0021's to take: the candidates are `.selection-bounding-box` and its eight group-resize handles, per-instance resize handles, ports, and the focus ring, none of which the property bar owns. The bar itself must plainly *not* hide, since the next click after a colour change is usually another colour.
- What triggers it. A property commit is the obvious one. `NudgeCommand` and paste are tldraw's other two, and both already exist here, so the trigger set may be wider than "a property changed".
- How it ends. A self-expiring timer, the next pointer move, the next commit, or some combination. A timer is state that outlives the interaction that set it, which is the kind of thing that leaks.
- Whether it composes with ADR 0021's gesture rule without a third mechanism. Both end up asking "is selection chrome visible right now", and two independent flags answering that question is how the nine-flag smear ticket 01 dismantled got started.
- Whether it is expressible in CSS, as ADR 0015's framing flight was. A class applied for a duration, with `prefers-reduced-motion` handled by a media query rather than plumbed through C#, is the cheaper shape if the trigger can be expressed as a class.

Cheap and clearly right in the small, so the risk here is scope: it touches every overlay on the canvas, and every one of those is somebody else's decision.

## Answer

**Selection chrome hides while the property bar has `:hover` or `:focus-within`, instantly and by one CSS rule, and a selected edge stops repainting itself.** Recorded as [ADR 0041](../../../docs/adr/0041-selection-chrome-while-judging.md).

- **The edge case was a rendering defect.** `.edge-line.selected` repaints the stroke in the accent, so an `EdgeColour` or arrow change from the bar is invisible until deselection, and suppression would only flash the true colour. Selection becomes a translucent accent halo on a second path under the edge. This takes ADR 0016's deferred halo alternative and removes its accent-blue limit.
- **Surfaces:** the bounding box and group handles, resize handles, port strips, port circles, the `.selected` outline and the edge halo all hide. The bar stays. Accepted cost: a user with focus on an instance and the pointer on the bar loses the focus indicator until the pointer leaves.
- **Triggers:** none. The bar's hover and focus state is the whole mechanism. Nudge and paste are excluded, because without a timer only a pointer move ends them, and a keyboard user would lose their focus indicator indefinitely.
- **End:** leaving the bar by pointer, `Escape` or `Tab`. No timer; the library still has none.
- **Composition with ADR 0021:** no third mechanism. A bar hidden for a gesture or a menu can be neither hovered nor focused.
- **CSS:** yes, `:has()` on `.diagram-container`, with `visibility: hidden` and no transition, so there is no reduced-motion case.

Amends ADRs 0016 and 0021, and corrects `CONTEXT.md`'s `Edge colour` and `Property bar` entries. No new tickets and no fog added.
