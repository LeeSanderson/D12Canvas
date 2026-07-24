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
}
