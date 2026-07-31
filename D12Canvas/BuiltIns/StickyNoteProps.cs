using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// Text is content, edited inline/WYSIWYG - never panel-editable.
public sealed record StickyNoteProps(
    string Text,
    [property: PanelEditable(EditorKind.Color)] string Color,
    [property: PanelEditable(EditorKind.Color)] string TextColor,
    [property: PanelEditable(EditorKind.Number)] double FontSize
);
