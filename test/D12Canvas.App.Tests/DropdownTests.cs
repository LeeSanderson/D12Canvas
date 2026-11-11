using Bunit;
using Bunit.Rendering;
using D12Canvas.App.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace D12Canvas.App.Tests;

public sealed class DropdownTests : BunitContext
{
    private readonly List<string> _keysReachingTheParent = [];

    [Theory]
    [InlineData("z")]
    [InlineData("PageUp")]
    public void KeysOtherThanEscapeLeaveAnOpenMenuForThePage(string key)
    {
        var host = RenderDropdownInsideAKeyListener();
        host.Find(".dropdown > button").Click();

        host.Find(".dropdown-item").KeyDown(new KeyboardEventArgs { Key = key, CtrlKey = true });

        Assert.Equal([key], _keysReachingTheParent);
        Assert.Contains("show", host.Find(".dropdown-menu").ClassList);
    }

    [Fact]
    public void EscapeOnAnOpenMenuClosesIt()
    {
        var host = RenderDropdownInsideAKeyListener();
        host.Find(".dropdown > button").Click();

        host.Find(".dropdown-item").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.DoesNotContain("show", host.Find(".dropdown-menu").ClassList);
    }

    private IRenderedComponent<ContainerFragment> RenderDropdownInsideAKeyListener()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(
                1,
                "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(
                    this,
                    e => _keysReachingTheParent.Add(e.Key)
                )
            );
            builder.OpenComponent<Dropdown>(2);
            builder.AddComponentParameter(
                3,
                nameof(Dropdown.Toggle),
                (RenderFragment)(toggle => toggle.AddContent(0, "Menu"))
            );
            builder.AddComponentParameter(
                4,
                nameof(Dropdown.ChildContent),
                (RenderFragment<Action>)(_ => Item)
            );
            builder.CloseComponent();
            builder.CloseElement();
        });
    }

    private static void Item(RenderTreeBuilder item)
    {
        item.OpenElement(0, "button");
        item.AddAttribute(1, "class", "dropdown-item");
        item.AddContent(2, "Item");
        item.CloseElement();
    }
}
