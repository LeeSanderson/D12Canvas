namespace D12Canvas.Panel;

// ADR 0008: EditorKind.Custom's RenderFragment can't be expressed as an attribute argument
// (attributes require compile-time constants), so [PanelEditable(EditorKind.Custom)] can never
// supply one. Thrown by EditablePropertySchema.DiscoverFrom at discovery/registration time; a
// Custom-kind property must be declared via ComponentRegistrationBuilder.EditableProperties
// instead (ticket 58).
public sealed class CustomEditorRequiredException : Exception
{
    public Type PropsType { get; }
    public string PropertyName { get; }

    public CustomEditorRequiredException(Type propsType, string propertyName)
        : base(
            $"{propsType.Name}.{propertyName} declares [PanelEditable(EditorKind.Custom)], which requires a RenderFragment that only ComponentRegistrationBuilder.EditableProperties can supply."
        )
    {
        PropsType = propsType;
        PropertyName = propertyName;
    }
}
