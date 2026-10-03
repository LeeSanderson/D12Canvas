# No route to give an image any content

Type: grilling
Status: resolved
Blocked by:

## Question

Decide how a user puts a picture into an `"image"` instance, now that ADR 0032 has given the library somewhere to put the bytes but no way for a user to supply them.

`BuiltInComponents` registers `"image"` with `DefaultProps = new ImageProps("", "", "cover")` and a 240x180 `DefaultSize`, so placing one from the palette — by click-to-add or by drag-and-drop — produces a box that renders `Image.razor`'s "No image" placeholder. Nothing in the product can then fill it. `ImageProps.Url` carries no `[PanelEditable]` attribute, so the property panel cannot reach it, and its comment reserves the field for an `EditorKind.Custom` file picker that has never been built. The only producer of image content anywhere is ADR 0013's paste of a foreign bitmap, which creates its own instance rather than filling an existing one — so the palette entry is an affordance that cannot be completed, which is squarely this map's business.

ADR 0032 supplies the missing half of the mechanism: `Board.AddAsset(byte[], string mimeType)` is public and idempotent, and returns the reference string a declared property holds. What is missing is every route from a user to those bytes.

Decide:

- **Which routes exist.** A file picker reached from the property panel (what `ImageProps.Url`'s comment anticipates), a file picker reached from the property bar or the object menu, dropping a file onto the canvas, or dropping a file onto an existing empty image. These are not exclusive and the cheapest may be enough.
- **Whether dropping a file on the canvas is reachable at all as currently built.** `DiagramCanvas.razor` has one `@ondrop`, wired to the palette's own drag-and-drop, and its own code notes that Blazor's `DragEventArgs.DataTransfer` exposes no `GetData`. So a file drop cannot be read through Blazor's drop event, and reaching it means a JS listener — the same move ADR 0017 and ADR 0018 already made for pointer input, on a canvas that now has a JS listener to extend rather than a new one to invent.
- **What size a placed image takes.** `DefaultSize` is 240x180 and a photo is rarely 4:3, so `object-fit: cover` crops silently. Whether a supplied image resizes its instance to its own aspect ratio, and if so bounded by what, is a rule this ticket owns; ADR 0024's snap-to-grid now applies to placement, which interacts.
- **Whether an empty image instance should be placeable at all.** Ruling that the palette entry opens a picker immediately — so an image instance never exists without content — is a legitimate outcome and removes the "No image" state from the product rather than giving it an exit.
- **Whether this reaches the keyboard.** ADR 0026 gives every action on a selection a chord and this would be a new one; ADR 0028 already found `AddCustomPort` has no keyboard route at all, which is [Keyboard route to adding a custom port](43-keyboard-add-custom-port.md)'s.
- **Whether a size limit belongs here.** ADR 0032 states outright that there is no cap and that the library has no basis for one, but a file picker is the first place where the library chooses what to accept, which is a different position from the clipboard.

Feeds whatever implementation ticket carries ADR 0032, and touches ADR 0021's property bar and ADR 0023's object menu if either gains a row.

## Answer

Recorded as ADR 0052, worked as a grilling.

Decided:

1. **The `Empty image` stays.** Palette placement still produces the placeholder, because a wireframe needs one. Making the palette open a picker was rejected, partly because a `drop` event cannot reliably open one.
2. **Two surfaces: the property panel and the object menu.** `ImageProps.Url` gets its `EditorKind.Custom` editor with **Choose file…** and **Remove image** and no URL field. The menu gets **Choose image…** and **Remove image**. Nothing goes on the property bar, which ADR 0021 keeps for properties judged by eye.
3. **No double-press and no `F2`.** `Image` keeps declining `IInlineEditable`, because ADR 0051's edit-on-create would open a picker on every new image.
4. **File drop through a JS listener.** A drop on an unlocked `Empty image` fills it. A drop anywhere else, a filled image included, creates new images cascading `+20, +20` from the drop point. Non-image files are ignored and the drop is one history entry.
5. **An image made from a file (drop or bitmap paste) takes the picture's own size,** scaled down to fit half the visible viewport and never up. Aspect ratio is kept.
6. **Filling or replacing keeps the box.** `ObjectFit` decides the crop.
7. **No size limit on any route.** A host-set limit, if ever needed, goes in `Board.AddAsset` for every route at once.
8. **Several selected images:** both surfaces act on all of them as one history entry, because the panel already does. Mixed values follow ADR 0040.
9. **No chords.** `Shift+F10` reaches both rows, following ADR 0050's reading of ADR 0026.

Consequences checked against the other answers before sign-off: question 8's single-image menu rows contradicted the panel's existing multi-selection behaviour and were reversed in question 10; several files dropped on an `Empty image` fill it with the first and cascade the rest; paste still never fills, because it has an anchor and no target. Expected but unmeasured: user activation surviving the Blazor hop for `input.click()`, which ADR 0013 measured for clipboard writes. The implementation ticket asserts it with an `Interaction probe`.

Amends ADR 0013 (pasted-image size) and ADR 0023 (two rows). New `CONTEXT.md` term, `Empty image`.
