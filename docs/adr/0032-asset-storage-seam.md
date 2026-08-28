# Binary content is a content-addressed asset table on the Board, referenced by a declared props property and resolved before a component is rendered

A pasted image's bytes stop living inside `Props`. `Board` gains a fourth collection, keyed by a hash of the content itself, and a `TProps` property declares by attribute that it may hold a reference into it. `DiagramCanvas` swaps the reference for a renderable URL before binding props, so a component author writes `<img src="@Props.Url">` and never learns that assets exist.

```json
{
  "SchemaVersion": 1,
  "Components": [ ... ],
  "Groups": [ ... ],
  "Edges": [ ... ],
  "Assets": [ { "Id": "sha256-a3f1...", "MimeType": "image/png", "Data": "<base64>" } ]
}
```

An asset is `Id`, `MimeType` and `byte[] Data`. Nothing in that is image-shaped; only the built-in `Image` component knows about images, and a host component holding a PDF or an audio clip uses the identical seam.

## An asset is not an entity, which is why ADR 0003 is confirmed rather than reopened

That ADR's summary says "three flat collections", and read as a summary it forbids a fourth. Its decision is flat-and-ID-keyed *versus an ownership tree*, argued from one thing throughout: CRDT frameworks merge state by reconciling independently-addressable entities, and nested containment forces structural conflict resolution. An ID-keyed, reference-only table that nests nothing is the shape that argument asks for. ADR 0003 also pre-reserved an empty `Edges` collection for a ticket it did not own, so a collection arriving later is within its grain rather than against it.

An asset is not an `Entity`. It has no bounds, no `ZIndex`, it cannot be selected, hit-tested, moved or grouped, and it is referenced *by* entities rather than referencing them. The identity mechanism says the same thing from the other side: every entity id is a GUID **assigned** at creation, and an asset id is **derived** from its content. `Board` therefore holds three entity collections plus an asset table, and the two are different kinds of thing rather than four of a kind.

## Identity is a content hash

An asset's id is `sha256-` followed by the lowercase hex digest of its bytes. The algorithm prefix is there so a future digest can coexist with existing ids rather than invalidating them.

Dedup was the reason to separate bytes from props at all, and recognising a duplicate means hashing the content. Once the hash exists, a separately-assigned GUID is a second identity layered over the first that answers no question the hash has not already answered. So there is one identity, and it is the hash.

Three properties follow rather than being chosen. Adding the same file twice is free. Two clients adding the same image converge with no conflict, because an add-only table keyed by content is idempotent, which is the merge property ADR 0003's reasoning wants and nothing else on `Board` currently has. And the bytes can be checked against their own id.

**Asset ids never regenerate on paste.** ADR 0013 regenerates ids across five references precisely so pasted content is distinct from its source, and this is the first reference class on this board where that rule inverts: regenerating would duplicate the bytes on every paste and destroy the only reason the table exists. Paste therefore merges entities under fresh ids and assets under the ids they already carry, skipping any the target board already holds, which is safe because identical ids mean identical bytes by construction.

## The reference is declared, and the ticket's constraint against that was over-reaching

Ticket 23 held that nothing may inspect a `TProps` shape to discover that one of its strings is an asset reference. `EditablePropertySchema.DiscoverFrom` has always reflected over every public property of a `TProps` at registration time, reading attributes to learn what the library may do with each one. Reflection over declarations was never the thing being ruled out.

The rule underneath, and the one ADR 0021 states outright, is narrower: **the library may read what an author declares, and may never infer from a property's name or type.** That is what killed the derivation strategies for property roles, and it is what admits a declaration here.

So a `TProps` property carries `[AssetReference]`, discovered in the same registration-time pass that finds `[PanelEditable]` and cached on `ComponentRegistration` beside `EditableProperties`. The attribute applies to a `string` property; declaring it on anything else throws at registration, joining `DropdownOptionsRequiredException` and `CustomEditorRequiredException` in catching a bad declaration where it is written rather than where it is used.

This is deliberately not a `PropertyRole`. That vocabulary is closed because the library owns a glyph for each member, and an asset reference has no glyph. It does not hang off `PanelEditable` either: `ImageProps.Url` carries no such attribute today, and an asset field need not be editable at all, so coupling the two would force every asset into the property panel.

**A declared property does not always hold a reference.** `ImageProps.Url` can legitimately hold `https://example.com/photo.png`, so a reference is written as `asset:` followed by the id and the resolver leaves every other value alone. This looks at a glance like the value convention rejected below, and the difference is where the scan runs: the declaration says which property may hold a reference, and the prefix distinguishes a reference from an ordinary URL *within that one property*. No undeclared property is ever examined.

## Resolution happens before props are bound, so the component contract gains nothing

`DiagramCanvas` already constructs each instance's `DynamicComponent` and binds `Props`, and it already knows from the registration which properties are asset references. It therefore hands the component a props copy in which each `asset:` value is replaced by a `data:` URI built from the asset's mime type and bytes.

The copy is built by the same JSON round-trip ADR 0004 already uses to bind props, so no new reflection story and no clone contract on `TProps` is needed. It is cached per instance and keyed on props **reference identity**, which is exact because `MutateEntityCommand` swaps `Props` wholesale (`_instance.Props = _after`) rather than mutating it in place. A copy that had an unresolved reference is not cached, so an asset arriving later takes effect without any invalidation signal. The encoded `data:` URI is cached once per asset on the table rather than once per resolved copy, so ten instances of one image share one encoded string.

Two consequences worth stating. **ADR 0001 stays settled and gains no member** — an author writes an ordinary URL into an ordinary `src` and learns nothing. And a component type declaring no asset property is resolved by nothing at all, so the render path is byte-for-byte unchanged for every component registered today.

**ADR 0015's unbuilt board-revision counter gains no second consumer**, because reference identity answers the invalidation question without one.

## The table is add-only in memory and collected when the board is serialised

Nothing removes an asset during a session. `Serialize` writes only the assets the entities actually reference, so an abandoned image never reaches the file even though it stays in memory until the board is reloaded.

Reference counting was the obvious alternative and it defeats its own purpose. Undo has to bring deleted content back, so `RemoveEntityCommand` would have to hold the asset, which puts bytes into the history buffer — the one cost the ticket named and the one this decision was asked to remove. Add-only makes undo free instead: delete the last instance holding an image, undo, and the bytes were never gone.

**History holds references by construction**, and the ticket's hard case — undoing a paste whose asset has since been evicted — cannot occur, because nothing evicts within a session and ADR 0007 makes history session-scoped. No new command type is needed either: adding an asset is not the undoable act, creating the instance is. `Board.AddAsset(byte[] data, string mimeType)` is public, idempotent by hash, outside history, and returns the reference string ready to store in a declared property.

## Enumeration must walk edge labels

"Which assets does this set of entities reference" is one function with two callers: `Serialize`, over the whole board, and ADR 0013's copy, over the selection closed over interior edges — copying one sticky note must not carry every photo on the board.

`Edge.Label` is a full `ComponentInstance` embedded on the edge and absent from `board.Components`. An enumeration that loops over components alone therefore drops a labelled edge's image from the saved file and omits it from the clipboard payload, and does so silently. This is the same class of trap as the `PortDef.Id` regeneration ADR 0013 called out, and it is missable for the same reason: the reference lives somewhere the obvious loop does not reach.

## A missing asset degrades, and the rendering for it already ships

The serialiser has two precedents. An unresolvable `ComponentTypeKey` throws even on the partial path's strict sibling, because the instance cannot be rendered at all. A dangling edge endpoint and a dangling group member are tolerated even on the strict path, because the content merely degrades.

A missing asset is the second kind, so it is tolerated on both paths and recorded as a `BoardDeserializeWarning` on the partial one. The reference is left unresolved rather than blanked, which matters: `Image.razor` already distinguishes an empty URL ("No image") from a URL that fails to load ("Image unavailable") through its `@onerror` handler, and an unresolved reference correctly lands on the second. That degradation was written for a different reason and is exactly right for this one.

## The format does not change version

`Assets` defaults to null on `BoardEnvelope`, the sixth use of its "field didn't exist yet" convention after `Groups`, `Edges`, `CustomPorts`, `Label` and `CustomPortId`. Every board ever saved loads unchanged.

A bump was not available in any case. `EnsureSupportedSchemaVersion` compares with `!=` rather than `<=`, so moving to 2 makes every existing board throw `UnsupportedSchemaVersionException` on load, and "bump the version" is really a proposal to redesign the gate. ADR 0016 declined a bump by preference and ADR 0027 by necessity; this is the third refusal and the second on the gate's own terms.

Read by older code, a new board's props hold an unresolved `asset:` reference and render the placeholder above. Forward compatibility was never promised, and it degrades rather than failing.

## What this amends and confirms

**Amends ADR 0004** in one place: the envelope describes binary content, as an `Assets` collection alongside the three entity arrays. Its boundary is untouched — the bytes still ride inside the one document the host stores, and D12Canvas still owns only the format and the pure functions that move a `Board` through it.

**Amends ADR 0013** in two: an asset id is the one reference class that must not regenerate on paste, and a copy payload carries the assets its own entity set references rather than the board's.

**Confirms ADR 0003** (flat, ID-keyed, nothing nested, and add-only content addressing is the merge shape it argues for), **ADR 0001** (no new contract member; the registration-time attribute pass is the mechanism its own addenda established), **ADR 0021** (the never-infer rule is what makes a declaration the only legitimate route, and the closed role vocabulary is why this is not a role), **ADR 0007** (history holds references, the closed command set is unchanged), **ADR 0015** (no revision counter needed) and **ADR 0016** (no version bump).

## Stated costs

**Memory is add-only for the life of a session.** A user who adds ten images and deletes nine keeps ten in RAM until the board is reloaded, and one on disk. Content addressing bounds this to distinct content the user deliberately added.

**Both the raw bytes and their encoded `data:` URI exist in memory** for any asset currently on screen. The encoding is shared across instances but cannot be avoided, since rendering needs a URL.

**There is no size cap.** The library has no basis for a limit and nobody controls what a user puts on their clipboard, so pasting a 500 megabyte image will do whatever that does.

**A host-implemented asset store is not built.** The table is the seam one would hang on, and reaching bytes that never enter the document is a larger change than this ADR takes.

Nothing here introduces a tuned number, so ADR 0025's absent manual acceptance pass costs this decision nothing.

## Considered and rejected

- **Keeping a `data:` URI in `Props` and accepting the cost.** No missing-asset case can ever exist, every by-value route works for free, and nothing is built. Rejected because the cost lands on every serialisation route at once: `IndexedDbBoardStore` persists a board as one JSON string through JS interop and ticket 83 wired debounced autosave over it, so a base64 image is re-encoded and re-shipped on every save, and ADR 0013 puts the same envelope on the system clipboard as `text/plain`.
- **A boundary transform** — props hold a `data:` URI in memory, the serialiser extracts on save and inlines on load. Fixes every serialisation route at a fraction of the surface, touches neither ADR 0001 nor ADR 0003, and leaves `Image.razor` unchanged. Rejected because it fixes nothing in memory and leaves no seam a host-implemented store could ever hang on, the reference having to survive in memory for bytes not to be shipped.
- **A host-implemented store the library only references.** Closest to ADR 0004's boundary and the only option that shrinks the document. Rejected because it breaks by-value transport on every route and images stop working out of the box, which is the burden `d12canvas-next` settled against when it required a default palette rather than a registry alone.
- **A textual value convention scanned across all props.** Zero author burden and works at any nesting depth, since ADR 0004 already treats props as an opaque `JsonElement`. Rejected as inference from a value, which is the guessing ADR 0021 rules out; rewriting a reference would also have to happen in the JSON before binding, a second code path for paste.
- **An `AssetRef` type with a `JsonConverter`.** Findable by type at any depth with no reflection and wire-compatible if it serialises as a string. Rejected because it changes `ImageProps.Url`'s type, breaking `Image.razor` and any host code constructing `ImageProps`, and references remain discoverable only by serialising.
- **An assigned GUID with a separate content index for dedup.** Consistent with every other id on the board, but the hash exists anyway to recognise a duplicate, so the GUID becomes a second identity over the first.
- **A content hash truncated into a `Guid`.** Keeps the board's uniform id type at the price of a value that looks assigned, is not, carries none of a GUID's version or variant bits, and gives a later reader no way to tell it must never be regenerated.
- **Reference counting with a sweep on every mutation.** Correct memory, but undo must resurrect the bytes, so `RemoveEntityCommand` holds them and the history cost returns.
- **A host-called `CollectUnusedAssets()`.** Safe for undo and consistent with ADR 0004 handing timing to the host, but it is a decision every host must make and almost none will, so it is serialise-time collection with an extra API and a worse default.
- **A cascading `IAssetResolver` the author takes as a parameter.** Blazor-idiomatic, needs no props copying, and the only option letting a component ask for bytes or a mime type rather than a URL. Rejected as a genuine addition to what an author must know, on a contract the map holds settled, where an author who does not take it renders a broken reference.
- **Blob URLs written into props, swapped at the persistence boundary.** No contract change and no per-render copy, but the bytes then exist in the table and the browser's blob store at once, and the swap logic lands in the serialiser anyway.
- **Declaring collections of references, or arbitrary paths into nested props.** A gallery component would need the first and a fully general walker the second. Rejected as machinery built against a hypothetical — ADR 0004's own reason for refusing a migration pipeline before a real second version existed — and additive later, since a reference is the same string whatever holds it.
- **Making an asset an `Entity`.** It would inherit a GUID, an addressable slot and the vocabulary of board content, none of which it can use, and would put a thing with no bounds into collections whose whole purpose is that everything in them has some.
