namespace D12Canvas.Registration;

public sealed class UnknownComponentKeyException : Exception
{
    public string Key { get; }

    public UnknownComponentKeyException(string key)
        : base($"No component type is registered under the key '{key}'.")
    {
        Key = key;
    }
}
