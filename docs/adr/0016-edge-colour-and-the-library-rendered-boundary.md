# Edge colour is per-edge board data with a theme-token default, and ADR 0012's boundary is who renders the pixels

An `Edge`'s colour is a nullable per-edge field on board data, and `null` means *no author opinion* and resolves to a theme token at paint time. This discharges a defect — `.edge-line` was a hard-coded `#4a4a4a` never swept into the token layer, which is below 2:1 against the dark theme's own `#1e1e1e` backdrop and therefore not "hard to see" but invisible — and, in doing so, settles a boundary ADR 0012 drew but never had to state precisely.

## The boundary is library-rendered, not library-owned

ADR 0012 gave chrome a token layer; ADR 0008 gave per-instance visual props no theming model at all. An `Edge` fits neither: it is board content, a persisted entity in `Board.Edges`, yet it is painted entirely by the library with no author-supplied component behind it. Read as *chrome versus content* the two ADRs leave it unassignable, which is why it ended up with neither a token nor a data field.

**The line is who renders the pixels.** A sticky note's yellow cannot be themed by the library because an author's Razor component paints it and the library cannot reach inside. An edge has no such component, so the library legitimately supplies its default. Nothing in ADR 0012 is reversed by this — tokens stay CSS-only, chrome still reads them, light and dark defaults still ship, per-root declaration still holds. What is added is a class of consumer ADR 0012 neither permitted nor forbade, because until now nothing sat on the far side of the line.

The consequence is that the token layer gains its **first content-role token**. This is deliberately not the `--d12-connector-preview` case: that token is ADR 0012's escape hatch, an element diverging from a shared token it could otherwise have read. `--d12-edge` is a new role, not a divergence, and it is named for the role rather than borrowed from `--d12-muted-text` — a connector is not text, and painting it with a text token would mean a host retuning its own label colour silently recolouring every edge on every board.

## The field, and where it lives

Colour joins `EdgeStyle` — the existing before/after snapshot behind `ChangeEdgeStyleCommand` — rather than becoming a field and command of its own:

```csharp
public readonly record struct EdgeStyle(
    EdgeRouting RoutingStyle,
    ArrowStyle SourceArrow,
    ArrowStyle TargetArrow,
    string? Color = null
);
```

`ChangeEdgeStyleCommand` already bundles "the settable visual properties of an edge" on the stated principle that they travel together, so **no new command type is introduced** and undo is free — the same conclusion ADR 0014 reached for align and distribute, and ADR 0013 for paste.

The type is a CSS colour `string`, matching `FillColor`/`StrokeColor`/`Color` on every built-in rather than introducing a bespoke value type that would buy validation nothing else in the codebase has. **Nullability is the one deliberate divergence**, and it is load-bearing: every other colour prop is non-nullable because every other colour prop belongs to a component the library cannot theme. An empty string normalises to `null` on write, so absence has one representation.

Persistence is additive by the convention `EdgeEnvelope` has already used three times over — one optional parameter defaulting to `null`. **No `SchemaVersion` bump**: ADR 0004 reserved the field but defers a migration pipeline until a real V2, and a board saved before this existed deserialises to `Color = null`, which resolves to the themed default and therefore renders *more* correctly than when it was saved. Nothing needs migrating.

## The cascade resolves the fallback, not the C\#

How the author colour reaches the SVG decides whether selection can still win, and the two obvious routes are both silently broken. A **presentation attribute** (`stroke="red"`) sits at the bottom of the cascade, so `.edge-line { stroke: … }` would override the author's colour outright. An **inline style** (`style="stroke: red"`) beats everything, so `.edge-line.selected` would fail to recolour and would need `!important` or a conditional to rescue it.

The author colour therefore rides in as an **inline custom-property rebind**:

```css
.edge-line          { stroke: var(--d12-edge-override, var(--d12-edge)); }
.edge-line.selected { stroke: var(--d12-accent); }
```

`style="--d12-edge-override: …"` is emitted only when `Color` is non-null. The rendering code never learns that selection exists — it answers one question, *does this edge have an author colour?*, and `.edge-line.selected` wins on specificity regardless of what the variable holds, because it sets a different declaration. "Null falls through to the token" becomes literally the cascade's own fallback rather than a C# null-coalesce.

The override is a **separate property from the token**, not a rebinding of `--d12-edge` itself. One name would be simpler, but this ADR's contribution is sharpening what the token layer means, and letting board data write into a theme token would blur it on the very first use. `--d12-edge-override` keeps token-means-theme and override-means-data legible to anyone reading the DOM.

## Arrowheads inherit through `context-stroke`, which deletes a marker def

`<marker>` content does **not** inherit the referencing element's stroke. The arrowhead defs are shared across all edges and coloured by `.edge-arrowhead { fill: … }`, a class on the path *inside* the def — so per-edge colour would otherwise produce coloured lines with grey arrowheads, unfixable by any CSS on the referencing element.

The marker path takes `fill="context-stroke"`, SVG 2's mechanism for exactly this: the marker's fill resolves from the referencing element's stroke. Crossing stroke into fill is legal — both keywords are paint values usable wherever a paint is expected — and is the canonical arrowhead idiom. Support is universal and old: Firefox 111 (March 2023, via the bug titled *"make the context-fill and context-stroke SVG paint values work in marker content"*), Chrome 124 (April 2024) as the binding floor, WebKit earlier.

**This removes a def rather than adding one.** `edge-arrow-selected` exists only to carry a different fill; under `context-stroke` the arrowhead tracks whatever the line's computed stroke is — token default, author colour, or selection accent — so the selected variant is redundant and `EdgeMarkerUrl` loses its `isSelected` parameter. Two defs become one and a call site gets simpler.

The accepted cost: arrowhead colour leaves CSS entirely and becomes paint inheritance. `.edge-arrowhead`'s fill rule disappears, so anyone later trying to restyle arrowheads by class will find nothing to grab.

## A selected edge is always the accent

Selection paints `var(--d12-accent)`, overriding the edge's own colour. ADR 0006 makes selection **transient, unpersisted UI state**, which puts it on the library's side of this ADR's own line: the library paints it, it is not board data, so it reads a chrome token. It is also what the code already did, spelled as a literal `#2f80ed` that happens to equal the accent — so this is the status quo tokenised, and no light-mode baseline moves.

The known limit, recorded rather than solved: **an author who sets an edge to the accent blue gets no colour change on selection.** The existing `stroke-width` bump from 2 to 3 is then the only cue, and it is retained explicitly as the non-colour channel for that reason. Drawing a separate selection indicator instead of recolouring — as Figma and tldraw do — is the better answer and a much larger one, belonging with the affordance work rather than here.

## Contrast is a guarantee about the token pair, not about arbitrary fills

`--d12-edge` maintains **at least 3:1 against `--d12-surface` in both shipped themes** — WCAG's non-text threshold, appropriate for a 2px line, and checkable in a test rather than by eye. The shipped values clear it with room: `#4a4a4a` on `#f0f0f0` is above 7:1, `#a0a0a0` on `#1e1e1e` above 6:1. Light is **byte-for-byte the pre-existing hard-coded value**, following the precedent set when the grid and marquee were tokenised, so every existing light-mode edge baseline stays valid and the visual-test diff is confined to the dark cases that are currently broken.

**No guarantee attaches to an authored colour.** A white edge on a light board is the author's data, exactly the position ADR 0008 already takes for a component's own fill; promising otherwise would mean the library second-guessing author data, which nothing else here does.

The "edge crossing an arbitrary component fill" hazard is **structurally rare and not a theming problem**. `.edges-layer` sits at `z-index: 0` and every container carries `z-index: {ZIndex}` starting from `NextZIndex()`, as siblings under the transformed `.canvas-content` — so edges paint *beneath* components and are occluded by them rather than crossing them. The case arises only through send-to-back, and belongs to ADR 0008's layering arithmetic (see the traps below).

## No editing surface ships with this

`Edge.Color` is settable through the existing command path and has **no end-user surface**. This is not a gap this decision creates: `RoutingStyle`, `SourceArrow` and `TargetArrow` are all already settable through `ChangeEdgeStyleCommand` behind a commit point whose own comment reads *"No panel UI calls this yet."* Colour becomes the fourth property behind one missing surface, fixed by one surface, which is strictly better than a colour-only surface now that would not compose with the other three later.

That surface is the selection-anchored property bar's, and it inherits a structural finding: **`PropertyPanel` cannot represent an edge at all.** Every `PanelField` target is a `(ComponentInstance, PropertyInfo)` pair discovered via `ComponentTypeKey` → registry → reflected `TProps` schema. An `Edge` has neither a type key nor a `TProps`, so `[PanelEditable]` cannot simply be applied to `Edge.Color` — the panel's discovery mechanism is closed to entities, not merely missing a case. When a surface does arrive, colour is an `EditorKind.Color` field, consistent with every built-in colour prop.

## Traps recorded rather than fixed

**Send-to-back can push a component beneath the edges layer.** `PreviousZIndex()` returns `min - 1`, so it yields negative values while `.edges-layer` is pinned at `0` — after which edges paint across that component's fill, the one case this ADR's contrast reasoning excludes. Whether the edges layer belongs in the z-order at all is ADR 0008's question, not this one's.

**Built-in components freeze light-mode assumptions into board data at placement time.** `DefaultProps` for `Text` is `TextProps("", "#000000", …)`, invisible on the dark theme's backdrop — and since an edge label defaults to a `Text` instance, that reaches *inside* an edge. The rule in this ADR implicates rather than excuses them: `Rectangle.razor`, `Text.razor` and `StickyNote.razor` all ship in the library, so the library does paint them. But they register through the same public path as a host's own types, with a comment stating outright that there is no separate built-in path, so theming them specially would manufacture a two-tier registry. And the damage is not retroactively fixable: once an instance is placed, `#000000` *is* its persisted prop value, beyond the reach of any theme. That is its own decision, and is not taken here.

**Considered and rejected:**
- **Edge colour as a token only, with no per-edge field** — fixes dark mode cheaply but forecloses per-edge colour, which ADR 0005 already set the precedent for by making routing and arrowheads per-edge rather than board-wide.
- **A non-nullable colour field with a literal default** — any literal is light-shaped, so the reported defect would ship as the default case permanently.
- **Reusing `--d12-muted-text`** — good contrast in both themes and roughly the right role, but a host retuning its label colour would silently recolour every connector.
- **Separate line and arrowhead colour fields** — the arrowhead is the line's terminus, not a separate mark; a red line with a grey arrowhead is a defect, not an option.
- **A bespoke colour value type** — buys validation that no other colour in the codebase has, at the cost of consistency with all of them.
- **One marker def per distinct colour**, with the colour in the marker id — portable to browsers predating `context-stroke`, but churns `defs` on every colour change and adds a board-wide scan to render.
- **Arrowheads staying themed-only**, never following the edge colour — cheapest, and produces exactly the mismatch above.
- **Inline `stroke` style suppressed while selected** — works, but duplicates selection state into the style emitter, which is the smearing this effort is removing elsewhere.
- **Having the author colour rebind `--d12-edge` itself** — one fewer name, at the cost of board data writing into a theme token on the first use of the boundary this ADR draws.
- **Keeping the edge's own colour when selected**, with a halo or dash as the cue — how Figma and tldraw do it, and better, but it needs a second stroked path per selected edge and belongs with the affordance work.
- **Guaranteeing legibility against arbitrary authored colours or component fills** — untestable as stated, and would mean overriding author data, which ADR 0008 declines to do.
- **A colour-only editing surface** — would not compose with the three edge properties already waiting behind the same missing surface.
- **Theming the built-in components' visual defaults here** — turns on a registry two-tier question and a frozen-into-data problem that need their own decision.
