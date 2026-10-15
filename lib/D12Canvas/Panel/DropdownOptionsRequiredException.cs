namespace D12Canvas.Panel;

// A Dropdown-kind property with no choices can't render a usable <select> - thrown by
// EditablePropertySchema.DiscoverFrom at discovery/registration time rather than surfacing as an
// empty control in the panel.
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
