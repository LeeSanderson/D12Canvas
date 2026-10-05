namespace D12Canvas.Panel;

// A property declares a role whose expected EditorKind or CLR type it does not match - thrown at
// registration time, naming the offending type and property, so the mistake fails at startup
// rather than merging a wrongly-shaped row into the panel or the bar.
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
