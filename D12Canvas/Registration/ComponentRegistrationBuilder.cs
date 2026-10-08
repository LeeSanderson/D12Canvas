using D12Canvas.Panel;

namespace D12Canvas.Registration;

public sealed class ComponentRegistrationBuilder<TProps>
    where TProps : class
{
    public string? DisplayName { get; set; }
    public string? AccessibleName { get; set; }
    public TProps? DefaultProps { get; set; }
    public string? Icon { get; set; }
    public string Role { get; set; } = "group";
    public ComponentSize? DefaultSize { get; set; }
    public string? Category { get; set; }

    // null means "use whatever TProps's own [PanelEditable] attributes declare" - set this to add,
    // override, or redefine the property panel's schema at registration time instead.
    public IReadOnlyList<EditableProperty>? EditableProperties { get; set; }

    // Says when an instance's committed Props count as empty. When an inline edit ends with the
    // predicate true, the canvas removes the instance, so set it only for a type whose empty
    // content is invisible. Left null, an instance is never removed for being empty. Only a
    // component type implementing IInlineEditable may declare one, since nothing else is edited.
    public Func<TProps, bool>? IsEmpty { get; set; }
}
