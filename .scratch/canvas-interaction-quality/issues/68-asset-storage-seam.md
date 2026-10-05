# 68 — Asset storage seam

**What to build:** A board holds binary content once, in a content-addressed `Asset` table (ADR 0032). A host developer puts bytes into a board only through `Board.AddAsset(bytes, mimeType)`, which returns the `asset:sha256-<hex>` reference string ready to store in a declared property, is idempotent by hash and sits outside history. A component author declares a string props property as an `[AssetReference]`, meaning it may hold `asset:<id>`; the canvas swaps that for a `data:` URI before binding props, so the component still writes an ordinary `src`. Assets are collected at serialise time, including those referenced by an edge label's instance, and persist as an optional envelope field that defaults to absent with no schema bump. A saved board whose asset bytes are missing still loads on both paths, with the reference left unresolved so the built-in Image shows "Image unavailable" in place.

**Blocked by:** None — can start immediately

**Status:** resolved

- [x] `Board.AddAsset` is the only public way bytes enter a board; adding the same bytes twice yields one asset
- [x] `Assets` serialise only when present; a board with none produces byte-identical JSON to today
- [x] `[AssetReference]` on a string props property is honoured at bind time; the swap is cached by props reference identity so an unchanged props object is not rescanned
- [x] The built-in Image's `Url` is an asset reference, and an `asset:` URL that resolves renders the picture
- [x] A missing asset is tolerated on strict and partial load and the Image renders "Image unavailable"
- [x] Assets referenced by an edge label's props are collected at serialise time
- [x] A board saved under the current schema loads unchanged
- [x] `CONTEXT.md`'s `Asset` term describes what shipped
- [x] Pure C# tests for the table, hashing and serializer; bUnit for the bind-time swap

## Comments

`Asset` (id, mime type, bytes, and a `DataUri` encoded once per asset) joins `Board` as a fourth, content-addressed table. `Board.AddAsset(bytes, mimeType)` hashes the bytes, copies them, adds the asset if the id is new, and returns the reference string `asset:sha256-<hex>` (the decision's wording; the ticket's original "returns the id" is corrected above, since the reference carries the id and is the thing a props property stores, so a host writes it straight into `ImageProps.Url`). The same bytes arriving again with a different mime type are the same asset and keep the first mime type; the id is the content, and nothing in the decision gives the mime type a say. `Asset`'s constructor is internal, so bytes enter only through `AddAsset`; the serializer adds a loaded asset under the id the file carries.

`[AssetReference]` lives in `D12Canvas.Registration`. `RegisterComponent` discovers it through `AssetReferenceSchema.DiscoverFrom` in the same pass as `[PanelEditable]`, records the properties on `ComponentRegistration.AssetReferences`, and a declaration on a non-string property throws `AssetReferenceStringRequiredException` at registration. `ImageProps.Url` declares it; the `Image` component is untouched.

`DiagramCanvas` resolves through `AssetReferenceResolver` before binding: a type with no declared reference binds its own `Props` object, otherwise a copy with each resolvable `asset:` value replaced by the asset's data URI, cached by props reference identity in a `ConditionalWeakTable`, with a copy that still holds an unresolved reference never cached. The resolved object is also what `ComponentContainer` receives as its `Props` parameter, because the container skips re-rendering when nothing it compares has changed; passing the resolved copy is what lets an asset added after the first render reach the child on the next one. The copy is made by `PropsCopy`, the panel's existing MemberwiseClone-and-overwrite extracted so both share it, rather than the JSON round-trip the decision sketched; the decision's aim was no new reflection story or clone contract, and reusing the one that already ships meets it.

Review found a trap in the resolve-before-bind design that the decision does not mention: a component that commits an edit hands the canvas the props object it was bound with, and for a type with a declared reference that is the resolved copy, so `Props with { Text = ... }` would have written the data URI into the board and the bytes would have shipped inside `Props` on the next save. `CommitPropsChange` and `CommitPropsChangeBatch` now run the pair through `AssetReferenceResolver.Unresolve`: when `before` carries every resolvable reference as its data URI it is recognised as the bound copy, the committed object becomes the command's `before`, and every reference the edit left as a data URI is put back in `after`. No built-in hits this today (`Image` has no inline editor), but the test component commits an edit this way and the board keeps the reference.

The serializer writes `Assets` only when some entity refers to one (the property is omitted entirely otherwise, so a board with none is byte-identical to before), walks components and every edge label through `ReferencedAssets`, writes each asset once, and on load adds assets before anything else. A reference to an asset the file does not carry is left in place on both paths and the partial path warns once per instance and asset; the unresolved `asset:` URL then reaches the `<img>` and the browser's error lands on "Image unavailable", the path `ImageTests` already covers. Nothing visible changes for any board without assets, so no baseline moved.
