# Create-adjacent-and-connect

Type: prototype
Status: open
Blocked by: 06

## Question

Decide whether a selected instance offers a one-gesture "create a new instance beside me and connect it", and what affordance carries it.

Ticket 03 named this the single biggest widening beyond the ten seed notes: all four reference tools have it, D12Canvas has none of it, and the teardown's judgement is that it is **the gesture that makes a tool feel like a diagramming tool rather than a shape editor** — load-bearing for feel in a way that, say, a laser pointer is not. The destination was widened specifically to go looking for things like this.

The reference answers converge on the affordance and diverge on the trigger:

- **Miro and FigJam both hang it off the same side dot that draws a connector**, switching meaning by proximity — drag the dot to draw an edge, click it (or release it on empty canvas) to create and connect in one move.
- **Excalidraw binds it to `Ctrl/Cmd+Arrow`** ("Create Flowchart"), keyboard-first with no pointer affordance at all.
- **Miro adds `Tab` and `Enter` variants** in its mind-map and sticky flows.

That convergence is why this is blocked on ticket 06: the affordance it would hang off is the port dot whose visibility and geometry ticket 06 is deciding, and overloading a dot that is itself being redesigned would be deciding the same thing twice.

Prototype and decide:

- **The trigger.** Click-the-port-dot, drag-and-release-on-empty-canvas, a keyboard chord, or more than one. The proximity switch between "draw an edge" and "create and connect" is a feel question, not a rules question — Miro and FigJam make it work, and whether it feels ambiguous or obvious is exactly what a prototype settles.
- **What gets created.** The same component type as the source, the registry's default, or a picker. ADR 0009's placement model gives `DefaultSize` per registered type, so the type choice determines the size for free.
- **Where it goes**, and what happens when that position is already occupied — cascading offset (as ADR 0009's click-to-add already does), displacement, or nearest free slot.
- **Whether the new instance takes selection and focus**, which is what makes the gesture chainable into building a flow without touching the palette. Ticket 67 of `d12canvas-next` already established the focus-after-placement mechanism this would reuse.
- **Whether the whole thing is one history entry.** ADR 0007 holds a gesture is exactly one entry, and this creates an instance *and* an edge, so it wants `CompositeCommand` — confirm rather than assume.
- **Keyboard parity**, which ticket 16 owns generally but which Excalidraw's `Ctrl/Cmd+Arrow` answers directly for this gesture.
