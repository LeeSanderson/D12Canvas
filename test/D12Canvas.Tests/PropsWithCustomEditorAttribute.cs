using D12Canvas.Panel;

namespace D12Canvas.Tests;

// A minimal fixture solely for D12CanvasOptionsTests' Custom-kind-via-attribute throw test -
// TestComponentDouble accepts any TProps, so no paired component double is needed.
internal sealed record PropsWithCustomEditorAttribute(
    [property: PanelEditable(EditorKind.Custom)] string Value = ""
);
