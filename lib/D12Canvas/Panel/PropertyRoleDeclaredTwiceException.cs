namespace D12Canvas.Panel;

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
