using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace D12Canvas.Panel;

// A single field the property panel renders for a component type - either discovered from a
// [PanelEditable] attribute (EditablePropertySchema.DiscoverFrom) or supplied directly by the
// registration builder as an override. Options is only populated for Kind == Dropdown;
// CustomEditor is only populated for Kind == Custom - an attribute can never supply it (attribute
// arguments must be compile-time constants), so a Custom-kind property always comes from the
// builder override. Role is only populated when the author has explicitly declared the role this
// property plays - null means it never participates in a cross-type multi-selection's merged view
// or the property bar, regardless of its name or Kind. CanHoldNull is read off the property's own
// nullability annotation: for a colour it means "no author opinion, follow the theme", and the
// panel offers the way back to that state.
public sealed record EditableProperty(
    PropertyInfo Property,
    EditorKind Kind,
    IReadOnlyList<string>? Options = null,
    RenderFragment<CustomEditorContext>? CustomEditor = null,
    PropertyRole? Role = null
)
{
    public bool CanHoldNull { get; } =
        new NullabilityInfoContext().Create(Property).WriteState == NullabilityState.Nullable;
}
