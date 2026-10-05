using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

public sealed record RectangleProps(
    [property: PanelEditable(EditorKind.Color, PropertyRole.Fill)] string FillColor,
    [property: PanelEditable(EditorKind.Color, PropertyRole.Stroke)] string StrokeColor,
    [property: PanelEditable(EditorKind.Number, PropertyRole.StrokeWidth)] double StrokeWidth
);
