namespace D12Canvas.Registration;

public sealed class DuplicateComponentKeyException : Exception
{
    public string Key { get; }

    public DuplicateComponentKeyException(string key)
        : base($"A component type is already registered under the key '{key}'.")
    {
        Key = key;
    }
}
