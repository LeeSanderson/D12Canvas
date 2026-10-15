using D12Canvas.Panel;

namespace D12Canvas.Tests;

// A minimal fixture for the role-validation tests: declares the Fill role, which expects
// EditorKind.Color on a string, but carries EditorKind.Text.
internal sealed record PropsWithRoleKindMismatch(
    [property: PanelEditable(EditorKind.Text, PropertyRole.Fill)] string Tint = ""
);
