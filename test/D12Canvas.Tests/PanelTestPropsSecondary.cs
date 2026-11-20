using D12Canvas.Panel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace D12Canvas.Tests;

internal sealed record PanelTestPropsSecondary(
    [property: PanelEditable(EditorKind.Color, PropertyRole.Fill)] string AccentColor = "#000000",
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
