# 98 — Image content routes

**What to build:** An empty image is never a dead end: an end user gives it a picture from the property panel, from the context menu, or by dropping a file onto it, and a dropped or pasted picture arrives at a sensible size (ADR 0052). The panel's `Url` property gets a `Custom` editor with Choose file and Remove image and no text field. The object menu gets the same two rows when every selected entity is an image. A `drop` listener fills an unlocked `Empty image` with the first file and creates new instances for the rest, or creates new instances anywhere else, cascading from the drop point, in one history entry. A pasted bitmap creates an image. An instance made from a file takes the picture's pixel size in board units, scaled down to fit half the visible viewport and never up. Filling keeps the box. Paste never fills an existing empty image. Bytes enter through `Board.AddAsset`, so a host caps size there; the library has no limit.

**Blocked by:** 95 (System clipboard)

**Status:** ready-for-agent

- [ ] Choose file in the panel fills the selected empty image and keeps its box; Remove image returns it to the placeholder; both undo
- [ ] Dropping a PNG onto an empty image fills it; dropping it on canvas creates an image at the picture's pixel size, bounded to half the viewport
- [ ] Dropping two files on canvas creates two cascaded images in one history entry
- [ ] Pasting a bitmap from the clipboard creates a new image even when an empty image is selected
- [ ] Pasting the same picture twice stores its bytes once
- [ ] The two rows appear only when every selected entity is an image
- [ ] A demo page shows the empty-image placeholder on a dark board for the visual suite; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Empty image` term describes what shipped
