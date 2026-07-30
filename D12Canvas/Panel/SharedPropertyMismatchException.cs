namespace D12Canvas.Panel;

// ADR 0008/ticket 59: two properties on different TProps types can't share a SharedTag unless they
// agree in EditorKind and CLR property type - thrown by SharedPropertyValidator at registration
// time, before the mismatched type ever reaches the registry, rather than letting the property
// panel's cross-type merge silently pick one shape or the other.
public sealed class SharedPropertyMismatchException : Exception
{
    public string SharedTag { get; }
    public Type ExistingPropsType { get; }
    public string ExistingPropertyName { get; }
    public Type NewPropsType { get; }
    public string NewPropertyName { get; }

    public SharedPropertyMismatchException(
        string sharedTag,
        Type existingPropsType,
        string existingPropertyName,
        Type newPropsType,
        string newPropertyName
    )
        : base(
            $"SharedTag '{sharedTag}' is declared on {existingPropsType.Name}.{existingPropertyName} "
                + $"and {newPropsType.Name}.{newPropertyName} with mismatched EditorKind or property type."
        )
    {
        SharedTag = sharedTag;
        ExistingPropsType = existingPropsType;
        ExistingPropertyName = existingPropertyName;
        NewPropsType = newPropsType;
        NewPropertyName = newPropertyName;
    }
}
