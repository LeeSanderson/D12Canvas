using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace D12Canvas.Tests;

// A pure presentation component - every assertion here is about what it shows and which
// callback a given action fires, never about Board/History (DiagramCanvas wires those callbacks
// to its own OnXPressed methods - see DiagramCanvasContextMenuTests).
public class SelectionContextMenuTests : ComponentTestBase
{
    public SelectionContextMenuTests()
    {
        var module = JSInterop.SetupModule("./_content/D12Canvas/SelectionContextMenu.razor.js");
        module.SetupVoid("registerClickOutside", _ => true).SetVoidResult();
        module.SetupVoid("unregisterClickOutside").SetVoidResult();
        module.SetupVoid("focusAdjacentItem", _ => true).SetVoidResult();
    }

    [Fact]
    public void RendersAtTheSuppliedAnchorPoint()
    {
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters.Add(p => p.X, 120).Add(p => p.Y, 80)
        );

        var style = menu.Find(".d12-context-menu").GetAttribute("style");
        Assert.Contains("left: 120px", style);
        Assert.Contains("top: 80px", style);
    }

    [Fact]
    public void AlwaysShowsDeleteAndTheFourLayeringActions()
    {
        var menu = Render<SelectionContextMenu>();

        var labels = menu.FindAll(".d12-context-menu-item")
            .Select(item => item.TextContent)
            .ToArray();
        Assert.Contains("Delete", labels);
        Assert.Contains("Bring to Front", labels);
        Assert.Contains("Bring Forward", labels);
        Assert.Contains("Send Backward", labels);
        Assert.Contains("Send to Back", labels);
    }

    [Fact]
    public void OmitsGroupAndUngroupByDefault()
    {
        var menu = Render<SelectionContextMenu>();

        var labels = menu.FindAll(".d12-context-menu-item")
            .Select(item => item.TextContent)
            .ToArray();
        Assert.DoesNotContain("Group", labels);
        Assert.DoesNotContain("Ungroup", labels);
    }

    [Fact]
    public void ShowsGroupOnlyWhenCanGroupIsTrue()
    {
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters.Add(p => p.CanGroup, true)
        );

        Assert.Contains(
            "Group",
            menu.FindAll(".d12-context-menu-item").Select(item => item.TextContent)
        );
    }

    [Fact]
    public void ShowsUngroupOnlyWhenCanUngroupIsTrue()
    {
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters.Add(p => p.CanUngroup, true)
        );

        Assert.Contains(
            "Ungroup",
            menu.FindAll(".d12-context-menu-item").Select(item => item.TextContent)
        );
    }

    private static IElement ItemNamed(IRenderedComponent<SelectionContextMenu> menu, string text) =>
        menu.FindAll(".d12-context-menu-item").Single(item => item.TextContent == text);

    [Fact]
    public void ClickingDeleteInvokesOnDelete()
    {
        var invoked = false;
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters.Add(p => p.OnDelete, () => invoked = true)
        );

        ItemNamed(menu, "Delete").Click();

        Assert.True(invoked);
    }

    [Fact]
    public void ClickingGroupInvokesOnGroup()
    {
        var invoked = false;
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters.Add(p => p.CanGroup, true).Add(p => p.OnGroup, () => invoked = true)
        );

        ItemNamed(menu, "Group").Click();

        Assert.True(invoked);
    }

    [Fact]
    public void ClickingUngroupInvokesOnUngroup()
    {
        var invoked = false;
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters.Add(p => p.CanUngroup, true).Add(p => p.OnUngroup, () => invoked = true)
        );

        ItemNamed(menu, "Ungroup").Click();

        Assert.True(invoked);
    }

    [Fact]
    public void ClickingEachLayeringActionInvokesItsOwnCallback()
    {
        var fired = new List<string>();
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters
                .Add(p => p.OnBringToFront, () => fired.Add("front"))
                .Add(p => p.OnBringForward, () => fired.Add("forward"))
                .Add(p => p.OnSendBackward, () => fired.Add("backward"))
                .Add(p => p.OnSendToBack, () => fired.Add("back"))
        );

        ItemNamed(menu, "Bring to Front").Click();
        ItemNamed(menu, "Bring Forward").Click();
        ItemNamed(menu, "Send Backward").Click();
        ItemNamed(menu, "Send to Back").Click();

        Assert.Equal(["front", "forward", "backward", "back"], fired);
    }

    [Fact]
    public void EscapeInvokesOnRequestClose()
    {
        var invoked = false;
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters.Add(p => p.OnRequestClose, () => invoked = true)
        );

        menu.Find(".d12-context-menu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.True(invoked);
    }

    [Fact]
    public void ANonEscapeKeyDoesNotInvokeOnRequestClose()
    {
        var invoked = false;
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters.Add(p => p.OnRequestClose, () => invoked = true)
        );

        menu.Find(".d12-context-menu").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.False(invoked);
    }

    [Fact]
    public void ArrowDownRovesFocusToTheNextItem()
    {
        var menu = Render<SelectionContextMenu>();

        menu.Find(".d12-context-menu").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var invocation = Assert.Single(JSInterop.Invocations["focusAdjacentItem"]);
        Assert.Equal(1, invocation.Arguments[1]);
    }

    [Fact]
    public void ArrowUpRovesFocusToThePreviousItem()
    {
        var menu = Render<SelectionContextMenu>();

        menu.Find(".d12-context-menu").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });

        var invocation = Assert.Single(JSInterop.Invocations["focusAdjacentItem"]);
        Assert.Equal(-1, invocation.Arguments[1]);
    }

    [Fact]
    public void ClickOutsideInvokesOnRequestClose()
    {
        var invoked = false;
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters.Add(p => p.OnRequestClose, () => invoked = true)
        );

        menu.InvokeAsync(() => menu.Instance.OnClickOutside());

        Assert.True(invoked);
    }

    [Fact]
    public void EveryMenuItemIsAFocusableButton()
    {
        var menu = Render<SelectionContextMenu>(parameters =>
            parameters.Add(p => p.CanGroup, true).Add(p => p.CanUngroup, true)
        );

        foreach (var item in menu.FindAll(".d12-context-menu-item"))
        {
            Assert.Equal("button", item.TagName.ToLowerInvariant());
            Assert.Equal("button", item.GetAttribute("type"));
        }
    }
}
