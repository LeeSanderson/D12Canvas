using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// StrokeWidth is Number-kind (ticket 56); FillColor/StrokeColor are Color-kind (ticket 57).
public sealed record RectangleProps(
    [property: PanelEditable(EditorKind.Color)] string FillColor,
    [property: PanelEditable(EditorKind.Color)] string StrokeColor,
    [property: PanelEditable(EditorKind.Number)] double StrokeWidth
);
