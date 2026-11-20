using D12Canvas.Panel;

namespace D12Canvas.Tests;

internal sealed record PropsWithRoleClrTypeMismatch(
    [property: PanelEditable(EditorKind.Color, PropertyRole.Fill)] int Tint = 0
);
