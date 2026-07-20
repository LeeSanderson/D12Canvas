# Component styling is edited through a generic property panel driven by declarative TProps schema; layering is explicit ZIndex commands with no z-slot on Group

A single property panel — a new canvas chrome component (ADR 0002), standing alongside the palette — surfaces editable fields for the current selection, built generically from whatever properties a `TProps` type declares as editable rather than one bespoke panel per component type. A component's `Text`-type content is excluded from this mechanism entirely: it is edited inline and WYSIWYG directly on the canvas, never through the panel.

Editable properties are declared by default via attributes placed directly on a `TProps` record (e.g. `[PanelEditable(EditorKind.Color)]`), mirroring the registration contract's precedent of authors declaring metadata the framework consumes without touching rendering markup themselves. The registration builder (ADR 0001) may add, override, or redefine any attribute-declared property at registration time — attributes set the default schema, the builder is the escape hatch.

Each editable property carries an `EditorKind`: a closed built-in set (Text, Color, Number, Checkbox, Dropdown, ...) covering the common cases, plus a `Custom` kind taking a `RenderFragment<TProps>` supplied by the component author for anything the built-ins can't express (e.g. Image's file picker).

When a multi-select spans different component types, the panel shows only the properties common to every selected type — and only for properties an author has explicitly opted into cross-type matching via a shared-property tag (never inferred from name alone). The framework validates that every property carrying the same tag agrees in `EditorKind`/CLR type before merging them into one editable row; a mismatch is a registration-time error, not a silent merge.

Layering acts directly on `ComponentInstance.ZIndex` (ADR 0003) via four commands: Bring to Front, Send to Back, Bring Forward, Send Backward — arithmetic-only operations (no renumbering pass), consistent with `ZIndex` having been reserved specifically so re-layering is a field write. A newly created instance defaults to current-max + 1, i.e. always on top.

`Group` (ADR 0006) gets no z-position field of its own. Group-level layering commands are bulk writes across member `ZIndex` values — preserving members' relative order to each other while moving the whole set above/below the rest of the board — so rendering stays a plain flat sort with no group-awareness, keeping `Group` as thin as it already was (computed bounds, no extra stored state).

**Considered and rejected:**
- **Reflection + naming convention for panel discovery** (e.g. auto-detect a `Color`-suffixed string as a color picker) — rejected in favor of explicit declarative schema; avoids implicit magic and can't express the `Text`-is-inline-only exception without a special case.
- **A `Group`-level z-position field with group-aware rendering** — rejected; reintroduces the structural hierarchy the flat, ownership-tree-free `Board` model (ADR 0003) was deliberately designed to avoid, for a benefit (a group occupying one stacking slot) not required by any other decision on this map.
- **Matching cross-type properties by name alone** — rejected; silently fragile to renames and to incidental name collisions between semantically unrelated properties.
- **New instances default to a fixed baseline ZIndex** — rejected; buries newly placed content behind existing shapes, contrary to the convention set by every comparable tool.
