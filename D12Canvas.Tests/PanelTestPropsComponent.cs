using D12Canvas.Panel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace D12Canvas.Tests;

// A second props type alongside TestProps: PropertyPanelTests needs one shape that carries a
// Text-type *content* field (Content, no attribute - excluded from the panel, like StickyNote's
// Text), a genuinely panel-editable Text field (Label), and a Number field (Count), all at once.
internal sealed record PanelTestProps(
    string Content,
    [property: PanelEditable(EditorKind.Text)] string Label,
    [property: PanelEditable(EditorKind.Number)] double Count
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
