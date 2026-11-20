using D12Canvas.Panel;

namespace D12Canvas.Tests;

internal sealed record PropsWithRoleKindMismatch(
    [property: PanelEditable(EditorKind.Text, PropertyRole.Fill)] string Tint = ""
);
