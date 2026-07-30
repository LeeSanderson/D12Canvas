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
            .Select(entry => new EditableProperty(entry.Property, entry.Attribute!.Kind))
            .ToList();
}
