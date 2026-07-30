using System.Reflection;

namespace D12Canvas.Panel;

// ADR 0008: the default editable-property schema for a TProps type is whatever its own
// [PanelEditable] attributes declare - reflection, not naming convention (a Color-suffixed string
// isn't auto-detected as a color picker; that was considered and rejected). A Text-type *content*
// field (e.g. StickyNoteProps.Text) is excluded simply by never carrying the attribute - inline
// WYSIWYG editing owns that field instead (ticket 43).
public static class EditablePropertySchema
{
    public static IReadOnlyList<EditableProperty> DiscoverFrom(Type propsType) =>
        propsType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property =>
                (
                    Property: property,
                    Attribute: property.GetCustomAttribute<PanelEditableAttribute>()
                )
            )
            .Where(entry => entry.Attribute is not null)
            .Select(entry => ToEditableProperty(entry.Property, entry.Attribute!))
            .ToList();

    // A Dropdown control with no choices can't render a usable <select> - caught here, at
    // discovery/registration time, rather than surfacing as an empty control in the panel. A
    // Custom-kind attribute can never carry the RenderFragment it needs (ticket 58) - caught the
    // same way.
    private static EditableProperty ToEditableProperty(
        PropertyInfo property,
        PanelEditableAttribute attribute
    )
    {
        if (attribute.Kind == EditorKind.Dropdown && (attribute.Options?.Length ?? 0) == 0)
        {
            throw new DropdownOptionsRequiredException(property.DeclaringType!, property.Name);
        }

        if (attribute.Kind == EditorKind.Custom)
        {
            throw new CustomEditorRequiredException(property.DeclaringType!, property.Name);
        }

        return new EditableProperty(
            property,
            attribute.Kind,
            attribute.Options,
            SharedTag: attribute.SharedTag
        );
    }
}
