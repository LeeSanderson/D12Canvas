using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// Text is content, edited inline/WYSIWYG (ticket 43/ADR 0008) - never panel-editable. Color awaits
// EditorKind.Color and FontWeight/TextAlign await EditorKind.Dropdown (ticket 57); FontSize is
// Number-kind, already shipped by ticket 56.
public sealed record TextProps(
    string Text,
    string Color,
    [property: PanelEditable(EditorKind.Number)] double FontSize,
    string FontWeight,
    string TextAlign
);
