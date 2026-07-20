# Built-in component/shape set

Type: grilling
Status: resolved

## Question

Given the resolved component registration contract (string key, `TComponent`/`TProps` pair, required `DisplayName`/`AccessibleName`/`DefaultProps`, optional `Icon`/`Role`/`DefaultSize`/`Category`), decide which component types ship pre-registered as D12Canvas's own built-ins (candidates raised while scoping this map: rectangle, sticky note, text, freehand pen, image, etc.). For each: its `TProps` shape, default size, and default props. These register through the exact same `AddD12Canvas`/`RegisterComponent<TComponent, TProps>` mechanism as any custom host-defined component — there is no separate built-in path.

## Answer

**Boundary with ticket 12 (styling/theming):** each built-in's own visual fields (fill color, stroke color, text color, etc.) live as ordinary fields on that shape's `TProps` — the same way any custom component's `TProps` works. Ticket 12 designs only the shared editing UX/mechanism (a property panel, layering commands) on top of whatever fields exist; it does not own a separate generic style data model.

**v1 built-in set** (all registered via the identical `AddD12Canvas`/`RegisterComponent<TComponent, TProps>` call, `Category: "Basic Shapes"` on every one, each with its own distinct `Icon` glyph):

- **Rectangle** — `RectangleProps(string FillColor, string StrokeColor, double StrokeWidth)`. `DefaultSize`: 160×100. `DefaultProps`: `("#FFFFFF", "#333333", 2)`.
- **Sticky Note** — `StickyNoteProps(string Text, string Color, string TextColor, double FontSize)`. `DefaultSize`: 200×200. `DefaultProps`: `("", "#FFEB3B", "#000000", 14)`.
- **Text** — `TextProps(string Text, string Color, double FontSize, string FontWeight, string TextAlign)`. `DefaultSize`: 200×40. `DefaultProps`: `("", "#000000", 16, "normal", "left")`.
- **Image** — `ImageProps(string Url, string AltText, string Fit)`. `DefaultSize`: 240×180. `DefaultProps`: `("", "", "cover")`. `AltText` feeds the registration contract's `AccessibleName` delegate.

**Freehand Pen is out of scope for this map** — not a built-in, not deferred, not fog. Its content (a drawn path) and interaction model (continuous drag-to-draw) are fundamentally unlike the static-default-props shape every other built-in follows, and standing it up is a larger effort than this map's destination covers.

No new ADR: this is a concrete catalog built entirely on the existing registration contract (ADR `0001`) — it introduces no new architectural mechanism, so it belongs in the PRD's built-in shape catalog rather than a fresh ADR.
