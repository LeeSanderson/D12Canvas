using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace D12Canvas.Panel;

// A single field the property panel renders for a component type - either discovered from a
// [PanelEditable] attribute (EditablePropertySchema.DiscoverFrom) or supplied directly by the
// registration builder as an override (ADR 0008). Options is only populated for Kind == Dropdown;
// CustomEditor is only populated for Kind == Custom - an attribute can never supply it (attribute
// arguments must be compile-time constants), so a Custom-kind property always comes from the
// builder override (ticket 58).
public sealed record EditableProperty(
    PropertyInfo Property,
    EditorKind Kind,
    IReadOnlyList<string>? Options = null,
    RenderFragment<CustomEditorContext>? CustomEditor = null
);
