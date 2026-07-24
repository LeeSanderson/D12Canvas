namespace D12Canvas.Registration;

public sealed class ComponentRegistrationException : Exception
{
    public string Key { get; }
    public string MissingField { get; }

    public ComponentRegistrationException(string key, string missingField)
        : base($"Component registration for key '{key}' is missing required '{missingField}'.")
    {
        Key = key;
        MissingField = missingField;
    }
}
