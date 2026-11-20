namespace D12Canvas.Panel;

[AttributeUsage(AttributeTargets.Property)]
public sealed class PanelEditableAttribute : Attribute
{
    public EditorKind Kind { get; }

    public string[]? Options { get; set; }

    public PropertyRole? Role { get; }

    public PanelEditableAttribute(EditorKind kind)
    {
        Kind = kind;
    }

    public PanelEditableAttribute(EditorKind kind, PropertyRole role)
    {
        Kind = kind;
        Role = role;
    }
}
