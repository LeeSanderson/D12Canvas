namespace D12Canvas.Panel;

// Run once per registration over the type's final editable-property schema, whether that schema
// came from [PanelEditable] discovery or from a builder override.
public static class PropertyRoleValidator
{
    public static void Validate(IEnumerable<EditableProperty> properties)
    {
        var firstDeclaredBy = new Dictionary<PropertyRole, EditableProperty>();

        foreach (var property in properties)
        {
            if (property.Role is not { } role)
            {
                continue;
            }

            var expected = PropertyRoleDeclarations.For(role);
            if (
                property.Kind != expected.Kind
                || property.Property.PropertyType != expected.ClrType
            )
            {
                throw new PropertyRoleMismatchException(property, expected);
            }

            if (!firstDeclaredBy.TryAdd(role, property))
            {
                throw new PropertyRoleDeclaredTwiceException(
                    property.Property.DeclaringType!,
                    role,
                    firstDeclaredBy[role].Property.Name,
                    property.Property.Name
                );
            }
        }
    }
}
