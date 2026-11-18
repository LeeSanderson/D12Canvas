using D12Canvas.Panel;
using Microsoft.AspNetCore.Components;

namespace D12Canvas.Registration;

public sealed class D12CanvasOptions
{
    private readonly ComponentRegistry _registry;

    public D12CanvasOptions()
        : this(new ComponentRegistry(), new ColourPresetCatalog()) { }

    internal D12CanvasOptions(ComponentRegistry registry, ColourPresetCatalog colourPresets)
    {
        _registry = registry;
        ColourPresetCatalog = colourPresets;
    }

    public IComponentRegistry Registry => _registry;

    internal ColourPresetCatalog ColourPresetCatalog { get; }

    // The swatches every colour role offers in the Property bar's colour flyout, in order. An
    // empty list removes the grid. Setting a null list, or a preset whose Hex is not #RRGGBB,
    // throws.
    public IReadOnlyList<ColourPreset> ColourPresets
    {
        get => ColourPresetCatalog.Presets;
        set => ColourPresetCatalog.Presets = value;
    }

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

        var editableProperties =
            builder.EditableProperties ?? EditablePropertySchema.DiscoverFrom(typeof(TProps));

        PropertyRoleValidator.Validate(editableProperties);

        var isEmpty = builder.IsEmpty;
        var registration = new ComponentRegistration(
            key,
            typeof(TComponent),
            typeof(TProps),
            builder.DisplayName,
            builder.AccessibleName,
            builder.DefaultProps,
            builder.Icon,
            builder.Role,
            builder.DefaultSize,
            builder.Category,
            editableProperties,
            AssetReferenceSchema.DiscoverFrom(typeof(TProps)),
            isEmpty is null ? null : props => props is TProps typed && isEmpty(typed)
        );

        if (isEmpty is not null && !registration.IsInlineEditable)
        {
            throw new EmptyPredicateWithoutInlineEditException(key, typeof(TComponent));
        }

        _registry.Register(registration);

        return this;
    }
}
