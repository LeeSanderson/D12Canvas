namespace D12Canvas.Registration;

// An asset reference is a string holding "asset:<id>" or an ordinary URL, so [AssetReference]
// on any other property type is a declaration error - thrown by AssetReferenceSchema.DiscoverFrom
// at registration time rather than failing when the reference is first resolved.
public sealed class AssetReferenceStringRequiredException : Exception
{
    public Type PropsType { get; }
    public string PropertyName { get; }

    public AssetReferenceStringRequiredException(Type propsType, string propertyName)
        : base(
            $"{propsType.Name}.{propertyName} declares [AssetReference] but is not a string property."
        )
    {
        PropsType = propsType;
        PropertyName = propertyName;
    }
}
