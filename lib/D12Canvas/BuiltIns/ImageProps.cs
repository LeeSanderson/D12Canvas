using D12Canvas.Panel;
using D12Canvas.Registration;

namespace D12Canvas.BuiltIns;

// Url is edited through a Custom editor, a file picker, which the registration declares, since an
// attribute cannot carry one. It may hold an asset reference in place of an ordinary URL, and an
// empty Url is the "No image" placeholder rather than a defect.
// Fit's option set is CSS object-fit's most useful values, curated rather than exhaustive
// (matching Options being declarative metadata, not a CLR enum).
public sealed record ImageProps(
    [property: AssetReference] string Url,
    [property: PanelEditable(EditorKind.Text)] string AltText,
    [property: PanelEditable(EditorKind.Dropdown, Options = new[] { "cover", "contain", "fill" })]
        string Fit
);
