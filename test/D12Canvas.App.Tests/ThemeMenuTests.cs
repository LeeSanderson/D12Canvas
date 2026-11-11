using AngleSharp.Dom;
using Bunit;
using D12Canvas.App.Components;
using D12Canvas.App.Layout;
using D12Canvas.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace D12Canvas.App.Tests;

public sealed class ThemeMenuTests : AppTestContext
{
    private const string ThemeKey = "d12canvas.theme";

    [Fact]
    public void AFirstVisitShowsSystem()
    {
        var layout = RenderBoardList();

        AssertToggleShows(layout, "circle-half", "System");
        Assert.Equal("System", ActiveOption(layout));
    }

    [Theory]
    [InlineData("light", "sun-fill", "Light")]
    [InlineData("dark", "moon-stars-fill", "Dark")]
    [InlineData("system", "circle-half", "System")]
    [InlineData("sepia", "circle-half", "System")]
    public void TheToggleShowsTheStoredChoice(string stored, string icon, string label)
    {
        Preferences.Values[ThemeKey] = stored;

        var layout = RenderBoardList();

        AssertToggleShows(layout, icon, label);
        Assert.Equal(label, ActiveOption(layout));
    }

    [Theory]
    [InlineData("Light", "light")]
    [InlineData("Dark", "dark")]
    public void PickingAThemeStoresItAndAppliesIt(string option, string mode)
    {
        var themeModule = SetUpThemeModule(systemMode: "dark");
        var layout = RenderBoardList();
        Toggle(layout).Click();

        Option(layout, option).Click();

        Assert.Equal(mode, Preferences.Values[ThemeKey]);
        Assert.Equal(mode, themeModule.VerifyInvoke("setMode").Arguments.Single());
        AssertToggleShows(layout, mode == "light" ? "sun-fill" : "moon-stars-fill", option);
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void PickingSystemStoresItAndAppliesTheOsMode(string osMode)
    {
        Preferences.Values[ThemeKey] = "light";
        var themeModule = SetUpThemeModule(systemMode: osMode);
        var layout = RenderBoardList();
        Toggle(layout).Click();

        Option(layout, "System").Click();

        Assert.Equal("system", Preferences.Values[ThemeKey]);
        Assert.Equal(osMode, themeModule.VerifyInvoke("setMode").Arguments.Single());
        AssertToggleShows(layout, "circle-half", "System");
    }

    [Fact]
    public void TheToggleOpensTheMenu()
    {
        var layout = RenderBoardList();

        Toggle(layout).Click();

        Assert.Contains("show", Menu(layout).ClassList);
        Assert.Equal("true", Toggle(layout).GetAttribute("aria-expanded"));
    }

    [Fact]
    public void TheToggleClosesAnOpenMenu()
    {
        var layout = RenderBoardList();
        Toggle(layout).Click();

        Toggle(layout).Click();

        AssertMenuClosed(layout);
    }

    [Fact]
    public void PickingAThemeClosesTheMenu()
    {
        SetUpThemeModule(systemMode: "light");
        var layout = RenderBoardList();
        Toggle(layout).Click();

        Option(layout, "Dark").Click();

        AssertMenuClosed(layout);
    }

    [Fact]
    public void EscapeClosesTheMenuAndFocusesTheToggle()
    {
        var layout = RenderBoardList();
        var toggleReference = Toggle(layout).GetAttribute("blazor:elementReference");
        Toggle(layout).Click();

        Option(layout, "Dark").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        AssertMenuClosed(layout);
        var focused = (ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!;
        Assert.Equal(toggleReference, focused.Id);
    }

    [Fact]
    public void AClickOutsideClosesTheMenu()
    {
        var layout = RenderBoardList();
        Toggle(layout).Click();

        layout.Find(".dropdown-outside").Click();

        AssertMenuClosed(layout);
        Assert.Empty(layout.FindAll(".dropdown-outside"));
    }

    [Fact]
    public void PickingAThemeLeavesTheOffcanvasOpen()
    {
        SetUpThemeModule(systemMode: "light");
        var layout = RenderBoardList();
        layout.Find("button[aria-controls='app-menu']").Click();
        Toggle(layout).Click();

        Option(layout, "Dark").Click();

        Assert.Contains("show", layout.Find("#app-menu").ClassList);
    }

    [Fact]
    public void EscapeInTheOffcanvasClosesTheMenuButLeavesTheOffcanvasOpen()
    {
        var layout = RenderBoardList();
        layout.Find("button[aria-controls='app-menu']").Click();
        Toggle(layout).Click();

        Toggle(layout).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        AssertMenuClosed(layout);
        Assert.Contains("show", layout.Find("#app-menu").ClassList);
    }

    [Fact]
    public void TheEditorHasNoThemeSelectOrThemeOfItsOwn()
    {
        var boardId = BoardStore.Add("Service map", new Board());

        var layout = RenderBoardEditor(boardId);

        layout.WaitForAssertion(() => Assert.NotEmpty(layout.FindAll(".diagram-container")));
        Assert.DoesNotContain(
            layout.FindAll("main select option"),
            option => option.TextContent.Trim() == "Dark"
        );
        Assert.Empty(layout.FindAll("[data-d12-theme]"));
    }

    private BunitJSModuleInterop SetUpThemeModule(string systemMode)
    {
        var themeModule = JSInterop.SetupModule("./Components/ThemeMenu.razor.js");
        themeModule.Setup<string>("systemMode").SetResult(systemMode);
        themeModule.SetupVoid("setMode", _ => true).SetVoidResult();
        return themeModule;
    }

    private void AssertToggleShows(IRenderedComponent<MainLayout> layout, string icon, string label)
    {
        var expectedIcon = Render<Icon>(parameters => parameters.Add(i => i.Name, icon));
        Toggle(layout).QuerySelector("svg")!.MarkupMatches(expectedIcon.Markup);
        Assert.Equal($"Theme: {label}", Toggle(layout).GetAttribute("title"));
    }

    private static void AssertMenuClosed(IRenderedComponent<MainLayout> layout)
    {
        Assert.DoesNotContain("show", Menu(layout).ClassList);
        Assert.Equal("false", Toggle(layout).GetAttribute("aria-expanded"));
    }

    private static IElement Toggle(IRenderedComponent<MainLayout> layout) =>
        layout
            .FindAll("#app-menu .dropdown > button")
            .Single(button => button.TextContent.Trim() == "Theme");

    private static IElement Menu(IRenderedComponent<MainLayout> layout) =>
        Toggle(layout).ParentElement!.QuerySelector(".dropdown-menu")!;

    private static IElement Option(IRenderedComponent<MainLayout> layout, string label) =>
        Menu(layout)
            .QuerySelectorAll(".dropdown-item")
            .Single(item => item.TextContent.Trim() == label);

    private static string ActiveOption(IRenderedComponent<MainLayout> layout) =>
        Menu(layout).QuerySelectorAll(".dropdown-item.active").Single().TextContent.Trim();
}
