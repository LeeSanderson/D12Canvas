using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// Text is content, edited inline/WYSIWYG - never panel-editable. Color is the note's background,
// so it plays Fill; TextColor is the foreground and plays TextColour. The alignments default to
// centred here as well as in the registration, so a board saved before they existed loads centred.
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
