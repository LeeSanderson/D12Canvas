# Chrome suppression while a property is being judged

Type: grilling
Status: open
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
