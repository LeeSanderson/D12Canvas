# A user gives an image its picture from the panel, the object menu or a file drop, and an image made from a file takes the picture's own shape

ADR 0032 gave `Board` somewhere to keep an image's bytes, and `Board.AddAsset(byte[], string mimeType)` returns the reference a declared property holds. Nothing let a user supply those bytes. Placing an `"image"` from the palette produced the "No image" placeholder, `ImageProps.Url` carried no `[PanelEditable]` attribute, and the only producer of image content was ADR 0013's paste of a foreign bitmap, which creates a new instance and never fills one. This decides every route from a user to an image's picture, and what size the result takes.

## The empty image stays

**An `Empty image` is a legitimate state, not a defect to remove.** Palette placement, by click or by drag-and-drop, still produces one. A placeholder frame is how a wireframe says "a picture goes here", and that use needs the state to exist.

Removing it was the alternative: the palette entry opens a file picker and cancelling places nothing. It was rejected for that use, and for a mechanical reason. A file picker opens only under user activation, and a `drop` event is not reliably one, so palette drag-and-drop would have had a route that could fail silently.

## Two surfaces fill or replace a picture

- **The property panel.** `ImageProps.Url` gets the `EditorKind.Custom` editor its comment anticipated, declared through `ComponentRegistrationBuilder.EditableProperties`. It has two buttons, **Choose file…** and **Remove image**, and no text field.
- **The object menu.** Two rows, **Choose image…** and **Remove image**, shown when every selected entity is an `"image"` instance.

**Remove image** sets `Url` back to empty, returning the instance to an `Empty image`. The placeholder therefore has a way back in as well as a way out.

There is no text field for typing a URL. A typed URL is a hotlink, which breaks when the other site moves the file and makes the board depend on something outside the document the host stores, which ADR 0032 moved away from. A host can still set `Url` to any URL from code.

**The property bar gets nothing.** ADR 0021 admits a property only through a role, and a role exists for a property judged by eye with a glyph that paints its value. Choosing a file is neither.

The picker opens from a menu row or a panel button after the click has crossed into C# and back. ADR 0013 measured that user activation survives that hop for `navigator.clipboard.write`, with a two-second delay on top, so the same is expected for `input.click()`. The implementation ticket asserts it with an `Interaction probe` rather than assuming it.

## No double-press, no `F2`

**`Image` keeps declining `IInlineEditable`.** Double-pressing an image selects it, as before. The interface carries more than double-press. ADR 0051 opens every new instance of an implementing type for editing on creation, so every palette placement and `Quick create` of an image would open the OS file picker immediately, which is the removed-empty-image design returning by another door. A file picker also does not fit `EndInlineEdit` or "`Escape` commits and returns focus", which describe an editor the page can see.

If double-press-to-replace is wanted later, it needs its own seam with no edit-on-create attached.

## Dropping files on the canvas

**A JS `drop` listener on the canvas reads dropped files.** Blazor's `DragEventArgs.DataTransfer` cannot read data, so the existing `@ondrop` cannot see a file. A file drag is told apart from a palette drag by `"Files"` in `dataTransfer.types`, and the palette's own Blazor drop path is untouched.

Where a drop lands decides what it does:

- **On an unlocked `Empty image`:** the first image file fills it. Any further files become new instances.
- **Anywhere else:** each image file becomes a new `"image"` instance. That includes a drop on a filled image, a locked image, or any other instance.

New instances from one drop cascade by ADR 0013's `+20, +20` from the drop point. Files whose MIME type is not `image/*` are ignored. The whole drop is one history entry.

Replacing a filled image on drop was rejected. A slightly misplaced drop would overwrite a picture the user meant to keep, which undo recovers only if the user notices. Miro and Figma both create a new image instead, and replacing stays on the panel and the menu, where it is deliberate.

**Paste still never fills.** A bitmap pasted while an `Empty image` is selected creates a new instance, as ADR 0013 decides. A drop has a target under the pointer. A paste has an anchor and no target.

## Size

**An instance created from a file, by drop or by bitmap paste, takes the picture's own pixel size in board units, scaled down to fit within half the visible viewport at the current zoom, and never scaled up.** The aspect ratio is always kept, so nothing is cropped. A small icon lands at its real size and a large photo lands as large as fits comfortably. The bound is in board units at the current zoom, so zooming out before dropping gives a larger image. Snap-to-grid rounds the top-left per ADR 0048, and the size is not rounded, because size changes only through resize.

JS reads the pixel size while decoding, which both the drop route and the paste route already do in JS.

The 240x180 `DefaultSize` was rejected for these routes: it crops anything that is not 4:3 and makes a screenshot unreadable. The picture's raw pixel size was rejected because a phone photo lands many screens wide.

**Filling or replacing a picture keeps the instance's box exactly.** `ObjectFit`, default `cover`, decides how the picture sits in it. A placeholder's box is already part of a layout, either because the user sized it or because they put it there to hold a spot, and resizing it when the picture arrives would push that layout around. PowerPoint placeholders and Figma image fills both keep the frame. The cost is the crop the original ticket named. It is visible at once and fixed in one step, with `ObjectFit` or a resize. If it proves to hurt in use, the answer is a "fit box to picture" row, not a change to this rule.

## What is accepted

- **No size limit on any route**, as ADR 0032 decided for paste. The library still has no basis for a number. A limit only on the picker and drop would make the same file fail when dropped and succeed when pasted. If a host needs a limit, it belongs in `Board.AddAsset`, where it covers every route with one rejection behaviour.
- The picker uses `accept="image/*"`, and drop filters on an `image/*` MIME type.
- A file the browser cannot decode is ignored on drop, and leaves the instance unchanged on the picker routes. A stored picture that later fails to render already shows `Image.razor`'s error state.
- Cancelling the picker writes nothing.
- Each choose and each remove is one history entry, recorded as a props change.

## Several images at once

**Both surfaces act on every selected image.** The panel already shows every property of a same-type multi-selection and commits a change to all targets, so the editor follows that. The menu rows follow the panel rather than being single-image only, so the same action never depends on where it was started. One picture goes into every selected image as one history entry, and **Remove image** clears every one. When the images hold different pictures the editor shows mixed, per ADR 0040. A selection holding locked images follows whatever the existing selection rows do with locked members. No rule is invented for it here.

## Keyboard

**No chords.** Both rows are reached with `Shift+F10` or the `ContextMenu` key, and the panel is reachable by focus. This follows ADR 0050's reading of ADR 0026: the rule that every selection action has a chord covers actions done about as often as grouping. Replacing a picture is rarer than that and applies to one built-in type, while every chord ADR 0026 assigned works on any selection. Under ADR 0026's hint rule, neither row shows a hint.

## How this is verified

Per ADR 0025:

- An `Interaction probe` opens the picker from a menu row and from the panel button, and asserts that the file input received a user-activated `click`. A test that calls the commit callback directly cannot prove the browser would open the picker.
- An `Interaction probe` dispatches a real `drop` carrying a `File` onto an `Empty image`, onto a filled image and onto empty canvas, and asserts fill, new instance and new instance.
- A drop of several files is one history entry, and new instances cascade from the drop point.
- An image created from a file keeps the file's aspect ratio. The size bound is asserted as a relationship to the viewport, not as a value.
- Filling or replacing leaves `Bounds` unchanged.
- A non-image file on drop leaves `Board` unchanged.
- A multi-selection of images takes one picture into every target as one history entry.
- The panel editor and the two menu rows get visual cases.

## Amends, confirms

- **Amends ADR 0013** in one place: a pasted bitmap's instance takes the picture's own size, bounded as above, instead of the type's `DefaultSize`.
- **Amends ADR 0023** with two rows, **Choose image…** and **Remove image**, eligible when every selected entity is an `"image"` instance.
- **Confirms ADR 0032.** No cap, and the bytes enter `Board` only through `AddAsset`.
- **Confirms ADR 0051.** `Image` stays one of the built-ins that decline `IInlineEditable`.
- **Confirms ADR 0021.** No new role.
- **Confirms ADR 0026 as read by ADR 0050.** A rare, type-specific action is reached through the menu.

## Considered and rejected

- **No empty image: the palette opens a picker.** Removes the wireframe placeholder, and the drag-and-drop path cannot reliably open a picker from `drop`.
- **A property-bar role for the picture.** A role is for a property judged by eye, and this is a file choice.
- **`Image` implementing `IInlineEditable`.** Brings edit-on-create with it, so every new image opens a picker.
- **Replacing a filled image on drop.** Easy to trigger by accident, and the overwrite is easy to miss.
- **A typed URL field.** Hotlinks break, and they put the board's content outside the stored document.
- **`DefaultSize` for an image made from a file.** Crops anything that is not 4:3.
- **The picture's raw pixel size, unbounded.** A phone photo lands many screens wide.
- **Resizing the box on fill, either to the picture's aspect or by the new-instance rule.** Moves a layout the user built around the placeholder.
- **A library default size limit on the picker and drop.** No basis for a number, and it makes the same file behave differently by route.
- **Single-image-only menu rows.** Contradicts the panel, which already applies to every selected image.
- **Chords for the two rows.** Too rare, and limited to one type, to earn a slot in a nearly full table.
