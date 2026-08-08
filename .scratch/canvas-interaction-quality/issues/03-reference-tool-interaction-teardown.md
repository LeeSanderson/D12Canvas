# Interaction teardown of reference tools

Type: research
Status: open

## Question

Establish the reference bar for canvas interaction, gesture by gesture, so this effort's decisions are measured against what good actually looks like rather than against the ten notes that seeded the map.

`CONTEXT.md` already describes D12Canvas as "a Miro-like board", but that comparison has never been made concrete. The destination was widened specifically to go looking for what is missing, and this ticket is the mechanism for that.

Survey Miro, FigJam, tldraw and Excalidraw — the last two are open source, so their source is a primary source, not just observed behaviour. For each, document:

- **Pointer arbitration.** What does left-press do on empty canvas, on an unselected object, on a selected object, on a handle, on a port? What does right-press do? What modifier keys change the answer, and is there a persistent tool mode (ADR 0009 explicitly chose *not* to have one — is that choice still right)?
- **Selection affordances.** When do handles and connection points appear — hover, selection, focus, proximity, or a drag in progress? How is the resize-handle vs connection-point collision solved geometrically? This bears directly on tickets 06 and 07.
- **Selection-anchored chrome.** Which tools float a property bar over the selection versus docking a panel, how it is placed and flipped, and what it does while the selection is being dragged. Bears on ticket 08.
- **Feedback during a gesture.** Do connectors follow live? Is there a ghost, a snap line, a dimension readout? Bears on tickets 05 and 11.
- **What they have that D12Canvas does not.** This is the widening: catalogue gestures and affordances outside the ten seed notes, and note which look load-bearing for the feel versus merely present. Feed anything sharp enough into the map's fog or a new ticket; anything past the destination goes to Out of scope.

Where a behaviour is worth adopting, record *why* it works, not just what it is — the decision tickets need the reasoning, not a feature list.

Capture findings as a markdown file in the repo and link it from this ticket.
