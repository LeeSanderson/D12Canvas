using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

public sealed record TextProps(
    string Text,
    [property: PanelEditable(EditorKind.Color, PropertyRole.TextColour)] string? Color,
    [property: PanelEditable(EditorKind.Number, PropertyRole.FontSize)] double FontSize,
    [property: PanelEditable(
        EditorKind.Dropdown,
        PropertyRole.FontWeight,
        Options = new[] { "normal", "bold" }
    )]
        string FontWeight,
    [property: PanelEditable(
        EditorKind.Dropdown,
        PropertyRole.TextAlign,
        Options = new[] { "left", "center", "right" }
    )]
        string TextAlign
);
