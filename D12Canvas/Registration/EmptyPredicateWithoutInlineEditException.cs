namespace D12Canvas.Registration;

public sealed class EmptyPredicateWithoutInlineEditException : Exception
{
    public string Key { get; }

    public EmptyPredicateWithoutInlineEditException(string key, Type componentType)
        : base(
            $"Component registration for key '{key}' declares IsEmpty, but {componentType.Name} "
                + $"does not implement {nameof(IInlineEditable)}, so no inline edit ever ends on it."
        )
    {
        Key = key;
    }
}
