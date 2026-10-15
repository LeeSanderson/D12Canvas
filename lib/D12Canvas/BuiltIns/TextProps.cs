using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// Text is content, edited inline/WYSIWYG - never panel-editable. Color is null when nobody has
// chosen one, and the component then paints the board's text token so the default reads on both
// themes; a non-null value is the author's choice and is taken literally in both. FontWeight/
// TextAlign's option sets are curated CSS values covering every value already in use across the
// demo app and test suite, not the full font-weight/text-align vocabulary.
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
