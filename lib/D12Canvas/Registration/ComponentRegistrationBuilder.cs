using D12Canvas.Panel;

namespace D12Canvas.Registration;

public sealed class ComponentRegistrationBuilder<TProps>
    where TProps : class
{
    public string? DisplayName { get; set; }
    public string? AccessibleName { get; set; }
    public TProps? DefaultProps { get; set; }
    public Glyph? Icon { get; set; }
    public string Role { get; set; } = "group";
    public ComponentSize? DefaultSize { get; set; }
    public string? Category { get; set; }

    public IReadOnlyList<EditableProperty>? EditableProperties { get; set; }

    public Func<TProps, bool>? IsEmpty { get; set; }
}
