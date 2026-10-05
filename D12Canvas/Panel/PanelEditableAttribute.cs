namespace D12Canvas.Panel;

// Declares a TProps property as editable through the property panel, mirroring the registration
// contract's precedent of authors declaring metadata via attributes rather than hand-writing panel
// markup. The registration builder (ComponentRegistrationBuilder.EditableProperties) can override
// whatever this attribute declares - attributes only set the default schema.
[AttributeUsage(AttributeTargets.Property)]
public sealed class PanelEditableAttribute : Attribute
{
    public EditorKind Kind { get; }

    // Only meaningful (and required, see EditablePropertySchema.DiscoverFrom) for EditorKind.Dropdown -
    // the fixed set of choices the <select> renders.
    public string[]? Options { get; set; }

    // The role this property plays, from the closed PropertyRole set. A role is what admits the
    // property to the property bar and what merges it with another type's property in a cross-type
    // multi-selection (never inferred from name alone). Each role declares the EditorKind and CLR
    // type it expects; PropertyRoleValidator enforces agreement at registration time.
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
