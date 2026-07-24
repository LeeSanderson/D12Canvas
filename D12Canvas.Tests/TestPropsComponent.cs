using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace D12Canvas.Tests;

internal sealed class TestPropsComponent : ComponentBase
{
    [Parameter]
    public TestProps Props { get; set; } = new();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "test-props-component");
        builder.AddContent(2, Props.Text);
        builder.CloseElement();
    }
}
