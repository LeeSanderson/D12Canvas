# Press-time focus transfer versus focus-follows-selection

Type: grilling
Status: open

## Question

Decide how ADR 0018's explicit focus transfer on press composes with ADR 0010's focus-follows-selection, given that they are now two focus writes per press with an interop hop between them.

ADR 0018 found that `preventDefault` on a captured press suppresses the browser's own focus transfer — which is why `ComponentContainer` declares it — and that suppressing focus transfer also suppresses **blur**, which is what commits an inline text edit and any focused chrome input. Its fix is for JS to blur the active element and focus the canvas container synchronously, in the same window as `preventDefault` and `setPointerCapture`.

But ADR 0010 already moves DOM focus when the selection changes, and a press that selects something changes the selection. So an ordinary press on an instance now produces: JS focuses the canvas container (synchronous), then C# receives the press, mutates the selection, and focus-follows-selection focuses that entity's tab stop (after the hop). Two writes, ordered across an async boundary, with a render in between.

Decide:

- **Whether the JS-side write should target the canvas container at all**, or whether blurring alone is sufficient to get the commit-on-blur behaviour ADR 0018 needs. Blur without an explicit focus target leaves focus on `<body>`, which is a real state with its own consequences for the window-level keyboard listener.
- **Whether the two writes can be reduced to one.** The synchronous write exists only to force a blur; the selection-driven write is the one that matters for ADR 0010. If the first can be expressed as "blur, do not focus", the second becomes the sole author of where focus lands.
- **What a press that changes nothing does.** `Pan`, and a re-press on an already-selected instance, produce no selection change and therefore no second write — so whatever the first write leaves behind is final for those presses. This is the case that killed the leave-it-to-selection option, so it needs a stated answer rather than an inherited one.
- **Whether the intermediate state is observable.** Focus moving to the container and then to a tab stop within one gesture may be visible as a focus ring flicker, and it is certainly visible to assistive technology as two focus events. ADR 0010 is reopenable, so if the right answer changes when focus-follows-selection fires, that is legitimately askable.
- **What `Native` does**, since it is uncaptured and therefore takes neither write — the browser's own focus transfer runs normally, which is the point. Confirm that an additive selection change (ADR 0018) does not then trigger focus-follows-selection and yank focus out of the author's control the user just clicked into. This is the sharpest failure candidate in the whole ticket.

Ticket 15 will want a test shape for the last one in particular: clicking an `<input>` inside a registered component must leave the caret in that input.
