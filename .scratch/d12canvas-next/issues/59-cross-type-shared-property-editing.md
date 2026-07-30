# 59 — Cross-type shared-property editing

**What to build:** An end user multi-selects instances of different component types and the property panel shows only the properties explicitly tagged as shared across types — editing one applies the change to every selected instance as a single atomic, undoable gesture. Untagged properties never appear for a cross-type selection. (ADR 0008.)

**Blocked by:** 32 (Marquee + shift-click multi-select), 56 (Property panel + attribute schema + first editors)

**Status:** resolved

- [x] Cross-type multi-selections surface only explicitly-tagged shared properties
- [x] Committing a shared-property edit applies it to all selected instances
- [x] The bulk edit is one atomic history entry; undo restores every instance's prior value
- [x] A same-type multi-selection still edits that type's full declared schema
- [x] bUnit coverage of tag filtering and bulk apply

## Comments

Implemented as: `PanelEditableAttribute`/`EditableProperty` gained a `SharedTag` (`string?`) - an
author explicitly opts a property into cross-type matching by giving it the same tag as another
type's property (never inferred from name alone). `SharedPropertyValidator` (new, `D12Canvas.Panel`)
runs from `D12CanvasOptions.RegisterComponent` on every registration, checking the new type's own
`EditableProperties` against every already-registered type's for `EditorKind`/CLR-type agreement per
tag, throwing `SharedPropertyMismatchException` (naming both conflicting types/properties) on a
mismatch - "a mismatch is a registration-time error, not a silent merge" (ADR 0008).

`DiagramCanvas.SinglySelectedComponent` (ticket 56) is replaced by `SelectedComponents`
(`IReadOnlyList<ComponentInstance>`) - every top-level selected id resolved via `Board.GetComponent`,
still NOT expanded through group membership; any id that fails to resolve (a selected Group) empties
the whole result rather than dropping just that one entry, preserving "a Group has no Props of its
own" both for a lone selected Group and for a selection that mixes a group with a standalone
instance (a real gap caught during review - a shift-click can mix a grouped member's group id with a
standalone instance's own id in the same ad-hoc selection).

`PropertyPanel` is rebuilt around a `PanelField` (FieldId/Label/Kind/Options/CustomEditor plus a list
of per-instance `(ComponentInstance, PropertyInfo)` targets). A same-type selection (1 or 2+
instances) maps every target to the one type's own `PropertyInfo`s (`SameTypeFields` - the full
declared schema, extending ticket 56's single-instance case to N same-type instances). A cross-type
selection instead intersects every selected type's own `SharedTag`s (`CrossTypeFields`) and, for each
surviving tag, resolves each instance's own type-specific `PropertyInfo` for that tag - the two types
can name the property differently, `SharedPropertyValidator` only guarantees `EditorKind`/CLR-type
agreement. A cross-type field is keyed/labelled by the tag itself, not any one type's property name.

Committing (`Commit`) diffs every target against the new value, builds one
`(InstanceId, Before, After)` triple per instance that would actually change, and routes the batch
through a new `DiagramCanvas.CommitPropsChangeBatch` - one `MutateEntityCommand` per changed instance
wrapped in a single `CompositeCommand` (ADR 0007), so the whole gesture undoes/redoes as one atomic
entry regardless of how many instances it touched, or whether every target ended up changing.

bUnit coverage: `PropertyPanelTests` gained a second registered component type
(`PanelTestPropsSecondary`, `AccentColor` sharing `PanelTestProps.Tint`'s new `"color"` tag) plus
cases for same-type multi-select full-schema rendering/bulk-commit/atomic-undo, cross-type
tag-filtered rendering/bulk-commit/atomic-undo, and the mixed-group-plus-instance selection edge
case surfaced during review. `D12CanvasOptionsTests` gained cases for `SharedTag` discovery, a
compatible cross-type registration, and both `EditorKind`- and CLR-type-mismatch throws (new
fixtures `PropsWithMismatchedSharedTagKind`/`PropsWithMismatchedSharedTagClrType`).

Did not extend `SharedPropertyValidator` to also check `Dropdown`'s `Options` list or duplicate tags
within one type's own schema - narrower than ADR 0008's literal "agrees in EditorKind/CLR type"
wording; left as a possible follow-up rather than expanding validation scope speculatively.
