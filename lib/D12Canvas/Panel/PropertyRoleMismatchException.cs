namespace D12Canvas.Panel;

public sealed class PropertyRoleMismatchException : Exception
{
    public EditableProperty Property { get; }
    public PropertyRoleDeclaration Expected { get; }

    public Type PropsType => Property.Property.DeclaringType!;
    public string PropertyName => Property.Property.Name;
    public PropertyRole Role => Property.Role!.Value;
    public EditorKind ExpectedKind => Expected.Kind;
    public Type ExpectedType => Expected.ClrType;
    public EditorKind ActualKind => Property.Kind;
    public Type ActualType => Property.Property.PropertyType;

    public PropertyRoleMismatchException(
        EditableProperty property,
        PropertyRoleDeclaration expected
    )
        : base(
            $"{property.Property.DeclaringType!.Name}.{property.Property.Name} declares the "
                + $"{property.Role} role, which expects EditorKind.{expected.Kind} on a "
                + $"{expected.ClrType.Name} property, but it is EditorKind.{property.Kind} on a "
                + $"{property.Property.PropertyType.Name} property."
        )
    {
        Property = property;
        Expected = expected;
    }
}
