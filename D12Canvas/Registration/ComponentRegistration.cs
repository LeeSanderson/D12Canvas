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
    string? Icon,
    string Role,
    ComponentSize? DefaultSize,
    string? Category,
    // null (the default for every existing call site, including every existing test's
    // ComponentRegistration) means "no editable properties" - D12CanvasOptions.RegisterComponent
    // always resolves this to a concrete (possibly empty) list before it reaches here.
    IReadOnlyList<EditableProperty>? EditableProperties = null,
    // The TProps properties declared [AssetReference], discovered by RegisterComponent in the same
    // pass as EditableProperties. null means none, and a type with none costs nothing at render.
    IReadOnlyList<PropertyInfo>? AssetReferences = null,
    Func<object, bool>? IsEmpty = null
)
{
    public bool CountsAsEmpty(object props) => IsEmpty?.Invoke(props) == true;

    // Known before any instance of the type is mounted, so the canvas can tell a type that
    // declines editing from one that has not mounted yet.
    public bool IsInlineEditable => typeof(IInlineEditable).IsAssignableFrom(ComponentType);
}
