using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// FillColor/StrokeColor await EditorKind.Color (ticket 57) - StrokeWidth is Number-kind, already
// shipped by ticket 56.
public sealed record RectangleProps(
    string FillColor,
    string StrokeColor,
    [property: PanelEditable(EditorKind.Number)] double StrokeWidth
);
