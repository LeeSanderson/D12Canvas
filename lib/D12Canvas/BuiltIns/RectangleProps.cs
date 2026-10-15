using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// FillColor and StrokeColor are null when nobody has chosen them, and the component then paints
// the board's fill and stroke tokens so the default reads on both themes; a non-null value is the
// author's choice and is taken literally in both.
public sealed record RectangleProps(
    [property: PanelEditable(EditorKind.Color, PropertyRole.Fill)] string? FillColor,
    [property: PanelEditable(EditorKind.Color, PropertyRole.Stroke)] string? StrokeColor,
    [property: PanelEditable(EditorKind.Number, PropertyRole.StrokeWidth)] double StrokeWidth
);
