using D12Canvas.Panel;

namespace D12Canvas.Tests;

// A minimal fixture solely for D12CanvasOptionsTests' Dropdown-with-no-Options throw test -
// TestComponentDouble accepts any TProps, so no paired component double is needed.
internal sealed record PropsWithMissingDropdownOptions(
    [property: PanelEditable(EditorKind.Dropdown)] string Mode = ""
);
