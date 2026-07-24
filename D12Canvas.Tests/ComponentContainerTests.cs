using Bunit;
using Xunit;

namespace D12Canvas.Tests;

public class ComponentContainerTests : ComponentTestBase
{
    public ComponentContainerTests()
    {
        var module = JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");
        module.SetupVoid("registerClickOutside").SetVoidResult();
        module.SetupVoid("unregisterClickOutside").SetVoidResult();
    }

    [Fact]
    public void ComponentContainer_ImportsColocatedJsModule()
    {
        var cut = RenderComponent<ComponentContainer>();

        Assert.Contains("view-mode", cut.Find(".component-container").ClassList);
    }

    [Fact]
    public void ComponentContainer_ClickOutside_ExitsEditMode()
    {
        var cut = RenderComponent<ComponentContainer>(parameters =>
            parameters.Add(p => p.InitialEditMode, true)
        );

        Assert.Contains("edit-mode", cut.Find(".component-container").ClassList);

        cut.InvokeAsync(() => cut.Instance.OnClickOutside());

        Assert.Contains("view-mode", cut.Find(".component-container").ClassList);
    }
}
