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

    // Opts this property into cross-type matching - a multi-selection spanning two different
    // component types merges their properties into one editable row only when both carry the
    // same SharedTag (never inferred from name alone). SharedPropertyValidator enforces, at
    // registration time, that every property sharing a tag agrees in EditorKind and CLR type.
    public string? SharedTag { get; set; }

    public PanelEditableAttribute(EditorKind kind)
    {
        Kind = kind;
    }
}
