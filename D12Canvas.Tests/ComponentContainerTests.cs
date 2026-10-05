using Bunit;
using D12Canvas.Model;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace D12Canvas.Tests;

public class ComponentContainerTests : ComponentTestBase
{
    public ComponentContainerTests()
    {
        SetupComponentContainerJsModule();
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
    public void AnAddressableContainerCarriesNoUnaddressableMarker()
    {
        var container = Render<ComponentContainer>();

        Assert.False(container.Find(".component-container").HasAttribute("data-d12-unaddressable"));
    }

    [Fact]
    public void AContainerThatIsNotAddressableCarriesTheMarkerThePointerListenerReads()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.Addressable, false)
        );

        Assert.Equal(
            "true",
            container.Find(".component-container").GetAttribute("data-d12-unaddressable")
        );
    }

    [Fact]
    public void EveryInstanceRendersAllFourStandardPortsWithDirectionalClasses()
    {
        // bUnit has no real browser layout/pseudo-class engine, so "at their border centers" and
        // "hidden otherwise" aren't checkable here - the class names below are what the
        // stylesheet keys its percentage-of-box positioning and hover/selection opacity off of,
        // and PortsVisualTests.cs proves the resulting on-screen behavior in a real browser.
        var container = Render<ComponentContainer>();

        Assert.Equal(4, container.FindAll(".port").Count);
        Assert.Single(container.FindAll(".port-top"));
        Assert.Single(container.FindAll(".port-right"));
        Assert.Single(container.FindAll(".port-bottom"));
        Assert.Single(container.FindAll(".port-left"));
    }

    [Fact]
    public void PortsSurviveAResizeRerenderUnchanged()
    {
        // Ports are positioned via plain CSS percentages of the container's own box, not computed
        // from Bounds in C#, so re-rendering the same instance at different Width/Height should
        // touch nothing about them - this exercises that actual update path (including
        // ComponentContainer's own ShouldRender override), rather than just asserting on two
        // independent fresh renders.
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.Width, 200).Add(p => p.Height, 150)
        );

        container.Render(parameters => parameters.Add(p => p.Width, 60).Add(p => p.Height, 400));

        Assert.Equal(4, container.FindAll(".port").Count);
        Assert.Single(container.FindAll(".port-top"));
        Assert.Single(container.FindAll(".port-right"));
        Assert.Single(container.FindAll(".port-bottom"));
        Assert.Single(container.FindAll(".port-left"));
    }

    [Fact]
    public void AZIndexOnlyChangeReRendersTheContainersStyle()
    {
        // A layering command changes only ZIndex, with Bounds/selection/Props/custom ports all
        // unchanged - ComponentContainer's own ShouldRender override must still treat that as a
        // real change, or a stacking change would silently fail to render immediately until some
        // unrelated parameter happened to change too.
        var container = Render<ComponentContainer>(parameters => parameters.Add(p => p.ZIndex, 2));

        container.Render(parameters => parameters.Add(p => p.ZIndex, 9));

        Assert.Contains("z-index: 9", container.Find(".component-container").GetAttribute("style"));
    }

    [Fact]
    public void SelectedInstanceRendersResizeHandles()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.IsSelected, true)
        );

        Assert.Equal(8, container.FindAll(".resize-handle").Count);
    }

    [Fact]
    public void UnselectedInstanceOutsideEditModeOmitsResizeHandles()
    {
        var container = Render<ComponentContainer>();

        Assert.Empty(container.FindAll(".resize-handle"));
    }

    [Fact]
    public void EditModeInstanceRendersResizeHandlesEvenWhenUnselected()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.InitialEditMode, true)
        );

        Assert.Equal(8, container.FindAll(".resize-handle").Count);
    }
}
