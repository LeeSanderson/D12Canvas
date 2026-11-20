namespace D12Canvas.Panel;

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
