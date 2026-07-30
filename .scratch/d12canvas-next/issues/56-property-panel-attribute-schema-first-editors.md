# 56 — Property panel + attribute schema + first editors

**What to build:** An end user selects an instance and the property panel — canvas chrome, host-positioned — shows its editable properties, built generically from attribute declarations on the component type's props record (overridable via the registration builder). This ticket ships the panel plus the Text and Number `EditorKind` controls. Each committed edit is one undoable `MutateEntity` gesture and re-renders the instance live. A component's `Text`-type *content* is excluded — that's inline editing's job. (ADR 0008.)

**Blocked by:** 29 (Click select, escape, and `aria-selected`), 37 (History core: undo/redo move & resize), 39 (Rectangle built-in)

**Status:** resolved

- [x] Editable properties are declared via attributes on the props record; the registration builder can override the declared schema
- [x] The panel renders Text and Number controls for the selection's editable properties
- [x] Committing an edit updates the instance live and records exactly one history entry
- [x] Content-text fields are excluded from the panel
- [x] The panel is chrome: standalone, host-positioned, empty-state when nothing is selected
- [x] bUnit coverage; screenshot case for the populated panel

## Comments

Implemented as: a new `D12Canvas.Panel` namespace (`EditorKind`, `[PanelEditable(EditorKind)]`,
`EditableProperty(PropertyInfo, EditorKind)`, `EditablePropertySchema.DiscoverFrom` - reflects a
`TProps` type's own attributes). `ComponentRegistration`/`ComponentRegistrationBuilder` gained a
trailing `EditableProperties` (nullable, so every pre-existing direct `ComponentRegistration(...)`
call site - tests included - is untouched); `D12CanvasOptions.RegisterComponent` resolves it as
`builder.EditableProperties ?? EditablePropertySchema.DiscoverFrom(typeof(TProps))`, i.e. attributes
set the default, the builder overrides (ADR 0008).

`PropertyPanel` is a new standalone chrome component wired to `DiagramCanvas` the same explicit way
`Palette` is (ADR 0002). Selection itself is `DiagramCanvas`'s own transient state, invisible to a
sibling component, so `DiagramCanvas` gained a `SelectionChanged` event (and a `SinglySelectedComponent`
accessor - null for no selection, a multi-selection, an edge, or a Group, matching ADR 0006/0008;
cross-type multi-select editing is ticket 59) that every selection-mutating method now raises,
alongside undo/redo (an undone/redone `MutateEntityCommand` can change the selected instance's Props
out from under the panel even though selection identity itself didn't move).

Editing a field clones the instance's boxed `Props` via `MemberwiseClone` + a single reflected
property overwrite (the generic equivalent of a `with` expression when the panel has no compile-time
`TProps`), then commits through the already-existing `DiagramCanvas.CommitPropsChange` (built for
ticket 43's inline text editing, unused by any UI until now) - so it's one `MutateEntityCommand` per
edit, same as every other Props mutation.

Decorated `RectangleProps.StrokeWidth`, `StickyNoteProps.FontSize`, `TextProps.FontSize` (Number) and
`ImageProps.AltText` (Text) - the two `EditorKind`s this ticket ships. Left every Color/Dropdown-kind
property undecorated and `ImageProps.Url` undecorated entirely (ADR 0008 reserves it for the
`Custom` file-picker escape hatch, ticket 58) for ticket 57 to add alongside their own `EditorKind`s.

New isolated `/property-panel-demo` page (its own route, not `/placement-demo`) so the new
`PropertyPanelVisualTests` baselines don't disturb the dozen existing tests that already share that
page. Observed `EdgeRoutingAndArrowheadsVisualTests.AllRoutingStylesAndArrowheads_MatchBaseline`
fail intermittently (a truncated HTML snapshot mid-render) across repeated `--parallel none` runs,
unrelated to this ticket - it wasn't touched and reran green.
