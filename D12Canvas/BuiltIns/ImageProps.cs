using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// Url is reserved for EditorKind.Custom - a file picker, ADR 0008's own worked example of what the
// built-in EditorKind set can't express (ticket 58). AltText is Text-kind (ticket 56); Fit is
// Dropdown-kind (ticket 57) - the option set is CSS object-fit's most useful values, curated
// rather than exhaustive (matches ADR 0008's Options being declarative metadata, not a CLR enum).
public sealed record ImageProps(
    string Url,
    [property: PanelEditable(EditorKind.Text)] string AltText,
    [property: PanelEditable(EditorKind.Dropdown, Options = new[] { "cover", "contain", "fill" })]
        string Fit
);
