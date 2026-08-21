# The property bar is canvas-rendered chrome anchored to the selection in container pixels, and the shared-property tag becomes a closed set of roles that each own a glyph

A **property bar** floats above the current selection carrying the properties that are judged by eye, drawn as glyphs with no text. The **property panel** stays exactly as it is and keeps the long tail. Which properties reach the bar is not a curation anyone maintains: it is whichever properties declare a **property role**, and the role is also what supplies the glyph.

The reported defect was that a sticky note's colour cannot be edited at all. That is true, and its cause is that `BoardEditor` never mounts `PropertyPanel`. Mounting it fixes the reported defect with no library code, which is why the bar had to earn its place separately rather than inherit the defect's urgency. It earns it on the same split the reference tools divide along: Miro and FigJam float because their editing is expressive and few-valued, Figma Design docks because its editing is numeric and authoritative, and tldraw splits by content type rather than by product. Two of the four ship both surfaces at once.

## The bar is a third kind of chrome, and it already exists unnamed

The tempting reading is that a selection-anchored surface reopens the chrome decision, because chrome is defined as not moving when the board pans and this thing tracks board geometry. It does not, because that decision governs **host-placed** chrome: components the host positions with its own CSS, wired to a canvas by reference, with `DiagramCanvas` ignorant of them. The palette and the minimap are those.

`SelectionContextMenu` is already something else: rendered by `DiagramCanvas` itself, a sibling of `.diagram-canvas` inside `.diagram-container`, positioned in plain container-relative pixels, with a comment in the markup spelling out why its anchor is not board space. The bar joins it. Nothing is reopened; a category that was already in the code gets a name.

Two facts make this the only workable answer rather than merely the tidy one.

**A host-placed bar cannot see the case that matters most.** An edge selection lives in `_selectedEdgeId`, an exclusive slot, and `SelectedComponents` returns empty whenever it is set. So no host component can tell an edge is selected, by any public surface that exists. A bar that cannot render for an edge fails the one bullet this decision was handed by the edge-colour work.

**The clamp frame belongs to the library.** Keeping the bar on screen means clamping it against the canvas's own box, and the library is what knows that box.

A positioning mode on `PropertyPanel` was rejected for a plainer reason: the panel is host-placed and the bar is not, so a `Position` parameter would make one component two components with a mode flag deciding which of two owners places it.

## Anchoring needs no measurement of the board, and one measurement of the bar

`ZoomPanTracker` already holds scale, pan and container size, and container size is measured on `.diagram-container`, which is the frame the bar is positioned in. So the selection's rect in container pixels is `bounds.X * Scale + PanX` and `bounds.Y * Scale + PanY`, scaled extents, with no interop and no DOM read. Both halves of what a placement rule needs are already in C#.

What is genuinely not known in C# is **the bar's own width**, and the placement rule needs it twice, to centre and to clamp. There is no CSS expression of it: percentages in `left` resolve against the containing block rather than the element, so `clamp()` cannot substitute. tldraw measures its own toolbar for exactly this reason. So the bar measures itself once per content change, not per frame, and that measurement is the only new interop this decision adds.

## Clamp, do not flip

`left = anchorMidX - barWidth / 2`, `top = anchorTop - barHeight - gap`, then clamp both against the container inset by a screen margin. When there is no room above, the bar **slides along the edge rather than jumping below the selection**.

The seed note asked for flipping and the only published implementation clamps. Clamping wins on three counts: it has no hysteresis problem at the boundary, it never changes which side of the selection the user is reaching for, and it degenerates correctly on the two cases the ticket asked about without any special handling. A selection larger than the viewport has an off-screen top edge, so the clamp pins the bar to the top margin; a selection mostly off-screen to one side has its midpoint outside the frame, so the clamp pins the bar to that side. Both are the clamp doing its ordinary job, which is why neither needs a rule of its own.

Flipping is kept only as a rejected alternative, not as a fallback for the clamp. Two placement rules competing for the same case is how a bar starts jittering.

**Jitter is a real cost and is deferred rather than denied.** tldraw carries a 150ms move timeout and a 16-pixel minimum reposition distance, and anyone building this hits both within a day. They are implementation detail of one rule, not a second decision, so they are recorded here and not ticketed.

## The shared-property tag becomes a closed set of roles

This is the load-bearing change, and it was forced by choosing glyphs over text.

**A glyph cannot be derived.** Not from the property name: `FillColor` is guessable and an author's `Accent` is not. Not from `EditorKind`: `RectangleProps.FillColor` and `RectangleProps.StrokeColor` are both `Color`, so a kind-keyed glyph draws the same circle twice on the single most ordinary selection there is.

The pair that settles it is sharper than either. **`StickyNoteProps.Color` is a background and `TextProps.Color` is a foreground.** Same name, same `EditorKind`, same CLR type, opposite meaning. Every derivation strategy fails on that one pair simultaneously, so the role has to be declared.

`EditableProperty.SharedTag` is already a declared, explicitly opted-into marker that a property plays a common role, already validated to agree across types, and used by no built-in. So the tag becomes the role: `SharedTag`'s free string is replaced by a closed `PropertyRole` enum, each member owning a glyph and declaring the `EditorKind` and CLR type it expects. An enum is a compile-time constant, so this stays expressible as an attribute argument and the `Custom`-kind restriction does not apply.

The vocabulary is `Fill`, `Stroke`, `StrokeWidth`, `TextColour`, `FontSize`, `FontWeight`, `TextAlign` for authors, plus `EdgeRouting`, `EdgeSourceArrow`, `EdgeTargetArrow` and `EdgeColour` which the library uses for an edge. The edge four need no separate mechanism and no author-facing exclusion, because each role declares its expected CLR type and no author's property will be an `EdgeRouting` or an `ArrowStyle` by accident.

Seven roles covers every built-in property that belongs in a bar and leaves `ImageProps.AltText` and `ImageProps.ObjectFit` roleless, which is the correct answer for both: one is prose and the other is a three-way choice that is incomprehensible without its label. **The high-frequency-versus-long-tail split therefore stops being a judgement call and becomes an output of the role vocabulary.**

**Making the vocabulary closed strengthens a guard that is currently weaker than it reads.** Today a lone type can register a wrongly-tagged property and pass, because the validator compares pairwise against whatever else is registered and there is nothing to collide with yet; the error surfaces later, when an unrelated type is added. With each role declaring its own expected kind and type, the check runs against the role rather than against a neighbour, so the mismatch is caught on the registration that causes it.

It also closes a hole that the "never inferred from name alone" rule does not. That rule stops the framework guessing; it does nothing about an author hand-tagging `StickyNoteProps.Color` and `TextProps.Color` with the same free string, which passes validation (both `Color`, both `string`) and merges a background with a foreground into one row. With a closed vocabulary the author has to choose `Fill` or `TextColour`, and choosing wrong is visible rather than silent.

**The accepted cost is that adding a role is a library change.** That is the price of the library owning the glyph, and it is the right trade while the vocabulary is this small. An author who needs a property in the bar and finds no role for it uses the panel, which is not a degraded outcome.

## Colour merges its glyph and its control; `Custom` opens a popover

A colour role paints its value into its own glyph: a filled circle for `Fill`, an outlined ring for `Stroke`, an underlined letterform for `TextColour`, a stroked segment for `EdgeColour`. The glyph shape carries which property and the ink carries the current value, so the control needs no separate swatch and the whole cell is 26 pixels. Non-colour roles are a glyph plus a compact control.

A role-tagged `Custom`-kind property renders the author's `RenderFragment` in a **popover below its glyph**, not inline. Inline is what the prototype did and it happens to work for the four-swatch picker it was tried against, but an arbitrary author's fragment has no size contract at all, and inline means either a bar whose height is set by the worst author or a fragment that gets clipped. The popover bounds the bar's height by construction and reuses the affordance colour already has, where a glyph opens a picker. Decided rather than prototyped, and flagged as such.

## The bar hides for the duration of a pointer gesture

Not frozen and not following. There are four independent confirmations in the reference tools: tldraw's contextual toolbars hide on mouse-down with a note in the CSS beside it, tldraw's selection chrome generally is absent from the display list in the translating, resizing and rotating states, its contextual-toolbar example gates on `select.idle`, and Excalidraw's bounding box returns false while elements are being dragged. Miro ships a manual version on held `Shift`.

The reason is not cost. A bar sitting over the object you are dragging occludes the thing you are positioning, and its controls are unusable mid-drag regardless.

The consequence is that this decision needs live geometry only at gesture end, so it is **substantially decoupled from the live-geometry work it was blocked on**. The gesture preview is still what the bar re-anchors against on release, since the preview is what the commit writes, but nothing here reads the preview per frame.

## The bar reads the expanded selection, and that removes a rule rather than adding one

`SelectedComponents` currently empties its whole result if any selected id fails to resolve to a `ComponentInstance`, and a `Group`'s id never resolves. So a selected group of three sticky notes offers nothing to edit, in the panel today and in the bar tomorrow.

The rule exists for a good reason, recorded in its own comment: a shift-click can mix a grouped member, which converges onto its group's id, with a standalone instance, and that must not silently collapse into "edit the one standalone instance". Editing a subset without saying so is the hazard.

Reading the **expanded** selection removes the hazard outright instead of defending against it. Every instance under the selection is a target, a group contributes its members recursively, and there is no subset to edit silently because there is no subset. So the bar reads expanded, `SelectedComponents` changes with it, and the panel inherits the fix. This is a change to a public surface's contract and is declared here rather than smuggled into an implementation.

Presentation across a multi-selection is then the role intersection: the roles every selected instance declares, which for one type is its whole tagged set and across types is what they agree on. A group is not a special case at all once the selection is expanded.

**What this does not solve is mixed values.** The panel shows the first target's value with no mixed indicator, which was defensible for a text field. It is worse in a bar, where the swatch *is* the value, so three differently-coloured notes show one arbitrary colour as though it were the truth. Sharp enough to own its own decision and left to one.

## An edge is editable here, and `EdgeStyle` is why

An `Edge` has no `ComponentTypeKey` and no `TProps`, so the panel's discovery path cannot reach it, and that path is closed rather than missing a case: every target is a `(ComponentInstance, PropertyInfo)` pair resolved through the registry.

It does not need that path. `EdgeStyle` already bundles the settable edge properties as one immutable record for `ChangeEdgeStyleCommand`, which is exactly the props-shaped snapshot the reflection path is trying to reconstruct for an instance. So the edge is not a widening of the instance mechanism, it is a second producer of the same thing.

The seam that makes them one surface is a **row**: an id, a role, an `EditorKind`, its options, its current value, and a commit callback the producer closes over. An instance row commits through the props batch, an edge row through the edge-style command, and the bar renders both identically without knowing which it has. The commit shapes genuinely differ, since instance props are an immutable record replaced wholesale while edge style is three mutable fields snapshotted into a struct, which is why the seam is a per-row callback rather than a shared notion of "target". The prototype was built on this shape and both producers worked against it unchanged.

This gives the four settable edge properties their one surface, where the commit point's own comment has read "No panel UI calls this yet" since it was written, and it is one surface rather than four.

## The context menu and the bar do not share space

Both anchor to a selection, so the collision is real: right-clicking a selected instance near its own top edge puts the menu underneath the bar.

**The bar hides while a context menu is open.** The menu is transient and dies on the next press, the bar is persistent for as long as the selection lasts, and the menu offers nothing the bar does, so there is no state where both are needed at once. This is the same shape as hiding for a gesture, and it means neither surface needs to know the other's geometry.

Two smaller consequences. The bar is chrome outside `.diagram-canvas`, so a right-click on the bar is not a canvas press and cannot open the selection menu at all. And the context menu today has no viewport clamping whatsoever, so the clamp rule above is the first of its kind in this codebase and the context-menu work should adopt it rather than re-derive it.

## Keyboard reachability has a published answer

Figma treats its selection properties menu as a named landmark with a direct key, `Fn+F6` or `Ctrl+F6`, and Miro uses `Cmd/Ctrl+Enter` for the same move, both then navigating within by arrow keys. That is the shape `SelectionContextMenu.HandleKeyDown` already implements: roving arrow focus inside the surface, `Escape` handled locally with propagation stopped so it closes the surface without also clearing the board selection.

So the bar takes the same shape rather than inventing one, and the specific chord belongs to the keyboard-parity decision alongside every other contested binding. The bar does not trap focus: it is reachable by a deliberate key and left by `Escape` or `Tab`, which is what a landmark region means.

## What this amends and what it confirms

**The property-panel decision is amended in three places.** The shared-property tag becomes a closed role enum rather than a free string, and each role declares the editor kind and CLR type it expects, which makes the validator catch a mismatch on the registration that causes it. A second surface is added alongside the panel, rendered by the canvas rather than placed by the host, showing role-tagged properties only. And the panel's selection surface reads the expanded selection, so a selected group is editable.

**The chrome decision is not reopened.** It governs host-placed chrome; the bar is canvas-rendered, which the selection context menu already established.

**The live-geometry decision is confirmed and this decision needs less of it than expected.** Hiding for the duration of a gesture means the bar reads the preview once, on release, rather than per frame.

**The selection model is untouched.** Reading the expanded selection changes what a surface targets, not what selection is or what may join one. Edges still cannot join a multi-selection, so an edge selection reaching the bar is the existing exclusive slot being read, not a widening.

**Nothing here is board state.** No persisted bar position, no visibility flag, no envelope change.

## Considered and rejected

- **Mounting the panel in the host's board editor and stopping there**: fixes the reported defect with no library code, and is why the bar had to justify itself independently, but it leaves every property equally distant from the object being judged.
- **Replacing the panel with the bar**: the bar can only carry roles, and prose and unlabelled three-way choices legitimately need a labelled surface.
- **A positioning mode on the existing panel**: one component with two owners deciding placement by a flag.
- **Host-placed selection-anchored chrome**: cannot observe an edge selection through any public surface, and cannot know the box it must clamp against.
- **Positioning the bar inside `.canvas-content` and counter-scaling it**: rides the existing transform so pan and zoom cost nothing, but fractional counter-scale degrades text rendering and viewport clamping becomes inexpressible from inside a transformed space.
- **Flipping below when there is no room above**: hysteresis at the boundary, moves the target the user is reaching for, and competes with the clamp on exactly the cases the clamp already handles.
- **Deriving the glyph from the property name**: works for `FillColor`, fails for any author's own vocabulary.
- **Deriving the glyph from `EditorKind`**: draws one circle twice for a rectangle's fill and stroke, and cannot tell a sticky note's background from a text instance's foreground.
- **An author-supplied glyph alongside a free-string tag**: keeps the vocabulary open at the price of every author drawing their own icon, with no default that is not one of the two failed derivations above.
- **A separate `Anchored` flag on the schema, orthogonal to the tag**: a second opt-in expressing what the role already expresses, and it would let a property be in the bar with no glyph to draw.
- **Rendering a `Custom` fragment inline in the bar**: no size contract, so the bar's height is set by the worst author or the fragment is clipped.
- **Following the selection live during a gesture**: occludes the object being positioned, with controls that cannot be used mid-drag.
- **Freezing the bar in place for the duration**: what the prototype did by accident, and it leaves a bar sitting over empty canvas pointing at where the selection used to be.
- **Keeping the panel's empty-the-whole-result rule and special-casing a group**: defends against editing a subset silently, when expanding the selection means there is no subset.
- **Showing the bar and the context menu together**: two selection-anchored popovers competing for the pixels above the selection, each needing the other's geometry.
- **Widening the panel's discovery path to reach an `Edge`**: gives an edge a synthetic type key and a reflected schema to reconstruct what `EdgeStyle` already is.
- **A shared notion of "target" across instances and edges**: their commit shapes genuinely differ, and unifying them puts a branch on entity kind inside the surface.
