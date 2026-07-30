# 57 — Full built-in `EditorKind` set

**What to build:** The property panel covers the whole closed set of built-in `EditorKind`s — Color, Checkbox, Dropdown, and the rest of the set per ADR 0008 — so the built-in component types' visual props (fill, stroke, note colour, font settings, image fit, …) are all editable. Every control commits through the same single-gesture undoable path.

**Blocked by:** 56 (Property panel + attribute schema + first editors)

**Status:** resolved

- [x] Each built-in `EditorKind` renders its control and commits edits correctly
- [x] All four built-in component types' declared props are fully editable in the panel
- [x] Every commit is one undoable history entry
- [x] bUnit coverage per control; screenshot cases for the new controls

## Comments

Implemented as: `EditorKind.Color`/`Checkbox`/`Dropdown` controls added to `PropertyPanel.razor`
alongside ticket 56's Text/Number (a `<input type=color>`, `<input type=checkbox>`, and `<select>`
respectively). `PropertyPanel.razor.cs`'s commit path (`ConvertValue`) gained a `bool` branch since
a checkbox's `ChangeEventArgs.Value` arrives as a real `bool` (Blazor reads the DOM `.checked`
property directly) rather than the `string` every other control sends - and a `CurrentBoolValue`
accessor since checkbox binds via `checked`, not `value`.

Dropdown needed a choice list `PanelEditable` didn't have: `PanelEditableAttribute`/
`EditableProperty` gained an `Options` (`string[]`/`IReadOnlyList<string>`) carried through
`EditablePropertySchema.DiscoverFrom` - a `Dropdown`-kind property with no `Options` throws a new
`DropdownOptionsRequiredException` (naming the props type/property) at discovery/registration
time rather than rendering an empty `<select>`, matching the existing
`ComponentRegistrationException`/`DuplicateComponentKeyException` shape for registration-time
failures.

Wired the four built-ins' remaining undecorated visual props (per ticket 10/12) to the new kinds:
`RectangleProps.FillColor`/`StrokeColor` and `StickyNoteProps.Color`/`TextColor` (`Color`);
`TextProps.Color` (`Color`) and `FontWeight`/`TextAlign` (`Dropdown`); `ImageProps.Fit`
(`Dropdown`). Dropdown option sets are curated CSS values covering everything already in use
across the demo app/test suite, not the full font-weight/text-align/object-fit vocabulary.
`ImageProps.Url` stays undecorated - reserved for the `Custom` file-picker escape hatch
(ticket 58).

Extended the shared `PanelTestProps` bUnit fixture (ticket 56) with one field per new kind
(`Tint`/`Flag`/`Mode`, defaulted so every pre-existing 3-arg call site kept compiling) for
control-level render/commit/undo/no-op-recommit coverage in `PropertyPanelTests` (mirroring
ticket 56's Text/Number coverage shape exactly), plus `BuiltInComponentsTests` assertions that
each real built-in's `EditableProperties` covers exactly its declared visual props, and a
`D12CanvasOptionsTests` case for the `DropdownOptionsRequiredException` throw path. `Checkbox`
has bUnit-only coverage - no built-in declares a `bool` prop yet, so there's no real screenshot
case for it; `PropertyPanelVisualTests` gained a Dropdown case (Text's `FontWeight`) and the
existing Rectangle case now also covers `Color` (`FillColor`).
