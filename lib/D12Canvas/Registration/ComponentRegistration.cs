using System.Reflection;
using D12Canvas.Panel;

namespace D12Canvas.Registration;

public sealed record ComponentRegistration(
    string Key,
    Type ComponentType,
    Type PropsType,
    string DisplayName,
    string AccessibleName,
    object DefaultProps,
    Glyph? Icon,
    string Role,
    ComponentSize? DefaultSize,
    string? Category,
    IReadOnlyList<EditableProperty>? EditableProperties = null,
    IReadOnlyList<PropertyInfo>? AssetReferences = null,
    Func<object, bool>? IsEmpty = null
)
{
    public bool CountsAsEmpty(object props) => IsEmpty?.Invoke(props) == true;

    internal Glyph IconOrDefault => Icon ?? Glyphs.Component;

    public bool IsInlineEditable => typeof(IInlineEditable).IsAssignableFrom(ComponentType);
}
