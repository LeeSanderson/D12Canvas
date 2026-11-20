# Board data keeps references, not resolved bytes, and everything that enters a board is checked or filtered

ADR 0013 makes a copy a temporary `Board`. ADR 0032 keeps binary content in a content-addressed asset table and swaps a reference for a `data:` URI before a component sees its props. ADR 0052 decides which routes give an image its picture. This records how a fragment shares data with its source, how props are copied and un-resolved, how image bytes cross from the browser into .NET, how a loaded board's edges and style values are handled, and which assets a fragment may bring in.

**A fragment builds new entities and shares the immutable props records. A committed edit goes back to asset references before it becomes a command. An asset whose id is not its hash is refused. Image bytes wait in the browser by token and cross as a stream. An edge with a missing end still loads. A props value written into an inline style is filtered before it reaches the markup.**

## A fragment's entities are new and its props are shared

`BoardFragment` builds new instances, groups and edges, so placing a fragment never moves or rewires the originals. Props records are immutable, so the fragment shares them with the source instead of copying them. The same `Board` type serves a clipboard copy and the copy a duplicate places.

`BoardFragment.StackAbove` restacks a fragment directly above everything on the board, keeping the fragment's own order. Running it again against the same board changes nothing. A clone drag stacks its copies while they are still in the user's hand, and `PlaceOnto` stacks again at release without moving them.

## An asset whose id is not its hash is refused

ADR 0032 makes an asset's id the hash of its bytes. When a fragment is placed, an asset whose id is not that hash is refused and reported in `Placement.RejectedAssetIds`. The board's asset table is keyed by content, and a mislabelled entry would later stand in for different bytes.

## A committed edit is un-resolved

A component that commits an edit hands the canvas the props object it was bound with. For a type with a declared asset reference, that object is the resolved copy and holds `data:` URIs. `AssetReferenceResolver.Unresolve` turns the edit back into references before it becomes a command. It recognises the copy by those URIs, takes the instance's committed props as the command's "before", and puts every reference the edit did not touch back into "after". The bytes never enter the board as a `data:` URI. ADR 0068 covers `DiagramCanvas.UnresolvedForCommit`, the canvas member that calls it.

## Props are copied by MemberwiseClone

`PropsCopy.With` copies a props record with `MemberwiseClone` and sets the changed properties by reflection. The library sees `TProps` only as an opaque object, so it has no compile-time type for a `with` expression. The clone copies every other field as it is and leaves the original untouched, so the original can stand as a command's "before".

## Saving tolerates unregistered types

`ReferencedAssets.IdsReferencedBy` skips an instance whose type key the registry does not know. `Serialize` does not require every instance's type to be registered, so collecting assets for a save must not add that requirement.

## Image files wait in the browser by token

`imageFiles.js` holds every picked, dropped or pasted file in a map keyed by a numeric token until .NET takes its bytes. .NET receives a `HeldImage` carrying the token, the mime type and the decoded pixel size, and nothing else. The library never reads the file's name.

A file .NET never takes, such as one it refused by type, is released once it has been held for a minute (`holdLimitMs`). That is far longer than any take waits. Stale files are released the next time `holdImages` runs. A take that started after a minute would find its file gone, and no take waits that long.

## Image bytes cross as a stream

`HeldImage.TakeBytes` reads the bytes through an `IJSStreamReference`, not as an interop return value. A stream is the one way bytes reach a Blazor Server host without hitting its interop message size limit, so a large picture arrives whole. The library sets no size limit of its own. A host that needs a cap adds it.

`TakeBytes` returns an empty array when the mime type is not a plain image subtype, since the type ends up inside a `data:` URI.

## The file picker opens inside the click

`chooseImageFile` clicks its hidden file input at once, while the click that asked for it still counts as user activation. Cancelling the picker resolves with nothing. A browser without the input's `cancel` event only gives focus back to the page, so a picker that has handed back focus and no file within a second also counts as cancelled.

## An edge with a missing end still loads

Groups are repaired on load (ADR 0053). Edges are not, because an edge end is already a reference the model allows to resolve to nothing. ADR 0032 records that the strict path tolerates a dangling edge endpoint. `BoardJsonSerializer`'s strict deserialize does no referential checking for edges and does not throw, and refusing the board would protect nothing.

The partial path never fails the whole load over an edge. It skips a structurally malformed edge with a warning, as it does a malformed component or group. An edge whose end names a missing instance still loads, its other end still resolves, and one warning records the missing reference. A missing custom port gets the same treatment as a missing standard port: one warning, and the edge loads. A host that wants dangling ends reported uses the partial path.

A board saved before auto endpoints existed can have floating ends that sit inside a shape. They stay floating. Loading never turns a floating end into an auto attachment to the shape under it, so both paths return the board the file describes.

Tests pin each case: a missing instance through a standard port, the same through a custom port, the strict path not throwing, and a floating end inside a shape staying floating.

## Inline style values are filtered

Built-in components write colours and keywords from their props into inline `style` attributes. A board can arrive from a file or from the clipboard, so those values are untrusted text.

`InlineStyleValue.Safe` passes an ordinary CSS value unchanged: hex colours, named colours, `rgb()` and `hsl()` with alpha, `var()` references, and keywords such as `bold`. It returns null for a value that holds any character that could end the declaration or break out of the attribute: `;`, `{`, `}`, `<`, `>`, a double or single quote, a backslash, a carriage return or a line feed. It also returns null for a value that contains `url(`, `image-set(`, `expression(` or `/*` in any letter case, since those fetch a resource or hide what follows.

A dropped value is not repaired or escaped. `InlineStyleValue.Declaration` emits nothing for it, so the component's own rule and the board tokens paint the property as if nobody set it. The filter is a deny list on characters and function names, not a CSS parser. An author colour that is valid CSS but uses a dropped form, such as a `url()` fill, does not render, and that is accepted.

## Consequences

Every built-in that writes a prop into an inline style goes through `InlineStyleValue.Declaration` or `InlineStyleValue.Safe`.

Code that commits a props edit runs it through `Unresolve` first, so no `data:` URI reaches a command.

The library accepts any image size the browser can decode.
