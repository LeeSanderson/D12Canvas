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
    IReadOnlyList<EditableProperty>? EditableProperties = null
);
