using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// Text is content, edited inline/WYSIWYG (ticket 43/ADR 0008) - never panel-editable. FontSize is
// Number-kind (ticket 56); Color/TextColor are Color-kind (ticket 57).
public sealed record StickyNoteProps(
    string Text,
    [property: PanelEditable(EditorKind.Color)] string Color,
    [property: PanelEditable(EditorKind.Color)] string TextColor,
    [property: PanelEditable(EditorKind.Number)] double FontSize
);
