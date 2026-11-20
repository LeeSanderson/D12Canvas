using D12Canvas.Panel;

namespace D12Canvas.Demo.Components;

public sealed record DemoShapeProps(
    string Label,
    [property: PanelEditable(EditorKind.Dropdown, Options = new[] { "solid", "dashed", "dotted" })]
        string Outline = "solid"
);
