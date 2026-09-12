# Edit mode is deleted, and a container has one rendering governed by selection

`ComponentContainer` has no edit mode. `_editMode`, `InitialEditMode`, `SwitchToEditMode`, `ExitEditMode`, the `[JSInvokable] OnClickOutside` entry and the `registerClickOutside`/`unregisterClickOutside` JS pair are all removed, and `ShowSelectionOverlay` becomes `IsSelected && !IsMultiSelected`. A container renders one way, and `Selection` alone decides whether its affordances appear.

ADR 0018 deleted the `_editMode`-gated `_isDragging`/`_isResizing` pair and said at line 60 that what remained of edit mode was its own question. This is that question, and the answer is that nothing remains worth keeping.

## The mode did not work, and no test could have said so

`OnInitialized` sets `_editMode = InitialEditMode` and never registers the dismissal listener. Only `SwitchToEditMode` calls `RegisterClickOutsideHandler`. So `InitialEditMode="true"` was a one-way door: the container entered edit mode and had no way out, because the listener that exits it was never attached. `/componentcontainer-demo`'s first container shipped in that state.

`ComponentContainerTests.ComponentContainer_ClickOutside_ExitsEditMode` appears to cover exactly this. It sets `InitialEditMode: true` and then calls `container.Instance.OnClickOutside()` directly, which is the method the listener would have invoked had it existed. The test drives the far end of a wire that was never connected. This is ticket 04's shape and ADR 0026's `Ctrl+Tab` shape a third time: a green test over a path that cannot run, because the test supplies the step the browser was supposed to.

The dismissal listener is also a module-level singleton. `ComponentContainer.razor.js` holds one `handler`, and `registerClickOutside` calls `unregisterClickOutside` before installing its own. Two containers in edit mode leave the first with no listener at all, stuck the same way. The demo page could reach that state with one double-click.

## ADR 0018 removes the only entry that worked

The one route in that functioned was `@ondblclick="SwitchToEditMode"` on the container root. ADR 0018 deletes every `@onclick` and `@ondblclick` binding on board content and names `SwitchToEditMode` as one of the three it deletes. After that the concept has no entry gesture at all, and the only remaining way to set it is the parameter that cannot be undone.

So this is not a decision to remove a working feature. It is a decision not to build one, taken about a concept that has never had both an entry and an exit at the same time.

## Keeping it would cost a guard that ADR 0018 is deleting

`StickyNote.razor` and `Text.razor` both carry `@ondblclick:stopPropagation` on the element that begins inline editing, and the comment says why: to stop that double-click reaching `SwitchToEditMode`. `DiagramCanvasInlineTextEditingTests.DoubleClickToEnterInlineEditDoesNotAlsoEngageComponentContainersLegacyEditMode` asserts it.

ADR 0018 line 123 deletes that `stopPropagation`, on the ground that inline editing composes via `Native` and the canvas never learns of the press. That is only safe if nothing above the author's content wants the double-click. Retaining `SwitchToEditMode` would mean writing a replacement guard to defend a mode with no working way in. Deleting the mode is what makes ADR 0018's deletion of the workaround correct rather than merely convenient.

Text editing is untouched by any of this. It was never routed through edit mode, and the codebase spent two `stopPropagation` directives and a test keeping the two apart.

## A container outside a Board stays supported, as rendering only

`/componentcontainer-demo` is deleted along with its nav entry. Everything it demonstrated is gone: edit mode, the legacy drag and resize pair (ADR 0018), and the state readout below.

It was not the only consumer of a container outside a `Board`. `VirtualizationStressTest.razor` mounts containers as `DiagramCanvas` child content with no `Board`, using `X`, `Y`, `Width`, `Height`, `@key` and child content and nothing else. The `d12canvas-next` map kept that page deliberately as a permanent dev tool, so the usage cannot simply be declared unsupported.

It is therefore narrowed rather than removed. A `ComponentContainer` with no enclosing entity renders as a positioned box with its child content, shows no affordances, and answers no gesture. That is not a concession, it is what ADR 0017 and ADR 0018 already force: classification yields `(role, entityId, part)` and every content role carries an entity id, so a container that is not board content has nothing for a gesture to act on and no command that could be written against it. One bUnit test pins it, since the page that used to demonstrate it is going.

## The public surface goes with it

`InitialEditMode`, `OnStateChanged` and `ComponentContainerStateChangedEventArgs` are all removed.

`OnStateChanged` has exactly two firing sites, both inside gesture branches ADR 0018 deletes, and its only consumer was the demo page. It could never fire again. `ComponentContainerStateChangedEventArgs` goes whole rather than losing one field: `IsEditMode` cannot outlive the mode, and its other four fields report bounds that `OnMoved` and `OnResized` already report on commit. Live geometry is ADR 0020's `LiveGeometry` on the canvas, and the container usage that survives is static, so it has nothing to report.

This is free. `D12Canvas.csproj` declares no `PackageId`, no `Version` and no `GeneratePackageOnBuild`, and `Directory.Build.props` carries only `TreatWarningsAsErrors` and the CSharpier check. There is no published package and no released-version story, so no host outside this repo can be depending on any of it.

## The class pair goes; its one pixel stays

`ContainerCssClass` always emitted `edit-mode` or `view-mode`, and for board content it was always `view-mode`. Both names are removed.

`view-mode` was not only decoration. `.component-container` sets `box-sizing: border-box` and `.view-mode` declared `border: 1px solid transparent`, so every instance on every board has its content inset by 1px on all four sides. That declaration moves onto `.component-container` unchanged. Rendered geometry stays byte-identical, which keeps this change out of the visual diff entirely and leaves the nav entry as the single reason any baseline moves. A shorter nav row is a claim a reviewer can check against 50 screenshots; two overlapping changes is not.

This disturbs nothing in ADR 0017. The participating element is the container's border box, which is exactly `Bounds`. The inset sits inside it, so "the rect is already an instance's true region" holds either way.

## Consequences

`_dotNetRef` loses its only consumer when `OnClickOutside` goes. The `_jsModule` import and `IAsyncDisposable` survive only while `focusElement` has a caller, and that caller is `HandleClick`, which ADR 0018 deletes. The container may end up needing no JavaScript at all, but that trigger belongs to ADR 0018's implementation, not here.

Five tests move. `ComponentContainer_ClickOutside_ExitsEditMode` and `EditModeInstanceRendersResizeHandlesEvenWhenUnselected` go. `UnselectedInstanceOutsideEditModeOmitsResizeHandles` keeps its assertion and drops the dead clause from its name. `DoubleClickToEnterInlineEditDoesNotAlsoEngageComponentContainersLegacyEditMode` goes, its surviving half already covered by `StickyNoteTests.DoubleClickEntersEditModeRenderingATextEditorInPlaceOfTheParagraph`. `ComponentContainer_ImportsColocatedJsModule` needs a new assertion: it asserts `view-mode` today under a name about the module import, and the import is in fact proved by the render not throwing under bUnit's strict JSInterop.

All 50 `.verified.html` files and all 50 `.png` baselines change, because the visual suite screenshots with `FullPage = true` and the nav sidebar is in every shot. The change is one fewer nav row in each.

Handed on: `.component-container` declares `cursor: move` unconditionally, which promises movability on a `Locked` entity and on a container that has no way to move at all. That belongs to the cursor and micro-feedback patch, and unlike most of that patch's open cases it is visible at rest, so ADR 0031's pointer-capture argument does not reach it. Two hard-coded blues also survive here, `#3498db` on the resize handle and `#2f80ed` on `.selected`, both for the hard-coded colour sweep.

## Amends ADR 0018

Line 60 defers this question and describes the remnant as `_editMode` still gating `ShowSelectionOverlay` and the `edit-mode`/`view-mode` classes. The remnant was larger: `InitialEditMode`, `OnStateChanged` and `ComponentContainerStateChangedEventArgs.IsEditMode` are all part of it and none is named. A pointer to this ADR is added, and the list corrected.

Line 111 says every `@onclick` and `@ondblclick` binding on board content is deleted, and then names three, none of which is `BeginEdit`. Line 123 keeps inline editing's own double-click and deletes only its `stopPropagation`. The rule is narrowed to the three bindings it names. Read literally it deletes the text editor's trigger, which line 123 plainly does not intend.

## Considered and rejected

- **Keep edit mode as a supported rendering mode.** Costs four fixes before it does anything: attach the dismissal listener on the `InitialEditMode` path, make the listener per-container instead of a module singleton, decide what the overlay's handles do when pressed on a container with no entity id, and write a replacement for the `stopPropagation` ADR 0018 is removing. All four are spent on a mode with no entry gesture.
- **Keep the concept, renamed as a presentation flag.** Avoids the dismissal-listener work, since a flag needs no exit. Still leaves affordances rendered on something that cannot answer a press, which is the state ADR 0017 built one participation predicate to prevent. An overlay you can see and cannot use is worse than no overlay.
- **Keep the demo page with the edit-mode parts stripped.** The only option with no baseline churn at all. Rejected because the page would then show two static boxes, a status panel that can never update, and copy instructing the reader to click an Edit button that does not exist. A page that lies costs more over time than 50 mechanical baseline updates cost once.
- **Declare a container outside a `Board` unsupported.** Forces `VirtualizationStressTest` to be rebuilt on a real `Board` and discards a dev tool a previous map chose to keep, to remove a capability that costs nothing because it is what the component does with no gesture wiring at all.
- **Delete the 1px transparent border along with the class names.** Tempting, since it is a value nobody chose doing invisible layout work. It grows every instance's content by 2px in each dimension, which is a visible change to every board in the product that no ticket asked for, and it would land in the same 50 screenshots as the nav change with no way to tell the two apart.
- **Keep `OnStateChanged` as a host hook.** Leaves a public callback with no firing site, waiting for someone to notice it is dead. ADR 0020's `LiveGeometry` is where live geometry lives now.
