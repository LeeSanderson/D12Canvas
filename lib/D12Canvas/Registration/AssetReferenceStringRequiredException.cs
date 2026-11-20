namespace D12Canvas.Registration;

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
