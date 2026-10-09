# Canvas Interaction Quality

Status: ready-for-agent

Synthesized from the resolved `canvas-interaction-quality` wayfinder effort ([map](map.md), tickets 01 to 64). The decisions are locked in ADRs 0013 to 0067 (`docs/adr/`) and the vocabulary in the root `CONTEXT.md`. This spec is the feature-level view over them, the same shape `d12canvas-next/spec.md` took over ADRs 0001 to 0012.

One fact frames everything below: none of ADRs 0013 to 0067 has reached library code. `D12Canvas/` last changed on 2026-08-08 and ADR 0013 landed the next day. The wayfinder effort produced decisions, probes and prototypes on branches, and nothing else. So this is a specification of fifty-five decided behaviours to be built against a codebase that still runs the `d12canvas-next` interaction layer, and the implementation tickets cut from it will land in this same `issues/` directory.

## Problem Statement

The current canvas technically supports every gesture a diagramming tool needs and feels like none of them. Using `D12Canvas.App` for an afternoon turns up defects that are not polish:

- Every one of the six canvas gestures can leak. An ordinary click on empty canvas can leave the canvas panning until the pointer next moves, because the press awaits a round-trip before setting its flag and a fast release gets processed first. Release outside the canvas leaks pan, move, resize and connector drags. Release over a shape leaks the marquee. A shape dragged past the clipped edge both leaks and never enters history, so Ctrl+Z afterwards deletes it instead of undoing the move.
- A plain left-drag on empty canvas pans, so there is no way to add to a selection with a band, and Shift is spent on "draw a band at all".
- Snap-to-grid previews unsnapped and snaps at release, so shapes jump to the grid when let go. Attached edges do not follow a shape mid-drag.
- Zoomed out past 0.16x the whole board is unreachable by every input path that exists, because the LOD placeholder takes no pointer events, is skipped by the marquee and skipped by Tab.
- Dropping a connector on a shape's body misses all four ports (the hit radius is 10 board units) and silently creates a floating end inside the shape that looks attached and tracks nothing. Standard ports and resize handles are concentric with the handle winning, so a port is unpressable at its own centre.
- An orthogonal edge from a top port leaves sideways, because the router ignores the side.
- Edges are invisible on the dark theme. So is the default black text, and the inline-edit outline. The property panel has no token layer at all and renders white in dark.
- There is no clipboard, no align, no distribute, no lock, no zoom-to-fit, no minimap, and a board authored away from the origin opens onto empty canvas on every load.
- The wheel is a fixed additive step, `@onwheel:preventDefault` is a silent no-op on the pinned runtime, and the blanket transform transition makes pan ease toward the pointer rather than track it.
- The context menu has no bounds check and gets clipped near an edge. Right-clicking a sticky note's body opens the browser's spellcheck menu.
- `ComponentContainer` has an edit mode that is a one-way door, a `Ctrl+Tab` binding the browser consumes before the page sees it, and a `PanStep` that is 200 screen pixels at 4x and five at 0.1x. Each of these sat behind a green test that called the handler directly rather than causing it.

A host developer embedding the library inherits all of this, and a component author has no way to opt into inline editing, declare an asset reference, or say whether a right-click inside their content belongs to them or the canvas.

## Solution

The canvas becomes a first-class direct-manipulation surface. Mouse and trackpad are first-class. Touch and pen are not built, and nothing here forecloses them.

Every press is classified once, in the browser, against what it hit, and exactly one of eight `Pointer gesture`s owns it until release. The left button selects and drags, the right and middle buttons pan, and a 4 screen-pixel `Drag threshold` separates a click from a drag at any zoom. What the gesture shows per frame is what its release commits, so snapping, guides and attached edges all track live and nothing jumps at release. Escape cancels a gesture back to its press-time geometry and selection, a window blur ends it, and the keyboard cannot write the board while a press is held.

On top of that spine: an additive marquee, press-to-select-and-drag, object snapping with full-width `Alignment guide`s, `Axis lock`, `Clone drag`, `Centre resize`, edges as full members of the `Selection`, an `Entered group` scope, Alt+click to reach a buried shape, and a `Hit stack` click through the selection box. Ports appear on selection and on the drop target, a shape's border is partitioned between connecting and resizing with the cursor as the only indicator, a drop on a body produces an `Auto endpoint` that keeps choosing its side, a click on a port is `Quick create`, and orthogonal edges route around the shapes they connect. The system clipboard carries board fragments, duplicate repeats the last offset, align and distribute are exact, a `Property bar` floats above the selection, a composed `Context menu` shows only rows that apply, the wheel adapts to mouse or trackpad, framing commands and a `Minimap` keep content findable, locking protects an entity without hiding it from the keyboard, the canvas starts every `Inline edit`, and a reconciled keyboard table gives every pointer capability a keyboard route.

Verification changes shape with it: gestures are driven directly over a fake context, the press-to-kind mapping is tested as a table, browser-owned plumbing is proven by `Interaction probe`s, a release-reliability theory over the closed set of eight is the standing obligation, and no test asserts a magnitude.

## User Stories

**Pressing and dragging**

1. As an end user, I want every press to be owned by exactly one behaviour until I release, so that a drag can never leak into a pan or marquee that keeps running after I let go.
2. As an end user, I want a release anywhere (outside the canvas, over another shape, past the clipped edge) to end the gesture and record it, so that Ctrl+Z after a long drag undoes the drag instead of deleting the shape.
3. As an end user, I want a quick click on empty canvas to do nothing more than clear the selection, so that the canvas is never left silently panning.
4. As an end user, I want a press to become a drag only after my hand moves about four screen pixels at any zoom, so that a shaky click is a click and a deliberate pull is a drag.
5. As an end user, I want a plain left-drag on empty canvas to draw a selection band and the right or middle button to pan, so that the two most common gestures never share a button.
6. As an end user, I want Shift+drag on empty canvas to add the band's contents to my selection, so that I can collect shapes across several sweeps and pans.
7. As an end user, I want pressing an unselected shape to select it at once and dragging to move it in the same motion, so that I never click first and drag second.
8. As an end user, I want pressing a shape that is already in my selection to leave the selection alone until I release, so that a drag moves the whole selection rather than collapsing it to one shape.
9. As an end user, I want Shift+click to toggle a shape in or out of the selection exactly as it does today, so that the new press rules change nothing I already rely on.
10. As an end user, I want a right-click to open the menu where I pressed and a right-drag pan never to wipe my selection as a side effect, so that navigating with the right button is safe.
11. As an end user, I want a drag that reaches the viewport edge to stop there rather than scroll the canvas under me, so that nothing moves that I did not move.
12. As an end user, I want the content under my pointer to stay under it while I wheel-zoom or pan mid-drag, so that I can reach a shape several screens away without releasing.
13. As an end user, I want a middle-button click below the threshold to do nothing, so that an accidental wheel press has no effect.

**What I see is what commits**

14. As an end user, I want attached edges to follow a shape while I drag or resize it, so that the diagram never shows a connector pointing at where a shape used to be.
15. As an end user, I want a shape to land exactly where the preview showed it, with snapping applied during the drag, so that content never jumps to the grid when I let go.
16. As an end user, I want a drag back to its starting point, or a release before the threshold, to leave no history entry, so that undo is never spent on a no-op.
17. As an end user, I want each pointer gesture to be exactly one undo step, so that one Ctrl+Z reverses one action.
18. As an end user, I want dragging one shape on a board with hundreds of edges to stay smooth, so that board size does not degrade a gesture.
19. As an end user, I want a shape I drag in from off-screen to appear and stay mounted until I release, so that a drag never shows a hole where the shape should be.

**Cancelling**

20. As an end user, I want Escape mid-drag to put the geometry and the selection back exactly as they were before I pressed, so that a wrong drag costs nothing.
21. As an end user, I want Escape to leave the viewport where it is, so that cancelling a pan simply stops the pan.
22. As an end user, I want a cancelled press to do nothing when I finally release, so that Escape on a port press cannot be defeated by the release creating a shape anyway.
23. As an end user, I want switching windows with Alt+Tab mid-drag to cancel the drag cleanly and leave the keyboard working when I return, so that a focus steal cannot strand a gesture.
24. As an end user, I want the keyboard unable to edit the board or the selection while I am holding a press, so that a stray Ctrl+Z or Delete mid-drag cannot corrupt the drag's undo entry.
25. As an end user, I want Escape to step back one level at a time (gesture, then port pick or placement, then additive traversal, then the entered group, then the selection), so that one Escape never throws away more than I meant.

**Snapping and modifiers**

26. As an end user, I want snap-to-grid on by default, so that content lines up out of the box.
27. As an end user, I want an optional object-snapping toggle that aligns the edges and centres of what I drag to nearby shapes and draws a full-width guide along the match, so that I can line shapes up by eye.
28. As an end user, I want object snapping to win on any axis where it fires and grid snapping to fill the other, so that the two helps cooperate instead of fighting.
29. As an end user, I want equal-spacing snapping when I drop a shape beside a row, so that the new shape lands at the row's own rhythm.
30. As an end user, I want holding Ctrl to suppress all snapping for as long as I hold it, taking effect without my moving the pointer, so that I can free a shape exactly when I want.
31. As an end user, I want holding Shift during a move to lock the motion to one axis, re-read as I go, so that I can slide a shape in a straight line and change my mind mid-drag.
32. As an end user, I want snapping to stand down while my pointer is moving fast, so that help arrives only when I am being careful.
33. As an end user, I want a move to round the selection's top-left to the grid and a resize to round only the edge I am moving, so that sizes change only when I resize.
34. As an end user, I want a keyboard resize under snap to step to the next grid line, so that an off-grid edge is repaired on the first press.

**Clone drag and centre resize**

35. As an end user, I want holding Alt while dragging to leave the originals in place and drag copies instead, so that duplicating a cluster into position is one gesture.
36. As an end user, I want Alt read live during the drag and the release to commit whatever I last saw, so that I can switch between moving and cloning mid-gesture.
37. As an end user, I want the copies to become the selection at release and to start a duplicate run, so that I can keep working on the new ones.
38. As an end user, I want holding Alt while resizing to keep the selection's centre fixed and mirror the opposite edge, so that I can grow a shape in place.

**Selection**

39. As an end user, I want to select edges alongside shapes in one multi-selection, so that I can delete, lock or move a connected cluster as one set.
40. As an end user, I want a marquee to take an edge when both of its ends are inside what the marquee selected, and never an edge that merely passes behind the cluster, so that deleting a marqueed cluster never deletes a connector I did not sweep.
41. As an end user, I want moving a mixed selection to translate only floating edge ends, so that an attached end keeps tracking its shape.
42. As an end user, I want commands that cannot act on an edge (align, group, resize, z-order) to skip the edges and act on the shapes, so that a mixed selection never refuses an action.
43. As an end user, I want Select All to select every top-level shape and every edge, so that select-all then Delete clears a board.
44. As an end user, I want to double-press a grouped shape to enter its group and then select, move, align and edit the members directly, so that I can work inside a group without ungrouping.
45. As an end user, I want Escape to step out of a group one level at a time and reselect the group I left, so that stepping out never loses my place.
46. As an end user, I want pressing outside an entered group to step out as far as needed, and pressing empty canvas inside its bounds to keep me inside, so that I can marquee within a group.
47. As an end user, I want a dashed outline around the group I have entered, so that I can tell which scope I am in.
48. As an end user, I want a click inside the multi-selection box to select the shape beneath it, so that a selection box never traps a shape under it.
49. As an end user, I want Alt+click to cycle down through shapes stacked under the pointer, wrapping at the bottom, so that I can reach a buried shape without rearranging.
50. As an end user, I want a group never left referencing deleted members, dissolving at one member and disappearing at none, so that groups never silently break.

**Ports and edges**

51. As an end user, I want ports to appear on a single selected shape and on the shape under my pointer during a connector drag, never on hover, so that the canvas is quiet at rest and the same rule works on touch.
52. As an end user, I want to start a connector by grabbing a side of a selected shape near its port, and to resize by grabbing nearer a corner, with the cursor telling me which I am about to do, so that ports and resize never fight for the same pixel.
53. As an end user, I want corner resize reachable at any zoom and for any port layout, so that two-axis resize can never be buried.
54. As an end user, I want dropping a connector anywhere on a shape's body to attach it so that it keeps choosing the side facing the other end, so that I never have to aim at a dot.
55. As an end user, I want dropping precisely on a port to pin the connector to that port, so that I can still choose a side deliberately.
56. As an end user, I want an auto-attached end to re-choose its side as either shape moves and never face away from what it connects, so that connectors stay sensible after rearranging.
57. As an end user, I want a plain click on a selected shape's port to create a connected duplicate beside it on that side and move selection and focus to the new shape, so that I can chain shapes rapidly.
58. As an end user, I want Ctrl+Arrow to do the same from the keyboard, so that chaining needs no mouse.
59. As an end user, I want a quick-created text shape to open for typing with its text selected, so that typing replaces the duplicated label.
60. As an end user, I want orthogonal edges to leave each end straight out from its side and route around both connected shapes, so that an edge from a top port never leaves sideways.
61. As an end user, I want curved edges to bend out along each side, so that curves look intentional.
62. As an end user, I want edge labels to sit halfway along the drawn path, so that labels stay on the line whatever the routing.
63. As an end user, I want edges readable on the dark theme by default, so that a board authored in light mode is not invisible in dark.
64. As an end user, I want to colour an edge and have a selected edge keep its colour, with selection shown as a halo under the stroke, so that recolouring from the bar is visible at once.
65. As an end user, I want edges to paint beneath every shape and the selection box above everything, so that send-to-back never hides a shape under its connectors.
66. As an end user, I want to add a custom port from the context menu at the pressed point or, from the keyboard, by sliding a provisional port along the border with the arrows and pressing Enter, so that custom ports are reachable both ways.
67. As an end user, I want to remove a custom port from the menu or with Delete while port picking, with any edge pinned to it falling back to auto-attachment on the same shape, so that removing a port never deletes or strands a connector.

**Clipboard, duplication and images**

68. As an end user, I want Ctrl+C, Ctrl+X and Ctrl+V to use the system clipboard, so that I can copy between boards and between browser tabs.
69. As an end user, I want pasting the text of a saved board file to merge that board in, so that sharing a board is as simple as sharing text.
70. As an end user, I want a paste to land where I last indicated (under the pointer, at the point I right-clicked, or the viewport centre) as a rigid body, so that pasted content keeps its layout and appears where I am looking.
71. As an end user, I want repeated pastes at the same spot to cascade, so that pasting five times gives five visible copies.
72. As an end user, I want Ctrl+D to duplicate without touching my clipboard, so that duplicating never overwrites something I copied elsewhere.
73. As an end user, I want a second Ctrl+D to repeat the offset I moved the first copy to, so that I can lay out a row by duplicating, nudging once, then duplicating again.
74. As an end user, I want a copied selection to carry the edges between its shapes and every edge I selected, with an end on an uncopied shape becoming floating, so that a lone selected edge still copies, cuts and duplicates.
75. As an end user, I want pasting plain text to create a text shape and pasting a bitmap to create an image sized to its pixels and bounded to half the viewport, so that foreign content drops straight onto the board.
76. As an end user, I want to give an image its picture from the property panel, from the context menu, or by dropping a file onto it, so that an empty image is never a dead end.
77. As an end user, I want a dropped image file on empty canvas to create a new image at the picture's own shape, so that photos arrive at a sensible size.
78. As an end user, I want image bytes stored once in the board and deduplicated by content, so that pasting the same picture ten times does not grow the file tenfold.
79. As an end user, I want a saved board to load when an image's bytes are missing, showing "Image unavailable" in place, so that one lost asset never loses the board.

**Align and distribute**

80. As an end user, I want six align actions and two distribute actions over my selection, so that I can tidy a layout exactly rather than by eye.
81. As an end user, I want a selected group aligned as one rigid body, so that aligning never scatters a group's members.
82. As an end user, I want distribute to equalise gaps, so that mixed-size shapes space evenly.
83. As an end user, I want align to respect snap-to-grid so that aligned edges land on grid lines, and a no-op align to add no undo entry.

**Context menu**

84. As an end user, I want the right-click menu to show only rows that apply, grouped in a stable order, so that I never see a greyed-out row.
85. As an end user, I want right-clicking empty canvas to offer Paste, Select All, Zoom to Fit, Zoom to 100%, Snap to Grid, object snapping and Unlock All, so that board and view settings have a home.
86. As an end user, I want the object menu to carry Cut, Copy, Paste, Duplicate, Delete, Group, Ungroup, an align strip, four z-order rows, Lock or Unlock, Zoom to Selection, and the port and image rows where they apply, so that every action on a selection is discoverable.
87. As an end user, I want the menu to open right and down from where I pressed and to flip to the other side of that point on whichever axis it would cross the container edge, so that no row is ever clipped off and the point I pressed stays clear.
88. As an end user, I want the menu to close on the next press anywhere inside the canvas container and never to swallow a press on the host's own buttons, so that dismissing is cheap and costs no click elsewhere.
89. As an end user, I want right-clicking a sticky note's body to open the object menu rather than the browser's spellcheck menu, so that the most common object on the board behaves like an object.
90. As an end user, I want right-clicking inside a text field I am editing, a link, or a video to open the browser's own menu, so that native content keeps its native menu.
91. As an end user, I want every menu row with a live chord to show it in my platform's convention, so that I learn the shortcuts as I use the menu.
92. As an end user, I want Shift+F10 or the ContextMenu key to open exactly one menu at my selection and return focus where it was when I close it, so that the menu is fully usable from the keyboard.

**Property bar and panel**

93. As an end user, I want a compact bar of glyphs floating above my selection for the properties I judge by eye (fill, stroke, stroke width, text colour, font size, weight, alignment, and an edge's routing, arrows and colour), so that the common edits are one click away.
94. As an end user, I want the bar to slide along the container edge rather than flip when there is no room above, so that it never jumps to the other side of the selection.
95. As an end user, I want the bar hidden while I drag and while a menu is open, so that it never sits in the way of a gesture.
96. As an end user, I want the selection chrome hidden while my pointer or focus is on the bar, so that I can see the colour I am changing.
97. As an end user, I want a selected group editable through its members in both the bar and the panel, so that selecting a group is not a dead end.
98. As an end user, I want a property whose targets disagree to show as mixed and stay editable, with one commit writing to every target, so that bulk edits work across differing shapes.
99. As an end user, I want Ctrl+Enter to move focus into the bar, so that the bar is reachable from the keyboard.
100. As an end user, I want a text or rectangle colour I never chose to follow the theme, with a clear control in the panel to return to the themed default, so that a board reads correctly in both themes without recolouring.
101. As an end user, I want the property panel to follow the dark theme, so that it is not a white box on a dark host.

**Viewport**

102. As an end user, I want a mouse wheel to zoom about the pointer and a trackpad swipe to pan, chosen from how the input arrives, so that both devices feel native.
103. As an end user, I want zoom steps multiplicative and smooth on a mouse and no easing lag when panning on a trackpad, so that the view tracks my hand.
104. As an end user, I want wheel zoom and pan kept out of undo, so that Ctrl+Z never undoes a pan.
105. As an end user, I want Zoom to Fit, Zoom to Selection and Zoom to 100% on Shift+1, Shift+2 and Shift+0, animated as one short flight, so that I can get my bearings in one keystroke.
106. As an end user, I want a board to open framed on its content rather than on empty canvas, so that a board authored away from the origin is visible on load.
107. As an end user, I want a minimap showing every shape as a box plus my viewport, clickable and draggable to pan, so that content I have panned away from stays findable.
108. As an end user, I want the library never to guess what chrome the host floated over the canvas, with the host able to compensate by panning, so that placement is predictable.

**Locking**

109. As an end user, I want to lock a shape or edge so that no command changes it and no primary press or marquee catches it, so that a background or template cannot be nudged by accident.
110. As an end user, I want a right-click on a locked entity to select it and offer Unlock, and an Unlock All row on the canvas menu, so that locking is never permanent.
111. As an end user, I want a locked entity to stay reachable by Tab and visible in the panel with its fields disabled, so that locking is never an accessibility failure.
112. As an end user, I want moving, resizing, aligning or deleting a partly locked group to act on its unlocked members and leave the locked ones exactly where they are, so that a locked member is never dragged along.
113. As an end user, I want to copy or duplicate a locked entity and have the copy locked, so that a locked template can be reused.

**Inline editing**

114. As an end user, I want a double-press on a text or sticky note to start editing it, and F2 to do the same from the keyboard, so that editing has both routes.
115. As an end user, I want a newly placed, quick-created or newly labelled text to open ready to type with its text selected, so that creating and naming is one motion.
116. As an end user, I want Escape in an editor to commit and return focus to the shape, so that I never lose typing to a reflex Escape.
117. As an end user, I want a text or edge label left empty to disappear, with an abandoned new one leaving no undo entry at all, so that the board collects no empty debris.
118. As an end user, I want an empty sticky note to stay, so that a blank marker is still a marker.

**Keyboard**

119. As a keyboard user, I want one reconciled shortcut table behind one focus guard, so that I can predict when a key reaches the canvas.
120. As a keyboard user, I want an arrow nudge under snap to move to the next grid line and Shift to move ten lines, so that nudging repairs an off-grid shape on the first press and stays screen-relative at any zoom.
121. As a keyboard user, I want arrow keys with nothing selected to pan by a screen-relative step, so that keyboard panning works at any zoom.
122. As a keyboard user, I want Space on a focused shape to add it to the selection and start a mode where Tab moves focus without selecting and Space toggles, so that I can build a multi-selection without a chord the browser steals.
123. As a keyboard user, I want a focused but unselected shape to show a dashed outline, so that I can see where focus is while building a selection.
124. As a keyboard user, I want Ctrl+Shift+Arrow to move focus to the nearest shape in that direction, so that crossing a dense board is not thirty Tabs.
125. As a keyboard user, I want every edge to be a Tab stop right after its source shape, announced as "Connector from A to B", so that connectors are reachable without a mouse.
126. As a keyboard user, I want Enter on a group to step inside it and Escape to step out, so that groups are navigable from the keyboard.
127. As a keyboard user, I want the minimap kept out of the tab order, so that a redundant view of state does not cost me a Tab stop.
128. As a keyboard user, I want Ctrl+Shift+L to lock and unlock, so that locking has a chord like every other selection action.
129. As a keyboard user, I want port picking to default to auto-attachment and cycle through auto, standard and custom ports with Space, so that I can connect without naming a side.
130. As a keyboard user, I want Ctrl+Tab gone, so that a chord the browser consumes cannot look like it works.

**Host developer**

131. As a host developer, I want a `WheelDeviceProfile` parameter, so that I can pin mouse or trackpad behaviour or persist the user's choice.
132. As a host developer, I want a bindable object-snapping toggle beside the existing snap-to-grid one, so that I can surface either in my own UI.
133. As a host developer, I want a `SelectedEdges` surface beside `SelectedComponents`, with the latter reading the expanded selection, so that my UI can react to any selection.
134. As a host developer, I want a standalone minimap component wired to the canvas by reference, so that I place it with my own CSS.
135. As a host developer, I want framing reachable through the public `ZoomPanTracker`, so that I can compensate for chrome I float over the canvas.
136. As a host developer, I want every new visual (edges, guides, board defaults, canvas frame, raised surfaces) themed through tokens on each component's own root, so that one override on an ancestor themes everything.
137. As a host developer, I want a board saved under the current schema to load unchanged after every change here, so that no board is ever orphaned by an upgrade.
138. As a host developer, I want paste warnings raised as an event, so that I can tell the user when a pasted component type was unknown.
139. As a host developer, I want `Board.AddAsset` to be the only way bytes enter a board, so that I can cap size or redirect storage at one point.

**Component author**

140. As a component author, I want to opt into inline editing by implementing `IInlineEditable.BeginEdit()`, so that the canvas can start an edit on a double-press, F2 or creation.
141. As a component author, I want to register an `IsEmpty` predicate, so that I decide whether an empty instance of my type should disappear.
142. As a component author, I want to declare a props property as an `[AssetReference]`, so that it may hold stored bytes and I still write an ordinary `src`.
143. As a component author, I want a marker that makes a plain region of my component count as interactive content, and a second marker that says whether a right-click there is mine or the browser's, so that I control both buttons without fighting the canvas.
144. As a component author, I want to tag a property with a `PropertyRole` and get a registration-time error if its kind or type does not match the role, so that a wrong tag fails at startup rather than on screen.
145. As a component author, I want my own component's colour fallback to work against the same public board tokens the built-ins use, so that themed defaults are not a built-in privilege.
146. As a component author, I want a `Custom` editor to learn that its targets are mixed through `CustomEditorContext.IsMixed`, so that my editor can show a mixed state.

**Verification**

147. As a library developer, I want one release-reliability case and one cancel case per member of the closed set of eight gestures, enumerated by the set, so that a ninth gesture fails the suite until its cases exist.
148. As a library developer, I want a `lostpointercapture` on a live gesture to fail the test run, so that "should never fire" is enforced rather than hoped.
149. As a library developer, I want constants asserted by their ordering and behaviour asserted by counts rather than clocks, so that tests catch a wrong number without pinning a right one.
150. As a library developer, I want an interaction probe for every claim about browser behaviour the model depends on, so that a green bUnit test can never again stand over a path the browser cannot run.

## Implementation Decisions

All decisions are locked in ADRs 0013 to 0067. Where an ADR was amended by a later one, the later ruling is what is stated here. Nothing in the `d12canvas-next` layer is contractual except where an ADR says it holds; ADRs 0001 to 0012 stay settled with the amendments noted.

### Press classification (ADRs 0017, 0036, 0047)

- A press resolves in JavaScript, on a `pointerdown` listener on the canvas element, by walking up from the event target to the nearest marked element. C# receives a `Hit target` of `(role, entityId, part)` plus press count, `pointerType`, buttons and modifiers. Blazor's event args carry no target, which is why classification cannot live in C#.
- Eleven roles, closed: `instance`, `resize-handle`, `port`, `port-strip`, `edge`, `edge-endpoint`, `edge-label`, `selection-bounds`, `selection-handle`, `author-content`, `canvas`. `canvas` means the walk found nothing.
- Five decisions are taken synchronously from the press, before the interop hop: `preventDefault`, `setPointerCapture` on a stable element the library controls, the single `Focus transfer`, the `Drag threshold`, and native-menu suppression. JavaScript holds no copy of selection state; it reads only what C# has rendered (`Locked`, non-addressable-in-group markers, context-menu markers).
- `author-content` is classified two ways and the two differ in their synchronous answers. Inferred from a natively interactive element (`input`, `textarea`, `button`, `select`, `a[href]`, `[contenteditable]`, `[tabindex]`), the browser keeps its own focus and text selection and nothing is prevented. Matched by the author's opt-in marker on a plain element, the press prevents and transfers focus like any other, because the target cannot take focus itself and the browser would otherwise focus the container and hard-select. `author-content` is `Native` only when its instance is addressable; inside an unentered group it classifies as `instance`.
- `Focus transfer` is one write per press, to the canvas element, which gains `tabindex="-1"` and is focused with `preventScroll`. Nothing ever moves focus because the selection changed. Focus moves on three occasions only: native traversal, the press transfer, and a `Command` that names its target (Quick create, Ctrl+G, keyboard placement, BeginEdit).
- `Hit region`s are real elements, sized against a `--d12-scale` custom property that `ContentStyle` publishes, so they stay constant in screen pixels. The visual is painted by a non-participant. Content is never dropped from hit, marquee or Tab; affordances leave render and hit together below a floor. The multi-selection box is a real hit element.
- The `Menu verdict` for a secondary press on `author-content` is taken once at `pointerdown` by five ordered rules (not addressable, then canvas; editable target or live selection, then browser; nearest `data-d12-context-menu` marker's value; `a[href]`, `video`, `audio`, then browser; otherwise canvas), stored, and consumed by that press's `contextmenu`. On Windows `contextmenu` fires after `pointerup` at the release target, which is why the verdict cannot be re-derived there. `<img>` is deliberately not inferred as browser-owned, because the built-in Image is a bare `<img>`.

### Pointer gesture arbitration (ADRs 0018, 0022, 0031, 0038, 0056, 0066)

- Eight `Pointer gesture`s, closed: `Pan`, `MarqueeSelect`, `MoveSelection`, `ResizeSelection`, `DragEdgeEnd`, `SelectEdge`, `Native`, `MinimapPan`. All owned by `DiagramCanvas`. `ComponentContainer` holds no gesture state and needs no JavaScript module of its own.
- Gestures are objects over an explicit context, not a switch. The kind is chosen in C# after the hop, because it needs the selection. Identity never changes mid-press; only the phase does: `pointing` until the threshold, `active` after, `cancelled` once Escape or an interruption ended its effects.
- The mapping from `(role, button)` to owner:

| Role | Primary | Secondary | Middle |
|---|---|---|---|
| `canvas` | `MarqueeSelect` | `Pan` | `Pan` |
| `instance`, `selection-bounds` | `MoveSelection` | `Pan` | `Pan` |
| `resize-handle`, `selection-handle` | `ResizeSelection` | `Pan` | `Pan` |
| `port`, `port-strip`, `edge-endpoint` | `DragEdgeEnd` | `Pan` | `Pan` |
| `edge`, `edge-label` | `SelectEdge` | `Pan` | `Pan` |
| `author-content` | `Native` (if addressable, else as `instance`) | by `Menu verdict`: browser gives `Native`, canvas gives `Pan` | `Pan` |

  `MinimapPan` is entered directly from the minimap root with `classify` off and has no role. The secondary and middle buttons ignore the role, so a later role cannot change what the pan buttons do. On macOS a Ctrl+primary press is a secondary press, behind a platform check.
- Release from `pointing` is the click outcome. `MarqueeSelect`: clear the selection (inside an entered group's bounds the scope is kept). `MoveSelection`: a non-member was collapsed to at press; a member or the selection box collapses to the pressed entity at release, where the pressed entity for `selection-bounds` is the top `Hit stack` entry beneath the box; Shift appends at press on a non-member and toggles at release on a member; Alt cycles down the `Hit stack` from the press-time `Selection snapshot`; a double-press enters a group or starts an inline edit. `DragEdgeEnd` on `port`/`port-strip`: `Quick create`. `SelectEdge`: select the edge (Shift toggles); a double-press adds a label. Secondary `Pan`: resolve the selection (preserve if inside it, select the pressed instance or edge, clear on empty canvas, select a locked entity) and open the `Context menu` at the press point. Middle `Pan`: nothing.
- `SelectEdge` has no active phase; crossing the threshold abandons it. `Native` takes no capture on the inferred branch.
- `Drag threshold` is 4 screen pixels, one number for every role and both buttons, no time component. JavaScript owns it and never calls C# below it, so any move C# receives is a real drag. The delta measures from the press point in board space, converted once at press, so `final delta = release − press` holds and whatever the pointer holds stays under it through a mid-gesture pan or zoom.
- Ownership is keyed by `pointerId` and the claiming button. Only that button's release ends the gesture; other buttons' down and up are dropped.
- Release channels: `pointerup` commits; `pointercancel` reverts; `lostpointercapture` on a live gesture reverts and writes `console.error`, because it should never fire. A window `blur` cancels and then ends the press (clears the gesture, releases capture), since the release happens in another application and never arrives. Escape and a `Board` swap cancel but hold the press until release.
- Cancel is three canvas-level steps and no gesture implements any of them: drop the `Gesture preview`, restore the `Selection snapshot` taken at press (both selection sets, before the press-time collapse), mark `cancelled`. The viewport is never restored. A cancelled gesture keeps capture until its claiming button comes up and that release does nothing. Escape is spent for the rest of the press.
- While any gesture owns the press, in any phase, nothing writes `Board` except that gesture's release and no keyboard command changes the selection. The guard sits on `CommandHistory` for board writes and on the two selection-only handlers. Escape, copy, snap toggle, focus moves and viewport commands stay live. A host replacing the `Board` reference mid-press cancels without restoring the snapshot.
- The viewport stays live in every phase. A viewport change re-runs the gesture tick at the last pointer position with zero velocity and promotes a `pointing` press to `active`. During `Pan`, an outside viewport change re-anchors the pan so the two add. A framing flight mid-press skips its pointer-events guard.
- Four public `[JSInvokable]` entry points (`OnPointerPressed`, `OnPointerMoved`, `OnPointerReleased`, `OnPointerCancelled`). Blazor forces them public; ADR 0018's "internal" means the gesture objects and their context. One reusable `addPointerListener` with `classify` on or off. All `@onmousedown`, `@onclick` and `@ondblclick` bindings on board content are deleted, and `_dragMoved`, `_isPanning`, `_isMoving`/`_isGroupMoving`, `_isResizing`/`_isGroupResizing` disappear rather than being ported.
- Edit mode on `ComponentContainer` is deleted entirely: `InitialEditMode`, `OnStateChanged`, `ComponentContainerStateChangedEventArgs`, the click-outside JS pair, the `edit-mode`/`view-mode` classes, and the demo page. A container has one rendering and `Selection` alone decides whether affordances appear. A container outside a `Board` is a positioned box with content and nothing more.

### Live geometry and the preview (ADRs 0020, 0037, 0042, 0048)

- The active gesture publishes a `Gesture preview` once per animation frame, coalesced in JavaScript: bounds overrides keyed by instance id, at most one pending edge line, moved floating endpoints, and a pending fragment (entities a clone drag will add at release). The entities it overrides are the gesture's participants.
- `Board` is never written mid-gesture. The commit writes the preview back verbatim, so a history entry records exactly what was on screen. Only entities whose previewed bounds differ from committed produce a command. No pointer gesture creates a command before release. `NudgeCommand`'s write-through-and-extend stays, because a keypress's result is fully determined when pressed.
- `Live geometry` is one read surface consulting the preview before committed state. Every derivation exists once with two named entry points: committed on `Board`, live on the reader. `instance.Bounds` always means committed. Windowing and `Content extent` read committed. Participants have a sticky mount for the gesture's duration, and a participant's LOD state is frozen at press (a late-mounting participant resolves once at mount).
- Snapping runs in the per-frame tick. A move rounds the top-left of the selection's bounding box and never its size; a resize rounds only the edges it moves, to the nearest line that respects the minimum size (50 by 50, raised for multi-selections); a multi-selection resize snaps only its bounding box and members scale proportionally inside it.
- An `Edge` becomes its own component with `ShouldRender` comparing resolved endpoints. The frame budget is structural: one frame per move, work proportional to participants and the edges touching them, never board size.

### Modifiers, snapping and guides (ADRs 0024, 0043, 0042, 0057)

- One rule: a modifier that chooses the gesture or the selection is read once, when it acts (Shift's append and toggle); a modifier that changes what the running gesture does is read live on every move (Shift axis lock, Ctrl suppress, Alt clone, Alt centre resize). Ctrl is never read at press because Ctrl+click is the macOS secondary click. A modifier change with the pointer still re-sends the last position as a move from a capture-phase window key listener, with zero velocity, through the frame coalescer.
- `SnapToGrid` defaults to on (one-word amendment to ADR 0011). `Object snapping` is a second bindable parameter pair, off by default. Both resolve per axis; object wins where it fires and grid fills the other. Ctrl suppresses both. Toggles are independent; no mutual exclusion.
- Object snapping: nine anchor pairings per axis over the selection box as a rigid body; candidates are the on-screen entities including locked ones and LOD placeholders, excluding edges, floating endpoints and groups; tolerance 8 screen pixels; sticky to 1.75 times tolerance; skipped above 3 screen pixels per millisecond; the search runs twice per move so the guide describes where the selection now is. `Equal-spacing snap` is moves only, with candidates filtered by perpendicular overlap before pair enumeration. Resize snaps point-wise for the edges actually moving. Placement and paste get grid only.
- `Alignment guide`s are full-bleed board-space lines in their own `Paint layer`, at half intensity, reading a new content-role token that is deliberately not the accent, drawn with `vector-effect="non-scaling-stroke"`.
- `Axis lock` on Shift during a move follows the press-anchored delta and is re-read every move; the locked axis is exempt from all snapping. Shift is unbound on resize.
- `Clone drag` is `MoveSelection` with Alt: the copies are exactly what duplicate would build, held in the preview's pending fragment from promotion, rendered with the selected look, added at release, then they become the selection and start a `Duplicate run`. Originals are snap candidates, copies are not. Nothing is built below the threshold.
- `Centre resize` is `ResizeSelection` with Alt: the centre of the instances-only box read at press stays fixed, the opposite edge mirrors, a toggle recomputes from the start box, the floor clamps symmetrically, and on each driven axis both edges are snap anchors with the smallest correction winning.

### Selection (ADRs 0037, 0044, 0046, 0053, 0055)

- `Selection` is two parallel sets, instances-and-groups and edges, because all three id kinds are bare GUIDs only `Board` can tell apart and a flat set would feed edge ids into group expansion. The host-facing surface gains `SelectedEdges`; `SelectedComponents` reads the expanded selection and loses its edge short-circuit.
- A command that cannot express an edge skips it: delete and lock take both kinds; align, distribute, z-order, group and resize read instances only; an edge widens no bounding box and counts toward no threshold. The property bar is empty on a mixed selection because the edge roles and author roles are disjoint.
- An edge joins a marquee by closure: every endpoint attached to a component the band selected or floating inside the band. No line geometry. Select All takes every edge. A move translates floating endpoints only, through a new `ChangeEdgeEndpointCommand`, which also makes endpoint repositioning undoable for the first time.
- `Entered group` is transient view state beside `Selection`, one level at a time, entered by a double-press on a member or Enter on the group's stop. While entered, the selection holds only direct members and content outside the scope does not respond to a primary press (an author control inside an unentered group goes quiet). Escape steps out one level and selects the group left; a press outside pops as far as needed; empty canvas inside the group's bounds keeps the scope. `GroupCommand` and `UngroupCommand` gain a parent-membership edit. Drawn as a dashed `--d12-muted-text` outline around the innermost entered group.
- `Hit stack`: `elementsFromPoint` at press, paint order, resolved through the effective selection id, without locked entities or the selection box, edges kept. Read at press for two click outcomes only and never for the role.
- A group's members always resolve. A delete edits every affected group's membership in the same history entry; a group left with one member dissolves (the survivor takes its place in the parent) and one left empty is removed, at every nesting level. Both load paths repair the same way and strict load does not throw. Ctrl+G is unavailable when every direct member of the entered group is selected. `Board`'s mutators stay plain.
- Four fixed `Paint layer`s inside the canvas content, each its own stacking context: edge band (lines, selected-edge halo, labels), instance layer (containers and placeholders by `ZIndex`, then host child content, plus every tab-stop proxy), guide layer, selection chrome (marquee, selection box and handles, floating endpoints, connector preview). `ZIndex` orders the inside of the instance layer only. Hit order and paint order agree everywhere. Edges have no z-order commands.

### Ports and edges (ADRs 0027, 0028, 0030, 0049, 0050, 0061, 0054, 0016, 0041)

- `IEdgeEndpoint` gains a fourth shape, `AutoPortEndpoint(ComponentId)`, carrying the id and nothing else. It resolves to the standard port on the side an aiming line (component centre to the other end's reference point) crosses; the one crossing-free case (other point inside the rect) takes the nearest side. Resolution is pairwise. Persistence uses the envelope's previously unreachable combination (component named, no port named), no new field.
- Three drop zones ordered so the easier gesture gives the more forgiving result: within a port's target pins; anywhere else over a component gives auto; nothing floats. The drop resolves in C#. The same-endpoint guard widens to same-component. Locked entities are not drop targets.
- Ports render on a single selected instance and on the one component under the pointer during a live connector drag, never on hover. Where a port is not rendered it is not hittable, so a drop there is auto.
- `Border partition`: per side, the run is the length in screen pixels minus a corner reserve at each end; each port takes the stretch nearest it capped at the port target; close ports split at the midpoint; resize takes what is left; anything below the floor drops from render and hit together; in a below-floor collision the standard port keeps its full width and the custom port is clipped. One number, `N` = 24 screen pixels, owned by C# and published through `ContentStyle`; floor `N/2`, corner reserve `N`, corner target `N`. `PortHitRadius` (10 board units) is deleted. Visuals (port dot 20px, corner handle 10px) clamp below 0.25x while targets do not. Side resize loses its drawn handle; the cursor is the only thing that draws the partition (`crosshair` on a port span, `ns-resize`/`ew-resize` on a resize span, corner cursors on corners, `move` on the body, none on author content). Invariant: no two affordance regions on a border intersect at any zoom.
- `Quick create` is `DragEdgeEnd` releasing from `pointing` on a port span. It builds a true duplicate (same type, props and size as the source) through the duplication path so every id regenerates, places it at the source's border on the pressed side plus a gap of 2 times `DominantGridSpacing()`, steps along the same axis while the slot is occupied, pins the edge at the source port and takes an `Auto endpoint` at the target, moves selection and focus to the new instance, and is one history entry. Ctrl+Arrow does the same for the four standard ports. The port span's double-click-to-add-port is retired. An `IInlineEditable` type then opens for editing with its text selected, panned into view first.
- Routing reads each end's side. `Orthogonal` leaves by a stub of 20 board units (equal to `GridBaseSpacing`) and takes the cheapest orthogonal path (length plus a per-bend penalty) clearing both connected shapes inflated by the stub, falling back to ignoring the shapes when no clear path exists. `Curved` places control points on each side's normal at `max(stub, 0.4 × distance)`. A floating end gets a pseudo-side facing the other end. Labels sit at the midpoint along the routed path. Routes are cached per edge on endpoints, sides and the two bounds, so a pan computes zero routes. The router is a pure function.
- Adding a custom port: the object menu's Add port here row, eligible when the press landed on a border span, pointer-only; from the keyboard it reads Add port… and enters `Port placement`, a hollow provisional port starting on the Top side at 0.25, rounded to the nearest grid line under snap as ADR 0050 says so the arrows step from a line, moved by the nudge step along the side, turning at corners, crossing to the opposite side when the arrow points inward, committed by Enter as one `AddCustomPortCommand`, cancelled by Escape or any focus move, with every other board-writing key a no-op while placing.
- Removing a custom port: a Remove port row on a custom port's span (replacing Add port here there), or Delete/Backspace while port picking highlights a custom port (a no-op on a standard port or the auto stage, which also removes the old hazard of deleting the instance mid-pick). Every end pinned to it becomes an `Auto endpoint` on the same instance. Unavailable on a locked instance or when any pinned edge is locked. One composite of a new `RemoveCustomPortCommand` (restoring at its index) plus one `ChangeEdgeEndpointCommand` per converted end. Standard ports cannot be removed.
- Every edge is a tab stop placed directly after the stop its source resolves to (a floating source sorts at its own point), carried by an invisible proxy element because the SVG precedes every stop in the DOM, labelled "Connector from {source} to {target}", with `aria-selected` and no role. No edge stops while a group is entered. Landing on one hard-selects it; Space toggles it.
- `Edge colour` is a nullable field on `EdgeStyle`; null resolves to `--d12-edge` at paint time via an inline rebind of `--d12-edge-override`. Arrowheads follow the stroke via `fill="context-stroke"`. Selection never repaints an edge; it adds a translucent accent halo under the edge's own stroke. One optional envelope field, no schema bump.

### Clipboard, duplication, assets and images (ADRs 0013, 0039, 0045, 0032, 0052, 0065)

- The system clipboard is the only clipboard. The payload is ADR 0004's board envelope verbatim as `text/plain`, recognised structurally, so a saved board's text pastes as a merge. Ctrl+C/X/V are DOM `copy`/`cut`/`paste` listeners, not keydown rows, because the `paste` event is the only permission-free read path; the menu rows use `navigator.clipboard.write`/`read` instead and are lost outside a secure context. Ctrl+D and Ctrl+A are keydown rows.
- A copy carries the selected instances closed over `Interior edge`s (both ends inside the set), every selected edge (an end on an uncopied instance becomes floating at its resolved position), groups recursively, and every referenced `Asset`. Every entity id regenerates across five references, `PortDef.Id` included; asset ids never regenerate. Paste drops an edge only when an attached end names an instance that did not materialise. `Locked` travels on every route and a copy of a locked entity is locked. Cut is eligible only when its delete would remove something and carries exactly that.
- `Paste anchor`: the pointer's board position when over the canvas, the stored press point for a menu paste, the viewport centre otherwise (including a keyboard-opened menu). Rigid-body translation measured against the extent (instances unioned with resolvable endpoints). Unchanged anchor cascades by +20,+20 board units; a changed anchor resets.
- `Duplicate run`: while the selection is exactly what the last duplicate produced, the next lands at that selection's committed offset from its source, top-left to top-left, replayed unsnapped. The first step is +20,+20. Any selection change ends it. Quick create does not start one; clone drag does. Transient canvas state.
- `Asset`: a content-addressed table on `Board` (`sha256-<hex>`, mime type, bytes), add-only in memory, collected at serialise time (including assets on an edge label's instance, which lives outside `board.Components`). `[AssetReference]` on a string `TProps` property declares it may hold `asset:<id>`; the canvas swaps that for a `data:` URI before binding props, cached by props reference identity. `Board.AddAsset(bytes, mimeType)` is the public entry, idempotent by hash, outside history. A missing asset is tolerated on both load paths and left unresolved. `Assets` defaults to null in the envelope, no schema bump.
- Image routes: the panel's `Url` gets a `Custom` editor with Choose file… and Remove image and no text field; the object menu gets the same two rows when every selected entity is an image; a `drop` listener fills an unlocked `Empty image` with the first file and creates new instances for the rest, or creates new instances anywhere else, cascading from the drop point, one history entry. An instance made from a file takes the picture's pixel size in board units, scaled down to fit half the visible viewport, never up. Filling keeps the box. Paste never fills an existing empty image. No size limit in the library; a host caps at `AddAsset`.

### Align, distribute and viewport (ADRs 0014, 0015, 0019, 0033, 0058)

- Eight public methods over two private implementations: align left, centre, right, top, middle, bottom; distribute horizontally, vertically. Computed against the unexpanded top-level selection's bounds (a group moves as a rigid body), thresholds 2 and 3 counted unexpanded, rows hidden below threshold, zero-delta entities contribute no command. Under snap, align rounds the target coordinate (a scalar snap, not `SnapBounds`) and distribute rounds the gap clamped to at least one step, pinning the first entity. A partly-locked group is measured by its unlocked members' box; a fully locked group does not count toward the thresholds. "Top-level" means the entered group's direct members when one is entered.
- Three viewport commands: fit, selection, 100% centre-preserving. No reset-view. `Framing` lives inside `ZoomPanTracker` so one `Changed` fires with scale and pan paired; contain, centred, inset to 0.9 of the container rect, never past 1.0; empty board and empty selection are strict no-ops. `Content extent` is instances unioned with resolvable edge endpoints, derived on demand. The state jumps and the CSS transition on the content element animates it over about 250ms; pointer events are suppressed for the flight keyed off the transition's lifecycle, never a timer. The canvas frames all content when a `Board` is first set, unanimated, no opt-out.
- The `Minimap` is host-placed chrome wired by reference: one plain box per instance, no edges, mapping the union of extent and viewport, its own `ZoomPanTracker`, boxes in board space under one transformed wrapper, `ShouldRender` keyed on board revision. Click jumps (animated), drag pans (not animated) through the arbitration listener with `classify` off. `aria-hidden`, no tab stop.
- `WheelDeviceProfile` (`Auto`, `Mouse`, `Trackpad`) decides plain-wheel meaning (zoom on mouse, pan on trackpad), ambient transform transition (100ms mouse, 0ms trackpad), and whether Shift binds to horizontal pan (mouse only). Alt pans both axes on both; Ctrl zooms on both; pinch arrives as Ctrl+wheel. Zoom is `scale *= exp(-deltaY / 600)` about the pointer. `Auto` classifies on delta granularity at `Wheel gesture` start (300ms idle boundary, `momentum` as early terminator) and holds for the run. The listener is a non-passive JavaScript `wheel` listener on the container, always `preventDefault`. The blanket transform transition ends. Wheel changes never enter history. The host owns any control and persistence.
- No viewport inset in any shape. Framing, click-to-add and the paste fallback read the full container rect; the host compensates for floated chrome through the public `ZoomPanTracker` (`SetPanPosition`, `Pan`, settable `Scale`). The initial fit's compensating pan is a visible jump and is accepted.

### Context menu, property bar and roles (ADRs 0023, 0021, 0040, 0041, 0026, 0067)

- One `Context menu` component handed a computed menu context, two content sets split on whether the press hit an entity, rows by per-item eligibility, unavailable rows hidden, separators only between rendered sections. Object menu order: Cut, Copy, Paste, Duplicate; Delete; Group, Ungroup; align strip then four flat z-order rows; Lock or Unlock; Zoom to Selection; plus Add port here / Add port… / Remove port, Choose image…, Remove image where eligible. Canvas menu: Paste; Select All; Zoom to Fit, Zoom to 100%; Snap to Grid (checked), object snapping; Unlock All. The align strip is one `role="group"` row of glyph `menuitem`s. The menu opens right and down from the press and, on each axis, flips to the other side of the press when it would cross the container edge; it clamps only if it fits on neither side, and shrinks to a container smaller than itself rather than being clipped. The dismissing press is consumed in the capture phase on `document`, only inside the canvas container.
- From the keyboard (Shift+F10, ContextMenu key) the content set splits on whether anything is selected, the menu draws at the selection's on-screen box or the viewport centre, the paste anchor is the viewport centre, and focus returns to the element focused at the keydown. The keydown writes the `Menu verdict` from a capture-phase window listener (any other key clears it, repeats do neither), prevents the keydown on a canvas verdict, and the browser's trailing `contextmenu` uses the verdict up without classifying its own target. The ContextMenu key acts on keydown.
- `Shortcut hint`s render beside rows whose binding is live, from one platform boolean cached at init: Apple concatenates `⌃⌥⇧⌘` symbols, everywhere else joins words with `+`. The snap row's hint follows `EnableSnapToGridShortcut`, whose guard moves from the public method to the keydown call site.
- `Property bar`: canvas-rendered chrome anchored by `bounds × Scale + Pan` with one measurement of its own width, centred above the selection's top, clamping and sliding along the container edge rather than flipping. Shows only role-tagged properties as glyphs with no text, 26px cells, colour glyph painted in its own value with a themed state for null and a hatched state for mixed. Reads the expanded selection; across types shows the role intersection. Hides for any pointer gesture and while a menu is open. Keyboard-reachable on Ctrl+Enter with roving arrows. Declares raised token values and `color-scheme` on its own root. While it has hover or focus-within, one CSS rule hides all selection chrome, outline and edge halo included, with no timer or C# state.
- `PropertyRole` replaces `SharedTag`: a closed enum (`Fill`, `Stroke`, `StrokeWidth`, `TextColour`, `FontSize`, `FontWeight`, `TextAlign`, plus `EdgeRouting`, `EdgeSourceArrow`, `EdgeTargetArrow`, `EdgeColour`), each owning a glyph and declaring the `EditorKind` and CLR type it expects, validated at registration. A role is both what admits a property to the bar and what merges it across types. Bar rows are produced through one row seam (id, role, kind, options, value, `IsMixed`, commit callback) by two producers: instance rows commit through the props batch, edge rows through `ChangeEdgeStyleCommand`.
- `Mixed`: decided per row, null counts as a value, colours compare case-insensitively; a commit writes to every target in one entry and skips those already holding the value. One display rule per `EditorKind` shared by bar and panel (hatched swatch, "Mixed" placeholder, no active option with a disabled "Mixed", indeterminate checkbox). `CustomEditorContext` gains `IsMixed`.

### Theming (ADRs 0016, 0034, 0063, 0064)

- The token boundary is who renders the pixels, not chrome versus content. New tokens: `--d12-edge` (first content-role token), the guide colour, `--d12-board-text`, `--d12-board-fill`, `--d12-board-stroke` (light values byte-identical to the old literals), `--d12-inline-edit-outline`, `--d12-canvas-frame` (escape hatch for the container border, since the canvas's `--d12-border` is the grid-line colour). `--d12-shadow` gains a dark value on the menu. The port fill reads `--d12-connector-preview`. Selection chrome reads `--d12-accent`, which moves two near-miss blues and breaks the byte-identical-light convention for about 26 baselines because nobody chose those blues. The orange and white on ports and floating endpoints stay fixed by decision.
- `Themed default`: `TextProps.Color`, `RectangleProps.FillColor` and `RectangleProps.StrokeColor` become nullable, default null, resolved from a board token in the component's own CSS via an override custom property emitted only when non-null. `StickyNote`'s yellow and black stay. The panel's colour editor gains a clear control that writes null. No migration; existing literals stay as authored.
- Two value sets: board values on the canvas, raised values plus `color-scheme` on every surface floating over the board (`Palette`, context menu, `PropertyPanel`, minimap, property bar). `PropertyPanel` joins the token layer with `Palette`'s four blocks and a text colour; inputs read the surface and inherit text. A guard test asserts every colour literal in the canvas and container style blocks is a token declaration or on the fixed list.

### Locking (ADRs 0017, 0022, 0058, 0065)

- `Locked` is a persisted, undoable bool on `ComponentInstance` and `Edge`, absent by default, one optional envelope field. A group is locked when every resolving member is. `ChangeLockedCommand` widens the command set; Unlock All is a composite of it.
- A locked entity takes no part in primary-press hit-testing, marquee or any command, and stays tab-reachable with its panel fields disabled; when the selection also holds unlocked entities the fields stay live and an edit writes only the unlocked ones, the partly-locked rule below. A secondary press reaches and selects it. An operation on a partly-locked group acts on its unlocked members: move and nudge apply one delta to unlocked leaves; resize keeps handles on the group's real bounds and scales unlocked members inside; align and distribute measure the unlocked box; delete removes only unlocked members and the group repair follows; clone drag copies the whole group. The Lock row reads Unlock when every top-level selected entity is locked.

### Inline editing (ADRs 0035, 0051, 0062)

- ADR 0001 reopens for one seam: `IInlineEditable` with a single parameterless `BeginEdit()`. Registration records whether the component type implements it at composition time. The canvas starts every edit: a double-press on an addressable instance, F2 on a focused one, a new instance from `Quick create`, palette placement (click, Enter, Space or drop), or a new edge label. Copies never open for editing. `BeginEdit` selects all text. If the instance is not fully in view the canvas pans the minimum distance in, pan only. Below the LOD cutoff the request is dropped. The built-ins' `@ondblclick` goes. `ParentCanvas` and `InstanceId` cascading values become part of the contract.
- Escape in an editor commits (reversing the old discard) and returns focus to the instance's tab stop, or the edge's stop for a label. The editor makes one `CommitInlineEdit(InstanceId, before, after, returnFocus)` call per edit end on every route, replacing `EndInlineEdit` and the editor's use of `CommitPropsChange`. Creation and edit are two history entries.
- An edit that ends empty (whitespace counts) removes the instance when its type registers an `IsEmpty` predicate; `Text` does, so every edge label does; `StickyNote` does not. If the creation is still the top history entry it is retracted through a new `CommandHistory.Retract`, leaving no history; otherwise the removal is one composite with the group repairs. Focus then goes to the quick-create source, the edge's stop for a label, or the canvas.

### Keyboard (ADRs 0026, 0059, 0060, 0054, 0050, 0041, 0056, 0063 probes)

- One table behind one early-return guard on the window listener: pass when `activeElement` is inside the canvas container, or when nothing is focused and no text selection lives outside it. `isEditableTarget` stays per row. Every binding accepts `metaKey` with `ctrlKey` and matches on `event.code`. Every row that writes `Board` or the selection also requires that no pointer gesture owns the press.
- The table after all addenda: Delete/Backspace deletes the selection, or removes the highlighted custom port during port picking. Escape stages: cancel the gesture, end port picking or placement, end `Additive traversal`, step out of the `Entered group`, clear the selection. Arrow nudges, or pans by `PanStep / scale` (50 board units) on an empty selection. Shift+Arrow nudges ten steps. Alt+Arrow and Alt+Shift+Arrow resize a single instance with the anchor fixed or flipped. Ctrl+Arrow quick-creates. Ctrl+Shift+Arrow is `Directional focus`. Space adds the focused stop and starts `Additive traversal`, or toggles inside it, or cycles ports while picking. Enter commits a port attachment on an instance stop or enters a group on a group stop. F2 begins an inline edit. Inside `Port placement` only Arrow, Shift+Arrow, Enter and Escape act. PageUp/PageDown zoom. Ctrl+Z and Ctrl+Shift+Z. Ctrl+G and Ctrl+Shift+G. Ctrl+] and Ctrl+Shift+], Ctrl+[ and Ctrl+Shift+[. Ctrl+Shift+L locks or unlocks. Ctrl+' toggles snap-to-grid (host-disableable). Ctrl+D duplicates. Ctrl+A selects all. Shift+1, Shift+2, Shift+0 frame. Shift+F10 and ContextMenu open the menu. Ctrl+Enter focuses the property bar. Ctrl+C/X/V ride the DOM clipboard events. Ctrl+Tab is removed. Align, distribute and Unlock All have no chord.
- `Nudge` under snap moves to the next dominant grid line in that direction (a third snap primitive, a directional ceiling and floor), so the step stays between 6.3 and 63 screen pixels; Shift moves ten, which is one cell of the next coarser layer. Nudge never object-snaps. The nudge anchor is the selection box's top-left.
- `Additive traversal`: Space on a focused stop adds it and starts the mode; inside it Tab and Shift+Tab move focus only and Space toggles; ended by Escape (selection kept), a pointer press, focus leaving the container, Enter on a group stop, or a command handoff. A focused unselected stop draws a dashed `--d12-accent` outline via `:focus-visible:not([aria-selected="true"])`. No other indicator and no live region.
- `Directional focus` on Ctrl+Shift+Arrow: candidates are the current ring (viewport plus overscan, or the entered group's members), instance and group stops only, never edges; same row or column first by gap, else smallest gap plus perpendicular offset; ties to reading order; no wrap, no pan; origin is the focused stop (for a focused edge, the stop its source resolves to, or its floating source point, since an edge's own box can sit in empty space far from both shapes), else the selection's box, else the viewport centre; a no-op during a gesture, placement or picking. Firefox needs `preventDefault` even on a no-op row.
- Measured facts carried into the build: Ctrl+Tab never reaches the page in Chrome or Edge on Windows; Ctrl+Arrow and Ctrl+Shift+Arrow reach the page in Chrome, Edge and Firefox on Windows and `preventDefault` takes; macOS is assumed to match and unmeasured, with the first Mac report as the trigger to reopen the bindings; a prevented Shift+F10 stops `contextmenu` in Chromium only and the ContextMenu key fires it on keyup in both engines; window `blur` fires on Alt+Tab and tab switch with the button held, and `lostpointercapture` fires late.

### Persistence, commands and the acceptance surface

- No `SchemaVersion` bump anywhere. The gate is strict equality, so a bump would refuse every saved board. Every addition is an optional envelope field defaulting to absent: `Locked`, `EdgeStyle.Color`, `Assets`, and the auto endpoint's existing-combination encoding. Group membership is repaired on both load paths.
- The command set grows by three (`ChangeLockedCommand`, `ChangeEdgeEndpointCommand`, `RemoveCustomPortCommand`) and `CommandHistory` gains `Retract` and the press-ownership gate. Every other new behaviour composes existing primitives. `CONTEXT.md`'s command list is corrected to what exists.
- Public surface removed or changed: `InitialEditMode`, `OnStateChanged`, `ComponentContainerStateChangedEventArgs` and `Ctrl+Tab` go; `SharedTag` becomes `PropertyRole`; `SelectedComponents` reads the expanded selection; three built-in colour props become nullable; `EdgeStyle` gains `Color`; `CustomEditorContext` gains `IsMixed`; `OnToggleSnapToGridPressed` is ungated. There is no published package, so none of this is a break for anyone.
- Public surface added: `WheelDeviceProfile`, an object-snapping parameter pair, `SelectedEdges`, eight align/distribute methods, three viewport command methods, a paste-warnings event, `Board.AddAsset`, framing on `ZoomPanTracker`, the `Minimap` component, `IInlineEditable`, an `IsEmpty` registration option, `[AssetReference]`, the author-content opt-in marker, `data-d12-context-menu`, and the new tokens.
- `D12Canvas.App`'s `BoardEditor` is the acceptance surface: full-bleed canvas, palette floating top-left, `PropertyPanel` top-right while something is selected, minimap bottom-right always, the wheel device profile behind a settings control bottom-left. Palette and panel are already folded in; the minimap and the settings control wait for their library pieces. `D12Canvas.Demo` gains pages for every new rendered state so the visual suite can cover them (property bar, guides, entered group, dark edges and inline editor, image placeholder, minimap, themed panel).

### Constants, and the relationships that defend them

Screen-pixel family, asserted by ordering and never by value: drag threshold 4 < object-snap tolerance 8 < affordance floor 12 < edge hit band 20 < port target 24. Also: snap sticky 1.75 times tolerance; velocity cut-off 3 px/ms; guide intensity 0.5; wheel K 600; ambient transition 100ms mouse, 0ms trackpad; wheel idle 300ms; framing 250ms, margin 0.9, cap 1.0; routing stub 20 board units equal to `GridBaseSpacing`; quick-create gap 2 times `DominantGridSpacing()`; paste cascade and first duplicate +20,+20 board units; `PanStep` 50 board units divided by scale; LOD threshold 32 (content never dropped); `Overscan` 200; minimum size 50 by 50; port dot 20px and corner handle 10px, clamped below 0.25x; property bar cell 26px; dropped image fit to half the viewport. The nudge step cannot join the screen-pixel ordering because it straddles it. Roughly ten of these are judged rather than derived and nothing in the repo judges them; see Testing.

## Testing Decisions

A good test asserts what a user, host or component author can observe at a public or deliberately exposed seam, never how the library got there. For interaction that means three more things, each the rule drawn once from a different direction (ADR 0025): probes prove plumbing and never magnitudes (synthetic input does not reproduce device granularity, and the green `deltaY: 120` assertion over a real 100px notch is the cautionary case); constants are asserted by their relationships and never their values; performance is asserted by counts and never by clocks.

Five seams, two of them new, confirmed with the dev for this spec:

1. **Pure C#** (existing, highest). `Board`, the commands and `CommandHistory` (including `Retract` and the press gate), the serializer (optional fields, group repair, assets, auto endpoints), `ZoomPanTracker` framing, and the new geometry: auto-endpoint resolution, the border partition, scalar and directional snap primitives, the router, equal-spacing candidates, duplicate-run offsets, content extent. Most new logic lands here. Prior art: `BoardTests`, `HistoryTests`, the command tests, `BoardJsonSerializerTests`.
2. **Gesture objects over a fake context** (new, assembly-internal via `InternalsVisibleTo` on `D12Canvas.Tests`). A gesture is constructed, given moves and a release, and asked what it published and what it committed. No renderer. `InternalsVisibleTo` is bought for exactly one thing, reading the `Gesture preview` as data; the gesture types stay internal and no public observation surface exists.
3. **bUnit** (existing). The press-to-kind mapping tested as a table over roles, buttons, modifiers, `pointerType` and selection state through the four public pointer entry points; the keyboard table; markup and ARIA (tab stops, `aria-selected`, edge labels, `aria-hidden` minimap); render counts (five hundred edges, drag one instance, assert the re-render count tracks the edges it touches); token declarations and the colour-literal guard; bar and panel rows including mixed and themed states. Prior art: `DiagramCanvasDragMoveTests`, `DiagramCanvasMarqueeSelectTests`, `DiagramCanvasThemeTokensTests`, `PropertyPanelTests`, `ComponentTestBase`.
4. **Interaction probes** (new class of test inside `D12Canvas.VisualTests`, not a third project). Browser-owned plumbing only: the classification walk, press count, the threshold and the decision not to call C# below it, the five synchronous decisions, frame coalescing (ten moves in one frame arrive as one call), ownership by pointer and button, the release channels, modifier re-send, `preventDefault` on `pointerdown` suppressing focus, a marked non-focusable press leaving a multi-selection intact, user activation surviving the interop hop, the real key paths for Shift+F10, Add port…, F2 and the ContextMenu key, a wheel with a button held. Prior art: the wheel probe on `prototype/wheel-pan-zoom`, the clipboard route probe on `prototype/clipboard-menu-route`, the leak harness on `research/gesture-leak-probe`, and ticket 54's and 64's probe assets under `.scratch/canvas-interaction-quality/assets/`.
5. **Playwright visual** (existing). One baseline per new rendered state, in the pinned Docker image with `-parallel none`, reduced motion applied suite-wide (such a rule may only zero durations) with one case deliberately running both paths to assert the framing flight's pointer suppression is applied during and cleared after.

The standing obligation is a `[Theory]` over the closed set of eight: one `Release-reliability case` per member (start the gesture, end it the way a user plausibly might, move the pointer with no button held, assert nothing responded) and one cancel case per member, enumerated by the set so a ninth gesture fails the suite until its cases exist. The approach is blind to `SelectEdge` and `Native`, which hold no state and cannot leak. `lostpointercapture` on a live gesture writes `console.error` and the fixture fails any test that logs one. `docs/agents/testing.md` gains one line pointing at that test.

Reattachment is most of the work: 39 of the 86 bUnit files dispatch pointer events at bindings ADR 0018 deletes, across 573 call sites, so they stop reaching the code rather than failing. The map found four green tests standing over paths the browser could not run (ticket 04's drag tests releasing inside their start element, `DiagramCanvasCtrlTabSpaceMultiSelectTests` invoking the handler directly, `ComponentContainer_ClickOutside_ExitsEditMode` calling an unregistered listener's callback, and the `preventDefault`-suppresses-focus assumption). The tell is in the test body: it calls the handler rather than causing it. Any such test is rewritten at seam 2, 3 or 4, never kept.

Expected baseline churn, planned rather than discovered: the initial fit changes the opening view of every board-mounting baseline; the deleted demo nav row touches all current `.verified.html` and `.png` files; the paint-layer wrappers and the canvas `tabindex` move every `.verified.html`; the accent sweep moves about 26 light baselines; the panel's input text colour moves four. There is no dark baseline today showing an edge, an inline editor or the image placeholder, so the dark fixes are unverified until those pages exist.

There is no manual acceptance pass, decided against a proposal for one. Roughly ten tuned numbers ship defended only by staying ordered against their neighbours, and a 60-to-30fps regression at identical render counts is invisible to every layer here. CI currently runs the visual job without `-parallel none` and must be fixed before any of this gates on it.

## Out of Scope

- Rotation, and any change giving `Bounds` an angle. Aspect-lock and proportional multi-resize go out with it (which is also why Shift stays unbound on resize).
- Touch and pen as built, tested features. The arbitration model keeps them cheap (`pointerId`, `pointerType`, a touch value of `N` declared but not set, the `canvas` row inverting) and nothing more.
- Real-time collaboration, presence and facilitation affordances.
- Containers with real parenting, named layers, and auto-layout of a connected structure. All three reopen ADR 0003's flat board or ADR 0008's `ZIndex` arithmetic, both settled.
- Changing the rendering strategy for ephemeral chrome. D12Canvas is a Blazor DOM/CSS canvas by definition.
- Edge auto-scroll (ADR 0029). Declined because a clamped cursor delivers no moves and the model has no clock. Revisit trigger: a host complaint, or anything else needing a per-frame heartbeat, which ADR 0056 has made the only remaining cost.
- A viewport inset or occlusion API (ADR 0033), a reset-view command, a zoom-control cluster, a host-configurable framing margin, an initial-fit opt-out (judged harmful).
- A `SchemaVersion` bump or a migration pipeline. Refused four times; the gate would need redesigning first.
- A host-implemented asset store, asset garbage collection beyond serialise time, and any library size limit on assets or images.
- A manual acceptance pass, a wall-clock performance canary, a third test project, a public gesture observation surface.
- Figma's Select layer submenu, self-loop edges, interior custom ports, a type picker on quick create, Miro's connect-instead-of-create.
- Everything the map still holds as fog, carried forward as open rather than decided here: the cursor and micro-feedback vocabulary (including a lock badge, the silent cancel and blocked-key states, the pin-versus-auto drop cue, Alt-over-a-stack discoverability, and `cursor: move` on a locked or board-less container); announcing keyboard mode state through a live region (port picking, placement, entered group, additive traversal); bidirectional numeric fields; relative arithmetic in a mixed number field; what a `Board` swap does to the selection and to history; the first duplicate step at far zoom; input capture for a canvas embedded in a scrolling host page.
- WebKit and macOS measurement. Chords and blur delivery were measured on Windows in Chrome, Edge and Firefox. macOS ships assumed.

## Further Notes

- **Build order.** The map's blocking graph suggests one, offered as a suggestion rather than a mandate. The spine first: classification, arbitration, button semantics, focus transfer, live geometry, cancellation and press ownership (ADRs 0017, 0018, 0020, 0022, 0031, 0035, 0036, 0038, 0066) together with the test reattachment they force (ADR 0025), because nothing else can be verified until the suite can reach the code again. Then selection widening and layering (0037, 0044, 0046, 0053, 0055). Then modifiers and snapping (0024, 0043, 0048, 0042, 0057). Then ports and edges (0027, 0028, 0030, 0049, 0050, 0061, 0054). Then clipboard, duplication, assets and images (0013, 0039, 0045, 0032, 0052, 0065). Then chrome (0023, 0047, 0067, 0021, 0040, 0041, 0014, 0058). Then viewport (0019, 0015, 0033, 0056). Keyboard (0026, 0059, 0060) and inline editing (0051, 0062) cut across and should land with the features they bind. Theming (0016, 0034, 0063, 0064) can go almost anywhere; the colour-literal guard ships with whichever of 0016, 0041 and 0063 lands last.
- **Live defects to close on the way, not separately.** Endpoint repositioning has never entered history. `PanStep` is undivided by scale. `EnableSnapToGridShortcut` guards the wrong thing. `LodSizeThreshold = 32` makes a default sticky note unreachable at 0.16x. The inline-edit outline vanishes on dark. `MenuStyle` has no bounds check. `PortHitRadius` is in board units. `<marker>` content does not inherit the referencing stroke. The ambient transform transition makes pan ease. `.canvas-content` still declares a vestigial 3000 by 3000. Each is named in the ADR that owns its area.
- **Prototypes and probes that exist.** `prototype/property-bar` (`a038c4c`), `prototype/snap-guides` (`5c68479`), `prototype/port-affordance` (`cec3e84`), `prototype/edge-routing`, `prototype/chrome-layout`, `prototype/wheel-pan-zoom`, `prototype/clipboard-menu-route`, `prototype/edge-autoscroll` (`25ed6d7`), `research/gesture-leak-probe`, and the hand-run probe pages under `.scratch/canvas-interaction-quality/assets/` and `probes/`. They are evidence and starting material, not code to merge.
- **Reference tools.** tldraw (pinned `ef6e81c`) is a design reference only; its licence forbids production use without a commercial key and nothing may be lifted. Excalidraw (`4872083c`) is MIT. Both move fast on exactly these subjects, so re-verify a citation rather than assume it.
- **Reading the decisions.** The map records fifteen variants of one failure family, all about a decision's text reaching further or less far than its argument. The cheap checks worth carrying into every implementation ticket: read the ADR, not its summary or its name; read a ticket's constraints and motivations as claims, and grep whether shipped code already contradicts them; when one ADR states a fact about another's subject, read it in the deciding ADR; when an ADR says it reuses a definition unchanged, diff the wording; when a decision changes a fact, grep the corpus for the fact rather than the ADR number; and ask what a changed entity type usually sits on or under, since the model cannot see a composition.
- **Vocabulary.** This spec follows `CONTEXT.md`. Implementation tickets should keep to it: `Pointer gesture` for what owns a press and `Gesture` for the history unit; `Hit target` and `Hit region`, never hit box or pick; `edge`, never connector or link, except in the edge tab stop's accessible label where "Connector" was chosen for users; `Context menu`, never right-click menu; `Entered group`, never isolation mode; `Additive traversal`, never multi-select mode; `Quick create`; `Auto endpoint`, never floating; `Themed default`; `Menu verdict`.
