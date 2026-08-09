# Asset storage seam: where binary content lives

Type: grilling
Status: open
Blocked by: 09

## Question

Decide whether binary content — a pasted or placed image, and anything like it later — lives inside board content or beside it, and amend ADR 0004 accordingly.

Ticket 09 decided that a pasted bitmap becomes an `"image"` component instance, and deliberately did *not* decide where its bytes go. This ticket owns that, and it is the reason ADR 0004 moved from the map's settled list to its reopenable one — the envelope's shape is exactly what changes if assets stop being ordinary `Props` data.

The default today is a `data:` URI in `ImageProps.Url`, which needs no ADR change at all: `Props` is opaque business data (ADR 0001/0003), and a long string is just a string. Its cost is concrete rather than theoretical. Base64 inflates the payload by roughly a third, and the result lands in three places at once — the in-memory `Board`, the persisted envelope (which `BoardJsonSerializer` round-trips as one whole document, with no streaming path), and ADR 0007's history, where `AddEntityCommand` holds the `ComponentInstance` by reference and keeps an undone image paste resident until the 1000-entry circular buffer evicts it.

The tension is that ADR 0004 drew its boundary at exactly this line: D12Canvas owns the wire format, the host owns the storage medium and timing. `d12canvas-next` ticket 13 then held that line hard, making import/export host-wired only. An asset store is either a widening of what the library's format describes, or a second thing the host owns — and which it is, is the decision.

Decide:

- **Whether an asset is a first-class concept at all**, or whether `Props` holding a `data:` URI is the answer and the cost is simply accepted. Ruling it out is a legitimate outcome; it returns ADR 0004 to settled.
- **Where the bytes live if they are separated** — a new collection in the envelope, a side-car the host is handed alongside the JSON, or a host-implemented store the library only references. The last is closest to ADR 0004's existing boundary and the furthest from working out of the box.
- **How a `Props` field references an asset without the library parsing opaque `Props`.** This is the hard part: nothing may inspect a `TProps` shape to discover that one of its strings is an asset reference. A textual convention (`asset:<guid>`), a registration-declared asset field, or a dedicated `TProps` field type are the obvious candidates, and each pushes cost somewhere different — onto every component author, onto the registration contract, or onto the serializer.
- **What a board without its assets does** — pasted into a tab that never saw them, imported without its side-car, or loaded when a host's store has lost one. ADR 0004's partial-deserialize-with-warnings path is the obvious precedent; whether a missing asset is a warning or a hard failure is not obvious.
- **Whether history stores bytes or references**, and what that means for undoing a paste whose asset has since been evicted.
- **Whether this is `SchemaVersion` 2**, and therefore whether it is the first real migration — ADR 0004 reserved the field and explicitly declined to build a migration chain "until a real second schema version exists". If this is that version, the mechanism it declined to speculate on is now in scope.
- **Whether the seam is image-specific or general.** A host component type could hold a PDF, an audio clip, or its own blob just as easily, and a seam named for images will not survive the second case.

Amends ADR 0004, and feeds whatever implementation ticket carries ADR 0013's image paste.
