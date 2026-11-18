using D12Canvas.Panel;

namespace D12Canvas.Demo.Components;

// Outline is a plain Dropdown with no option glyphs, so the property panel draws it as a <select>.
public sealed record DemoShapeProps(
    string Label,
    [property: PanelEditable(EditorKind.Dropdown, Options = new[] { "solid", "dashed", "dotted" })]
        string Outline = "solid"
);
