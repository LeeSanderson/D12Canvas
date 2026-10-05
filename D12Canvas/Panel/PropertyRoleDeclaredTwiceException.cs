namespace D12Canvas.Panel;

// Two properties on one TProps type declare the same role. The role is the key a cross-type row
// merges and commits on, so the second property would be silently unreachable - thrown at
// registration time instead, naming both properties.
public sealed class PropertyRoleDeclaredTwiceException : Exception
{
    public Type PropsType { get; }
    public PropertyRole Role { get; }
    public string FirstPropertyName { get; }
    public string SecondPropertyName { get; }

    public PropertyRoleDeclaredTwiceException(
        Type propsType,
        PropertyRole role,
        string firstPropertyName,
        string secondPropertyName
    )
        : base(
            $"{propsType.Name} declares the {role} role on both {firstPropertyName} and "
                + $"{secondPropertyName}; a type may declare each role once."
        )
    {
        PropsType = propsType;
        Role = role;
        FirstPropertyName = firstPropertyName;
        SecondPropertyName = secondPropertyName;
    }
}
