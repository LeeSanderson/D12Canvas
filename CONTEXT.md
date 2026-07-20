# D12Canvas

A Blazor Razor-component library for an embeddable, local-first diagramming canvas — a Miro-like board of extensible, developer-registered components.

## Language

**Component type**:
A registered kind of canvas content (e.g. "sticky note"), identified by a stable key and defined by a rendered component plus a props type.
_Avoid_: shape, node — not used anywhere in this codebase; "component" is the established term.

**Component instance**:
A placed occurrence of a component type on a board, with its own bounds and props value.

**Key**:
The stable string a component type is registered under, chosen independently of its .NET type name so persisted boards survive renames/refactors of the underlying class.
_Avoid_: using the CLR type name as identity.

**Props**:
A component type's own serializable business data (e.g. a sticky note's text and color) — distinct from its bounds.
_Avoid_: parameters — Blazor's own term for a broader concept (includes callbacks, render fragments); "props" specifically means the serializable data payload.

**Bounds**:
A component instance's position and size, tracked uniformly across every component type independent of its props — what lets the canvas query "what's on screen" without knowing any specific component type's shape.

**Canvas chrome**:
UI anchored to a canvas's viewport rather than its pannable/zoomable board surface — it doesn't move when the board pans and isn't part of persisted board content. The palette is the first example; a minimap would be another.
_Avoid_: overlay, widget.

**Palette**:
The default canvas chrome component that lists registered component types for the user to pick from.
