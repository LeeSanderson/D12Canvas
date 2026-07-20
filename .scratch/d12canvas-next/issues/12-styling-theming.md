# Styling/theming and layering

Type: grilling
Status: resolved
Blocked by: 05, 06

## Question

Design styling/theming for component instances (fill/stroke/colors/fonts) and the layering UX built on the state model's explicit `ZIndex` field (e.g. bring-to-front/send-to-back, how z-order interacts with groups). Depends on the component registration contract (05) to know what a component type exposes as themeable, and the state/data model (06) for the `ZIndex` field it acts on.

## Answer

No new theming/style data model — per the boundary set in ticket 10, visual fields (fill color, stroke, font, etc.) stay ordinary fields on each component type's own `TProps`. This ticket designs only the shared editing mechanism and the layering behavior on top of that.

**Property editing:**
- A generic **property panel** (a new canvas-chrome component, alongside the palette) shows editable fields for the current selection's `TProps` — except a component's `Text`-type content, which is edited **inline/WYSIWYG** directly on the canvas, never through the panel.
- Editable fields are declared by **attributes on the `TProps` record** as the default schema; the registration **builder can override or redefine** any of them per component type — mirroring the `AccessibleName` delegate precedent from ADR 0001.
- Each declared field carries an `EditorKind` from a **built-in set** (Text/Color/Number/Checkbox/Dropdown/etc.) **plus a `Custom` escape hatch** taking an author-supplied `RenderFragment<TProps>` for anything the built-ins can't express (e.g. Image's file picker).
- A multi-select spanning **different** component types shows the **intersection** of editable properties — but only for properties explicitly opted into cross-type matching via a **shared-property tag** the author adds deliberately (never matched by name alone). The framework validates tagged properties agree in `EditorKind`/CLR type before merging them into one editable row; a mismatch is a registration-time error.

**Layering:**
- Four commands act on `ComponentInstance.ZIndex`: **Bring to Front, Send to Back, Bring Forward, Send Backward** (one-step) — pure arithmetic field writes, no renumbering pass, consistent with `ZIndex` having been reserved in ADR 0003 specifically so re-layering is cheap.
- A newly created instance defaults its `ZIndex` to **current-max + 1** (always on top), reusing the "Bring to Front" arithmetic.
- `Group` gets **no z-position field of its own** — group-level layering commands are **bulk writes across member `ZIndex` values** (preserving members' relative order to each other), so rendering stays a plain flat sort with no group-awareness. Keeps `Group` as thin as ADR 0006 already made it (computed bounds, no extra stored state).

Seeded ADR [0008](../../../docs/adr/0008-property-panel-and-layering.md) and new `CONTEXT.md` terms (`Property panel`, `Editable property`, `EditorKind`), plus a short addendum to `Group`'s entry noting the layering behavior.
