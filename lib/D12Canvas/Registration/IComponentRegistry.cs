namespace D12Canvas.Registration;

public interface IComponentRegistry
{
    ComponentRegistration Resolve(string key);
    IReadOnlyList<ComponentRegistration> All { get; }
}
