using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// Text is content, edited inline/WYSIWYG (ticket 43/ADR 0008) - never panel-editable. Color/
// TextColor await EditorKind.Color (ticket 57); FontSize is Number-kind, already shipped by
// ticket 56.
public sealed record StickyNoteProps(
    string Text,
    string Color,
    string TextColor,
    [property: PanelEditable(EditorKind.Number)] double FontSize
);
