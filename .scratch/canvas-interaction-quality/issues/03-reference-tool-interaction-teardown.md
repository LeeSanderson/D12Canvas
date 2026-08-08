# Interaction teardown of reference tools

Type: research
Status: resolved

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

## Answer

Four tools read against their own sources — tldraw pinned at `ef6e81c` and Excalidraw at `4872083c`, Miro and FigJam against their help centres and developer docs with every undocumented behaviour marked `[observed]` rather than asserted. Organised by gesture across all four, so each decision ticket compares four answers to one question rather than reading four profiles.

**The reference bar, in one paragraph.** A press is classified exactly once, at press, against a hit target — tldraw's select tool registers eighteen child states and `Idle.onPointerDown` routes into precisely one of them, re-dispatching rather than branching inline, with camera panning intercepted *above* the state chart so it never competes for arbitration at all. All four separate "pointing at X" from "doing X" with a movement threshold measured in **screen** pixels (tldraw 4px mouse / 6px touch / 25px on its own toolbar; Excalidraw 10px), and that threshold is what lets one press safely mean both "select" and "begin a drag": a press outside the selection selects now, a press inside it defers to release so a drag moves the whole set. Left-drag on empty canvas marquees in all four; panning is right-drag, middle-drag or held space. Hit targets are sized in screen pixels by dividing by zoom, decoupled from their visuals, varied by pointer type (Excalidraw's `{ mouse: 8, pen: 16, touch: 28 }`), and *dropped from the set entirely* rather than drawn below a usable floor. Selection chrome disappears for the duration of a gesture and returns on release. Snapping is 8 screen pixels in both open-source tools independently, with a modifier that **inverts** rather than suppresses, and gap/equal-spacing snaps are table stakes.

**Where the settled decisions hold up.** ADR 0006's marquee-intersection and shift-click-toggle are confirmed verbatim (Miro additionally ships containment as an opt-in long-press, so rejecting containment-by-default was right). ADR 0008's new-instance-on-top and its refusal of a `Group` z-slot are universal. ADR 0010's reading-order tab stops match Miro's documented rule exactly, and group-collapses-to-one-stop matches FigJam's sibling rule. ADR 0009's one-shot placement tools are Miro's rule word for word. Most valuably, **ADR 0007's one-history-entry-per-gesture is confirmed structurally by tldraw**, which achieves it with history marks alone and has no `ephemeral`/`squashing` flag on its update API at all — live geometry does not need a second write path, only marks in the right places. That materially de-risks ticket 05's hardest constraint.

**Where they look wrong.** ADR 0005's discrete named ports are the largest divergence: **none of the four exposes ports as the primary model.** All four bind to a normalized anchor over the target's bounds and expose named sides only as sugar over it — Miro's `snapTo`/`position` are "always in sync, and mutually exclusive"; Figma's `ConnectorEndpoint` is a three-variant union with a seven-value magnet including `AUTO` and `NONE`; tldraw's `normalizedAnchor` defaults to the shape centre with `isPrecise: false`; Excalidraw has just replaced `focus`/`gap` with a `fixedPoint` normalized anchor whose draggable handle **belongs to the arrow endpoint, not the shape**. The ADR rejected the wrong thing — the reference anchors are both named *and* dynamic, because "auto" is a name — and `EdgeEndpoint` cannot currently express the case that matters most: attach to a component and let routing pick. Relatedly, D12Canvas can express `Straight` + `PortId.Top`, which all three tools that model both consider ill-formed, because four sides are a property of orthogonal routing rather than of shapes.

ADR 0009's "Select is the only persistent state" is too strong: it forbids a hand tool, a held space-pan quasimode and an opt-in tool lock, all of which every reference tool ships — and tldraw, with a 28-tool toolbar, still defaults to snapping back to select while refusing lockability for exactly the navigation tools. The reasoning survives; the rule needs restating as held-vs-entered-vs-one-shot. ADR 0009's "right-click on empty canvas opens no menu — there is no decided action that would belong there" is now false in both directions: actions do exist (ADR 0011's snap-to-grid toggle, ticket 13's viewport commands, unlock-all) and Miro documents exactly those as its empty-canvas menu.

**The handle/port collision is real and exact, and no reference tool has the problem** — because none of them creates it. Miro's resize nodes are white and on the corners only, connection dots blue and on the sides; tldraw and Excalidraw have no connection points at all; Figma stacks rotation on the space *outside* the box. D12Canvas puts a 10px resize handle concentric with a 20px port at all four border midpoints, with a 14px port strip underneath — three gestures separated only by markup order and click count, where no reference tool stacks more than two.

**Directly answers ticket 17's open wheel question (§1.5):** three of four default plain wheel to *pan* with Ctrl/Cmd inverting to zoom (tldraw's `wheelBehavior: 'pan'`, FigJam, Excalidraw); Miro splits it by an explicit Mouse/Trackpad preference. Every one of them zooms multiplicatively where D12Canvas steps linearly by ±0.1, and every one anchors on the pointer.

Two things to carry forward before anyone reaches for the source: **tldraw is not MIT-licensed** — its `LICENSE.md` requires a commercial key and forbids production use without one, so it is a design reference only, never code to lift. And the doc closes with an appendix of claims that turned out **wrong or superseded** — several widely-repeated "facts" about these tools no longer hold — so no later ticket cites them as established.

Surfaced four new tickets: [Hit-test participation and hit-region geometry](issues/18-hit-test-participation.md), [Edge attachment without a named port](issues/19-edge-attachment-without-named-port.md), [Drag past the viewport edge](issues/20-drag-past-viewport-edge.md), [Create-adjacent-and-connect](issues/21-create-adjacent-and-connect.md).

Full teardown, 893 lines with sources marked `[docs]`/`[source]`/`[observed]`: branch `research/reference-tool-teardown`, file `.scratch/canvas-interaction-quality/research/reference-tool-teardown.md`, commit `7fd2f04`.
