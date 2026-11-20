using D12Canvas.Panel;

namespace D12Canvas.Tests;

internal sealed record PropsWithCustomEditorAttribute(
    [property: PanelEditable(EditorKind.Custom)] string Value = ""
);
