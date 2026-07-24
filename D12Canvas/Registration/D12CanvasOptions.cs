using Microsoft.AspNetCore.Components;

namespace D12Canvas.Registration;

public sealed class D12CanvasOptions
{
    private readonly ComponentRegistry _registry;

    public D12CanvasOptions()
        : this(new ComponentRegistry()) { }

    internal D12CanvasOptions(ComponentRegistry registry)
    {
        _registry = registry;
    }

    public IComponentRegistry Registry => _registry;

    public D12CanvasOptions RegisterComponent<TComponent, TProps>(
        string key,
        Action<ComponentRegistrationBuilder<TProps>> configure
    )
        where TComponent : IComponent
        where TProps : class
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A component key must not be empty.", nameof(key));
        }

        var builder = new ComponentRegistrationBuilder<TProps>();
        configure(builder);

        if (string.IsNullOrWhiteSpace(builder.DisplayName))
        {
            throw new ComponentRegistrationException(key, nameof(builder.DisplayName));
        }

        if (string.IsNullOrWhiteSpace(builder.AccessibleName))
        {
            throw new ComponentRegistrationException(key, nameof(builder.AccessibleName));
        }

        if (builder.DefaultProps is null)
        {
            throw new ComponentRegistrationException(key, nameof(builder.DefaultProps));
        }

        _registry.Register(
            new ComponentRegistration(
                key,
                typeof(TComponent),
                typeof(TProps),
                builder.DisplayName,
                builder.AccessibleName,
                builder.DefaultProps,
                builder.Icon,
                builder.Role,
                builder.DefaultSize,
                builder.Category
            )
        );

        return this;
    }
}
