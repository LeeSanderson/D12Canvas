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

    // ADR 0008: null means "use whatever TProps's own [PanelEditable] attributes declare" - set
    // this to add, override, or redefine the property panel's schema at registration time instead.
    public IReadOnlyList<EditableProperty>? EditableProperties { get; set; }
}
