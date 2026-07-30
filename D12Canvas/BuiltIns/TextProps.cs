using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// Text is content, edited inline/WYSIWYG (ticket 43/ADR 0008) - never panel-editable. FontSize is
// Number-kind (ticket 56); Color is Color-kind and FontWeight/TextAlign are Dropdown-kind
// (ticket 57) - the option sets are curated CSS values covering every value already in use
// across the demo app and test suite, not the full font-weight/text-align vocabulary.
public sealed record TextProps(
    string Text,
    [property: PanelEditable(EditorKind.Color)] string Color,
    [property: PanelEditable(EditorKind.Number)] double FontSize,
    [property: PanelEditable(EditorKind.Dropdown, Options = new[] { "normal", "bold" })]
        string FontWeight,
    [property: PanelEditable(EditorKind.Dropdown, Options = new[] { "left", "center", "right" })]
        string TextAlign
);
