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
    string? Category
);
