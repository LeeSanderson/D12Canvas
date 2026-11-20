using D12Canvas.Panel;
using D12Canvas.Registration;

namespace D12Canvas.BuiltIns;

public sealed record ImageProps(
    [property: AssetReference] string Url,
    [property: PanelEditable(EditorKind.Text)] string AltText,
    [property: PanelEditable(EditorKind.Dropdown, Options = new[] { "cover", "contain", "fill" })]
        string Fit
);
