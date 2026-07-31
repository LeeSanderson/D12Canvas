using D12Canvas.Panel;

namespace D12Canvas.BuiltIns;

// Url is reserved for EditorKind.Custom - a file picker, an example of what the built-in
// EditorKind set can't express. Fit's option set is CSS object-fit's most useful values, curated
// rather than exhaustive (matching Options being declarative metadata, not a CLR enum).
public sealed record ImageProps(
    string Url,
    [property: PanelEditable(EditorKind.Text)] string AltText,
    [property: PanelEditable(EditorKind.Dropdown, Options = new[] { "cover", "contain", "fill" })]
        string Fit
);
