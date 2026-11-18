using AngleSharp.Dom;
using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// A partly locked selection opens the property panel with a line that counts the locked targets;
// a fully locked one shows the Unlock button instead, and an unlocked one shows neither.
public sealed class PropertyPanelLockLineTests : ComponentTestBase
{
    private readonly Board _board = new();

    public PropertyPanelLockLineTests()
    {
        SetupDiagramCanvasJsModule();
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        Services.AddSingleton(options.Registry);
    }

    private ComponentInstance AddRectangle(double x, bool locked = false)
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps("#ff0000", null, 2),
            new Bounds(x, 200, 100, 60),
            locked: locked
        );
        _board.AddComponent(instance);
        return instance;
    }

    // A right-click is the one pointer route that selects a locked shape; Escape then closes its
    // menu.
    private async Task<IRenderedComponent<PropertyPanel>> RenderWithAGroupSelected(
        int lockedMembers
    )
    {
        var members = Enumerable
            .Range(0, 3)
            .Select(index => AddRectangle(index * 150, locked: index < lockedMembers))
            .ToList();
        _board.AddGroup(new Group(members.Select(member => member.Id).ToList()));
        var canvas = Render<DiagramCanvas>(parameters =>
                parameters.Add(p => p.Board, _board).Add(p => p.SnapToGrid, false)
            )
            .ReturnToOrigin();
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        await canvas.Press(
            10,
            10,
            PointerPress.SecondaryButton,
            role: HitRole.Instance,
            entityId: members[0].Id
        );
        await canvas.Release(10, 10, PointerPress.SecondaryButton);
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        Assert.Equal(3, canvas.Instance.SelectedComponents.Count);
        return panel;
    }

    private static IElement? LockLine(IRenderedComponent<PropertyPanel> panel) =>
        panel.FindAll(".d12-property-panel-locks").SingleOrDefault();

    [Fact]
    public async Task APartlyLockedSelectionOpensThePanelWithTheLockLine()
    {
        var panel = await RenderWithAGroupSelected(lockedMembers: 1);

        var line = LockLine(panel)!;
        Assert.Equal("1 of 3 locked; changes skip them", line.TextContent.Trim());
        Assert.Equal(
            Glyphs.Lock.Parts.Select(part => part.Data),
            line.QuerySelectorAll("svg.d12-icon path").Select(path => path.GetAttribute("d"))
        );
        Assert.Equal("true", line.QuerySelector("svg.d12-icon")!.GetAttribute("aria-hidden"));
        Assert.Same(panel.Find(".d12-property-panel").Children[0], line);
        Assert.Empty(panel.FindAll(".d12-property-panel-unlock"));
    }

    [Fact]
    public async Task TheLockLineIsPlainText()
    {
        var panel = await RenderWithAGroupSelected(lockedMembers: 2);

        var line = LockLine(panel)!;
        Assert.Equal("2 of 3 locked; changes skip them", line.TextContent.Trim());
        Assert.Equal("P", line.TagName);
        Assert.Empty(line.QuerySelectorAll("button, [tabindex], [title], .d12-tooltip-anchor"));
        foreach (var attribute in new[] { "role", "aria-live", "title", "id", "tabindex" })
        {
            Assert.False(line.HasAttribute(attribute), attribute);
        }
        Assert.Empty(panel.FindAll("[aria-describedby]"));
        Assert.All(
            panel.FindAll(".d12-property-panel-field input"),
            control => Assert.False(control.HasAttribute("disabled"))
        );
    }

    [Fact]
    public async Task AFullyLockedSelectionShowsTheUnlockButtonAndNoLockLine()
    {
        var panel = await RenderWithAGroupSelected(lockedMembers: 3);

        Assert.Null(LockLine(panel));
        Assert.Single(panel.FindAll(".d12-property-panel-unlock"));
    }

    [Fact]
    public async Task AnUnlockedSelectionShowsNeitherTheLockLineNorUnlock()
    {
        var panel = await RenderWithAGroupSelected(lockedMembers: 0);

        Assert.Null(LockLine(panel));
        Assert.Empty(panel.FindAll(".d12-property-panel-unlock"));
    }
}
