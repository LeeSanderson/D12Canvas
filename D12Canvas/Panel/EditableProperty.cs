using System.Reflection;

namespace D12Canvas.Panel;

// A single field the property panel renders for a component type - either discovered from a
// [PanelEditable] attribute (EditablePropertySchema.DiscoverFrom) or supplied directly by the
// registration builder as an override (ADR 0008). Options is only populated for Kind == Dropdown.
public sealed record EditableProperty(
    PropertyInfo Property,
    EditorKind Kind,
    IReadOnlyList<string>? Options = null
);
