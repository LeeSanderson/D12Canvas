# 68 — Asset storage seam

**What to build:** A board holds binary content once, in a content-addressed `Asset` table (ADR 0032). A host developer puts bytes into a board only through `Board.AddAsset(bytes, mimeType)`, which returns the `sha256-<hex>` id, is idempotent by hash and sits outside history. A component author declares a string props property as an `[AssetReference]`, meaning it may hold `asset:<id>`; the canvas swaps that for a `data:` URI before binding props, so the component still writes an ordinary `src`. Assets are collected at serialise time, including those referenced by an edge label's instance, and persist as an optional envelope field that defaults to absent with no schema bump. A saved board whose asset bytes are missing still loads on both paths, with the reference left unresolved so the built-in Image shows "Image unavailable" in place.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] `Board.AddAsset` is the only public way bytes enter a board; adding the same bytes twice yields one asset
- [ ] `Assets` serialise only when present; a board with none produces byte-identical JSON to today
- [ ] `[AssetReference]` on a string props property is honoured at bind time; the swap is cached by props reference identity so an unchanged props object is not rescanned
- [ ] The built-in Image's `Url` is an asset reference, and an `asset:` URL that resolves renders the picture
- [ ] A missing asset is tolerated on strict and partial load and the Image renders "Image unavailable"
- [ ] Assets referenced by an edge label's props are collected at serialise time
- [ ] A board saved under the current schema loads unchanged
- [ ] `CONTEXT.md`'s `Asset` term describes what shipped
- [ ] Pure C# tests for the table, hashing and serializer; bUnit for the bind-time swap
