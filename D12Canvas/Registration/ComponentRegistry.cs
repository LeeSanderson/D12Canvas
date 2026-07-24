namespace D12Canvas.Registration;

public sealed class ComponentRegistry : IComponentRegistry
{
    private readonly Dictionary<string, ComponentRegistration> _registrations = new();

    public void Register(ComponentRegistration registration)
    {
        if (!_registrations.TryAdd(registration.Key, registration))
        {
            throw new DuplicateComponentKeyException(registration.Key);
        }
    }

    public ComponentRegistration Resolve(string key)
    {
        if (!_registrations.TryGetValue(key, out var registration))
        {
            throw new UnknownComponentKeyException(key);
        }

        return registration;
    }
}
