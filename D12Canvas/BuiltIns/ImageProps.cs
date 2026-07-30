using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// Url is reserved for EditorKind.Custom - a file picker, ADR 0008's own worked example of what the
// built-in EditorKind set can't express (ticket 58). Fit awaits EditorKind.Dropdown (ticket 57);
// AltText is Text-kind, already shipped by ticket 56.
public sealed record ImageProps(
    string Url,
    [property: PanelEditable(EditorKind.Text)] string AltText,
    string Fit
);
