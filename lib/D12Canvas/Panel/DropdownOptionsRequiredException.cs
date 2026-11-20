namespace D12Canvas.Panel;

public sealed class DropdownOptionsRequiredException : Exception
{
    public Type PropsType { get; }
    public string PropertyName { get; }

    public DropdownOptionsRequiredException(Type propsType, string propertyName)
        : base(
            $"{propsType.Name}.{propertyName} declares [PanelEditable(EditorKind.Dropdown)] but no Options."
        )
    {
        PropsType = propsType;
        PropertyName = propertyName;
    }
}
