using System.Reflection;

namespace D12Canvas.Panel;

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
            Role: attribute.Role
        );
    }
}
