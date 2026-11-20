using D12Canvas.Panel;

namespace D12Canvas.Tests;

internal sealed record PropsWithMissingDropdownOptions(
    [property: PanelEditable(EditorKind.Dropdown)] string Mode = ""
);
