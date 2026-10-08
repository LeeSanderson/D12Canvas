using Bunit;
using D12Canvas.Model;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace D12Canvas.Tests;

public class ComponentContainerTests : ComponentTestBase
{
    [Fact]
    public void AContainerOutsideABoardIsAPositionedBoxWithItsContentAndNoAffordances()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters
                .Add(p => p.X, 40)
                .Add(p => p.Y, 60)
                .Add(p => p.Width, 120)
                .Add(p => p.Height, 80)
                .AddChildContent("<span class=\"author-box\">Hello</span>")
        );

        var containerElement = container.Find(".component-container");
        Assert.Equal(
            "left: 40px; top: 60px; width: 120px; height: 80px; z-index: 0;",
            containerElement.GetAttribute("style")
        );
        Assert.Single(container.FindAll(".container-content .author-box"));
        Assert.Equal(["component-container"], containerElement.ClassList);
        Assert.Empty(container.FindAll(".resize-handle"));
        Assert.Empty(container.FindAll(".port-span"));
        Assert.Empty(container.FindAll(".port"));
        Assert.Null(containerElement.GetAttribute("data-d12-entity"));
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
    public void AnUnselectedInstanceRendersNoPorts()
    {
        var container = Render<ComponentContainer>();

        Assert.Empty(container.FindAll(".port"));
        Assert.Empty(container.FindAll(".port-span"));
    }

    // bUnit has no layout engine, so where a dot or span lands is PortsVisualTests' to prove; the
    // class names and the role marker on the span are what the stylesheet and the listener key off.
    [Fact]
    public void ASelectedInstanceDrawsADotAndASpanForEachStandardPort()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.IsSelected, true).Add(p => p.Width, 200).Add(p => p.Height, 150)
        );

        Assert.Equal(
            ["port port-top", "port port-right", "port port-bottom", "port port-left"],
            container.FindAll(".port").Select(dot => dot.ClassName)
        );
        Assert.All(
            container.FindAll(".port"),
            dot => Assert.Null(dot.GetAttribute("data-d12-role"))
        );
        Assert.Equal(
            ["Top", "Right", "Bottom", "Left"],
            container.FindAll(".port-span").Select(span => span.GetAttribute("data-d12-part"))
        );
        Assert.All(
            container.FindAll(".port-span"),
            span => Assert.Equal("port", span.GetAttribute("data-d12-role"))
        );
    }

    [Fact]
    public void ADropTargetShowsItsPortsAndNoResizeAffordances()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.IsDropTarget, true).Add(p => p.Width, 200).Add(p => p.Height, 150)
        );

        Assert.Equal(4, container.FindAll(".port-span").Count);
        Assert.Empty(container.FindAll(".resize-span"));
        Assert.Empty(container.FindAll(".resize-handle"));
    }

    // Each span is a percentage of its side plus a number of port targets over scale, so it stays
    // the same size on screen whatever the zoom.
    [Fact]
    public void ASpanIsPlacedAlongItsSideInPortTargetsOverScale()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.IsSelected, true).Add(p => p.Width, 200).Add(p => p.Height, 150)
        );

        Assert.Equal(
            "left: calc(50% + -0.5 * var(--d12-port-target) / var(--d12-scale)); "
                + "width: calc(0% + 1 * var(--d12-port-target) / var(--d12-scale));",
            container.Find(".port-span-top").GetAttribute("style")
        );
        Assert.Equal(
            "top: calc(0% + 1 * var(--d12-port-target) / var(--d12-scale)); "
                + "height: calc(50% + -1.5 * var(--d12-port-target) / var(--d12-scale));",
            container.FindAll(".resize-span-left")[0].GetAttribute("style")
        );
    }

    [Fact]
    public void ZoomingOutFarEnoughDropsTheResizeSpansAndZoomingBackInRestoresThem()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.IsSelected, true).Add(p => p.Width, 200).Add(p => p.Height, 200)
        );
        Assert.Equal(8, container.FindAll(".resize-span").Count);

        container.Render(parameters => parameters.Add(p => p.Scale, 0.45));
        Assert.Empty(container.FindAll(".resize-span"));
        Assert.Equal(4, container.FindAll(".port-span").Count);
        Assert.Equal(4, container.FindAll(".resize-handle").Count);

        container.Render(parameters => parameters.Add(p => p.Scale, 1));
        Assert.Equal(8, container.FindAll(".resize-span").Count);
    }

    [Fact]
    public void AnInstanceWithNoHitRegionCarriesNoHitMarker()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.HasHitRegion, false)
        );

        Assert.Null(container.Find(".component-container").GetAttribute("data-d12-role"));
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

    // Side resize has no drawn handle: the four corners keep one each, and each side's resize
    // spans are invisible regions the cursor alone reveals.
    [Fact]
    public void SelectedInstanceRendersFourCornerHandles()
    {
        var container = Render<ComponentContainer>(parameters =>
            parameters.Add(p => p.IsSelected, true).Add(p => p.Width, 200).Add(p => p.Height, 150)
        );

        Assert.Equal(
            ["top-left", "top-right", "bottom-right", "bottom-left"],
            container
                .FindAll(".resize-handle")
                .Select(handle => handle.GetAttribute("data-d12-part"))
        );
        Assert.Equal(8, container.FindAll(".resize-span").Count);
    }

    [Fact]
    public void UnselectedInstanceOmitsResizeHandles()
    {
        var container = Render<ComponentContainer>();

        Assert.Empty(container.FindAll(".resize-handle"));
    }
}
