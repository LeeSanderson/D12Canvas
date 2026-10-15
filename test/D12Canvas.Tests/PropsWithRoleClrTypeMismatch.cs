using D12Canvas.Panel;

namespace D12Canvas.Tests;

// A minimal fixture for the role-validation tests: declares the Fill role, which expects
// EditorKind.Color on a string, but is an int.
internal sealed record PropsWithRoleClrTypeMismatch(
    [property: PanelEditable(EditorKind.Color, PropertyRole.Fill)] int Tint = 0
);
