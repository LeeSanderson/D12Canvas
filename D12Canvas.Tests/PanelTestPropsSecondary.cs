using D12Canvas.Panel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace D12Canvas.Tests;

// A second, distinct component type for PropertyPanelTests' cross-type multi-select coverage.
// AccentColor carries the same SharedTag ("color") as PanelTestProps.Tint, with a matching
// EditorKind/CLR type, so SharedPropertyValidator accepts the pair at registration; Note carries
// no tag at all, standing in for a property that must NOT leak into a cross-type selection.
internal sealed record PanelTestPropsSecondary(
    [property: PanelEditable(EditorKind.Color, SharedTag = "color")] string AccentColor = "#000000",
    [property: PanelEditable(EditorKind.Text)] string Note = ""
);

internal sealed class PanelTestPropsSecondaryComponent : ComponentBase
{
    [Parameter]
    public PanelTestPropsSecondary Props { get; set; } = new();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "panel-test-props-secondary-component");
        builder.CloseElement();
    }
}
