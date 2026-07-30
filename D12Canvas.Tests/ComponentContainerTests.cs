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
            parameters.Add(p => p.OnSelect, (bool _) => selectCount++)
        );

        container.Find(".component-container").Click();

        Assert.Equal(1, selectCount);
    }

    [Fact]
    public void ClickingTheContainerWithShiftHeldInvokesOnSelectWithTrue()
    {
        bool? shiftKeyReceived = null;
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.OnSelect, (bool shiftKey) => shiftKeyReceived = shiftKey)
        );

        container.Find(".component-container").Click(new MouseEventArgs { ShiftKey = true });

        Assert.True(shiftKeyReceived);
    }

    [Fact]
    public void ClickingTheContainerWithoutShiftInvokesOnSelectWithFalse()
    {
        bool? shiftKeyReceived = null;
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.OnSelect, (bool shiftKey) => shiftKeyReceived = shiftKey)
        );

        container.Find(".component-container").Click();

        Assert.False(shiftKeyReceived);
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

    [Fact]
    public void EveryInstanceRendersAllFourStandardPortsWithDirectionalClasses()
    {
        // bUnit has no real browser layout/pseudo-class engine, so "at their border centers" and
        // "hidden otherwise" (ticket 47) aren't checkable here - the class names below are what
        // the stylesheet keys its percentage-of-box positioning and hover/selection opacity off
        // of, and PortsVisualTests.cs proves the resulting on-screen behavior in a real browser.
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
        // Ticket 60: a layering command changes only ZIndex, with Bounds/selection/Props/custom
        // ports all unchanged - ComponentContainer's own ShouldRender override must still treat
        // that as a real change, or a stacking change would silently fail to render (ADR 0008's
        // "renders immediately") until some unrelated parameter happened to change too.
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

    [Fact]
    public void DraggingTheBottomRightHandleInvokesOnResizedWithTheGrownBounds()
    {
        Bounds? resizedTo = null;
        var container = Render<ComponentContainer>(parameters =>
            parameters
                .Add(p => p.IsSelected, true)
                .Add(p => p.X, 100)
                .Add(p => p.Y, 100)
                .Add(p => p.Width, 50)
                .Add(p => p.Height, 50)
                .Add(p => p.OnResized, (Bounds bounds) => resizedTo = bounds)
        );

        var handle = container.Find(".resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 0, ClientY = 0 });
        handle.MouseMove(new MouseEventArgs { ClientX = 30, ClientY = 10 });
        handle.MouseUp(new MouseEventArgs { ClientX = 30, ClientY = 10 });

        // Bottom-right handle grows Width/Height directly; X/Y (the opposite/top-left corner)
        // stay anchored in place.
        Assert.Equal(new Bounds(100, 100, 80, 60), resizedTo);
    }

    [Fact]
    public void DraggingTheTopLeftHandleKeepsTheOppositeCornerAnchored()
    {
        Bounds? resizedTo = null;
        var container = Render<ComponentContainer>(parameters =>
            parameters
                .Add(p => p.IsSelected, true)
                .Add(p => p.X, 100)
                .Add(p => p.Y, 100)
                .Add(p => p.Width, 100)
                .Add(p => p.Height, 100)
                .Add(p => p.OnResized, (Bounds bounds) => resizedTo = bounds)
        );

        var handle = container.Find(".resize-handle.top-left");
        handle.MouseDown(new MouseEventArgs { ClientX = 0, ClientY = 0 });
        handle.MouseMove(new MouseEventArgs { ClientX = 10, ClientY = 20 });
        handle.MouseUp(new MouseEventArgs { ClientX = 10, ClientY = 20 });

        Assert.NotNull(resizedTo);
        // The bottom-right corner (Right/Bottom) is the anchor for a top-left drag - it must
        // land exactly where the un-resized instance's own bottom-right corner was.
        Assert.Equal(200, resizedTo!.Value.Right, precision: 10);
        Assert.Equal(200, resizedTo!.Value.Bottom, precision: 10);
        Assert.Equal(new Bounds(110, 120, 90, 80), resizedTo);
    }

    [Fact]
    public void ResizingCannotShrinkBelowTheMinimumSizeOrInvertBounds()
    {
        Bounds? resizedTo = null;
        var container = Render<ComponentContainer>(parameters =>
            parameters
                .Add(p => p.IsSelected, true)
                .Add(p => p.X, 100)
                .Add(p => p.Y, 100)
                .Add(p => p.Width, 200)
                .Add(p => p.Height, 200)
                .Add(p => p.OnResized, (Bounds bounds) => resizedTo = bounds)
        );

        var handle = container.Find(".resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 0, ClientY = 0 });
        // A huge inward drag would otherwise invert Width/Height into negative territory.
        handle.MouseMove(new MouseEventArgs { ClientX = -1000, ClientY = -1000 });
        handle.MouseUp(new MouseEventArgs { ClientX = -1000, ClientY = -1000 });

        Assert.Equal(new Bounds(100, 100, 50, 50), resizedTo);
    }

    [Fact]
    public void APressAndReleaseOfAResizeHandleWithNoMovementDoesNotInvokeOnResized()
    {
        var invoked = false;
        var container = Render<ComponentContainer>(parameters =>
            parameters
                .Add(p => p.IsSelected, true)
                .Add(p => p.OnResized, (Bounds _) => invoked = true)
        );

        var handle = container.Find(".resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 50, ClientY = 50 });
        handle.MouseUp(new MouseEventArgs { ClientX = 50, ClientY = 50 });

        Assert.False(invoked);
    }
}
