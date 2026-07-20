# Component registration & palette API contract

Type: grilling
Status: resolved

## Question

Design the public contract for developer-defined canvas components: how does a host app register a custom component (what does the registration call/attribute look like — type, metadata, icon, default size?), what must a registered component implement or supply to be screen-reader-friendly by contract (even though full keyboard navigation is deferred), and what shape does the default out-of-the-box palette UI take (a fixed sidebar? a searchable list? categorized?)? This is the ADR for the extensibility model — the single mechanism both built-in and custom shapes register through.

## Answer

Registration is DI-based: `services.AddD12Canvas(o => o.RegisterComponent<TComponent, TProps>("key", builder => ...))`. Identity is a developer-assigned string **key**, decoupled from the CLR type, so persisted boards survive class renames/refactors. `TProps` is a strongly-typed, JSON-serializable POCO per component type holding its own business data — kept structurally separate from bounds (X/Y/Width/Height), which stay a uniform, type-independent concept on the instance/`ComponentContainer`, not folded into `TProps`, so a future viewport-query surface can scan bounds across mixed component types without reflecting into per-type props.

Required builder metadata: `DisplayName`, `AccessibleName: Func<TProps,string>` (auto-applied by `ComponentContainer` as an ARIA label — component authors never touch ARIA markup themselves), `DefaultProps`. Optional: `Icon` (falls back to a derived initial), `Role` (default `"group"`), `DefaultSize` (falls back to `ComponentContainer`'s existing 200×150), `Category` (accepted, unused by v1 palette). Built-in shapes register through this exact same call — no separate path.

The default palette (`CanvasPalette`) and any future canvas-level UI (e.g. a minimap) form a new **canvas chrome** category, distinct from board content: standalone components the host places in their own markup — not nested inside `DiagramCanvas`'s pannable `ChildContent` — wired to a specific canvas via an explicit reference (mirroring the existing `ParentCanvas` cascading-parameter pattern). No built-in `Position` parameter; placement (including floating over the canvas) is entirely the host's own CSS/layout. `CanvasPalette` renders a flat list (registration order) using `DisplayName`/`Icon`; `Category` is accepted but unused for grouping in v1.

Deferred to other tickets: connector anchor points ("Connectors/edges model"); the click/drag verb for placing a palette entry onto the board (tracked as "Tool modes & interaction details" fog).

Full glossary and architectural rationale: `CONTEXT.md` (Component type, Component instance, Key, Props, Bounds, Canvas chrome, Palette) and `docs/adr/0001-component-registration-contract.md`, `docs/adr/0002-canvas-chrome-separate-from-board-content.md`.
