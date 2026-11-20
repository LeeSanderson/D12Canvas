using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

public sealed record StickyNoteProps(
    string Text,
    [property: PanelEditable(EditorKind.Color, PropertyRole.Fill)] string Color,
    [property: PanelEditable(EditorKind.Color, PropertyRole.TextColour)] string TextColor,
    [property: PanelEditable(EditorKind.Number, PropertyRole.FontSize)] double FontSize,
    [property: PanelEditable(
        EditorKind.Dropdown,
        PropertyRole.TextAlign,
        Options = new[] { "left", "center", "right" }
    )]
        string TextAlign = "center",
    [property: PanelEditable(
        EditorKind.Dropdown,
        PropertyRole.VerticalAlign,
        Options = new[] { "top", "middle", "bottom" }
    )]
        string VerticalAlign = "middle"
);
