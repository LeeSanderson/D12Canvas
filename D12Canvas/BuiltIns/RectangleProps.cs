using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

public sealed record RectangleProps(
    [property: PanelEditable(EditorKind.Color)] string FillColor,
    [property: PanelEditable(EditorKind.Color)] string StrokeColor,
    [property: PanelEditable(EditorKind.Number)] double StrokeWidth
);
