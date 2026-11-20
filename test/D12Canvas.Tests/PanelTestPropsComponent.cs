using D12Canvas.Panel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace D12Canvas.Tests;

internal sealed record PanelTestProps(
    string Content,
    [property: PanelEditable(EditorKind.Text)] string Label,
    [property: PanelEditable(EditorKind.Number)] double Count,
    [property: PanelEditable(EditorKind.Color, PropertyRole.Fill)] string? Tint = "#000000",
    [property: PanelEditable(EditorKind.Checkbox)] bool Flag = false,
    [property: PanelEditable(EditorKind.Dropdown, Options = new[] { "a", "b", "c" })]
        string Mode = "a",
    string CustomValue = ""
);

internal sealed class PanelTestPropsComponent : ComponentBase
{
    [Parameter]
    public PanelTestProps Props { get; set; } = new("", "", 0);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "panel-test-props-component");
        builder.AddContent(2, Props.Content);
        builder.CloseElement();
    }
}
