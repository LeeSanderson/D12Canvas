using Bunit;
using D12Canvas.Model;
using Microsoft.AspNetCore.Components.Web;
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
        var container = Render<ComponentContainer>();

        Assert.Contains("view-mode", container.Find(".component-container").ClassList);
    }

    [Fact]
    public void ComponentContainer_ClickOutside_ExitsEditMode()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.InitialEditMode, true)
        );

        Assert.Contains("edit-mode", container.Find(".component-container").ClassList);

        container.InvokeAsync(() => container.Instance.OnClickOutside());

        Assert.Contains("view-mode", container.Find(".component-container").ClassList);
    }

    [Fact]
    public void SelectedInstanceRendersAriaSelectedAndTheSelectedClass()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.IsSelected, true)
        );

        var element = container.Find(".component-container");
        Assert.Equal("true", element.GetAttribute("aria-selected"));
        Assert.Contains("selected", element.ClassList);
    }

    [Fact]
    public void UnselectedInstanceOmitsAriaSelectedAndTheSelectedClass()
    {
        var container = Render<ComponentContainer>();

        var element = container.Find(".component-container");
        Assert.Null(element.GetAttribute("aria-selected"));
        Assert.DoesNotContain("selected", element.ClassList);
    }

    [Fact]
    public void ClickingTheContainerInvokesOnSelect()
    {
        var selectCount = 0;
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.OnSelect, () => selectCount++)
        );

        container.Find(".component-container").Click();

        Assert.Equal(1, selectCount);
    }

    [Fact]
    public void DraggingASelectedContainerInvokesOnMovedWithTheFinalBounds()
    {
        Bounds? movedTo = null;
        var container = Render<ComponentContainer>(parameters =>
            parameters
                .Add(p => p.IsSelected, true)
                .Add(p => p.X, 100)
                .Add(p => p.Y, 100)
                .Add(p => p.Width, 50)
                .Add(p => p.Height, 50)
                .Add(p => p.OnMoved, (Bounds bounds) => movedTo = bounds)
        );

        var element = container.Find(".component-container");
        element.MouseDown(new MouseEventArgs { ClientX = 0, ClientY = 0 });
        element.MouseMove(new MouseEventArgs { ClientX = 30, ClientY = 10 });
        element.MouseUp(new MouseEventArgs { ClientX = 30, ClientY = 10 });

        Assert.Equal(new Bounds(130, 110, 50, 50), movedTo);
    }

    [Fact]
    public void DraggingAnUnselectedContainerDoesNotInvokeOnMoved()
    {
        var invoked = false;
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.OnMoved, (Bounds _) => invoked = true)
        );

        var element = container.Find(".component-container");
        element.MouseDown(new MouseEventArgs { ClientX = 0, ClientY = 0 });
        element.MouseMove(new MouseEventArgs { ClientX = 30, ClientY = 10 });
        element.MouseUp(new MouseEventArgs { ClientX = 30, ClientY = 10 });

        Assert.False(invoked);
    }
}
