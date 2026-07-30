namespace D12Canvas.Panel;

// ADR 0008: declares a TProps property as editable through the property panel, mirroring the
// registration contract's precedent of authors declaring metadata via attributes rather than
// hand-writing panel markup. The registration builder (ComponentRegistrationBuilder.EditableProperties)
// can override whatever this attribute declares - attributes only set the default schema.
[AttributeUsage(AttributeTargets.Property)]
public sealed class PanelEditableAttribute : Attribute
{
    public EditorKind Kind { get; }

    public PanelEditableAttribute(EditorKind kind)
    {
        Kind = kind;
    }
}
