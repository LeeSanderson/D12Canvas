using AngleSharp.Dom;
using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Panel;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// A partly locked selection ends the property bar with a lock count; a fully locked one keeps the
// bar where it would stand and offers only Unlock.
public sealed class PropertyBarLockTests : ComponentTestBase
{
    private readonly Board _board = new();

    public PropertyBarLockTests()
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

    private IRenderedComponent<DiagramCanvas> RenderCanvas() =>
        Render<DiagramCanvas>(parameters =>
                parameters.Add(p => p.Board, _board).Add(p => p.SnapToGrid, false)
            )
            .ReturnToOrigin();

    // A right-click is the one pointer route that selects a locked shape; Escape then closes its
    // menu, which hides the bar while it is open.
    private async Task<IRenderedComponent<DiagramCanvas>> RenderWithAGroupSelected(
        int lockedMembers
    )
    {
        var members = Enumerable
            .Range(0, 3)
            .Select(index => AddRectangle(index * 150, locked: index < lockedMembers))
            .ToList();
        _board.AddGroup(new Group(members.Select(member => member.Id).ToList()));
        var canvas = RenderCanvas();
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
        return canvas;
    }

    private static IElement Bar(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find(".d12-property-bar");

    private static IElement? LockCount(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-property-bar-locks").SingleOrDefault();

    private static IReadOnlyList<IElement> Controls(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-property-bar-control").ToList();

    private static string[] GlyphPaths(IElement icon) =>
        icon.QuerySelectorAll("path").Select(path => path.GetAttribute("d")!).ToArray();

    [Fact]
    public async Task APartlyLockedSelectionEndsTheBarWithItsLockCount()
    {
        var canvas = await RenderWithAGroupSelected(lockedMembers: 1);

        var count = LockCount(canvas)!;
        Assert.Equal("1/3", count.TextContent.Trim());
        Assert.Equal("1 of 3 locked; changes skip them", count.GetAttribute("aria-label"));
        Assert.Equal("SPAN", count.TagName);
        Assert.Null(count.Closest("button"));
        Assert.DoesNotContain("d12-property-bar-control", count.ClassList);
        Assert.Equal(
            Glyphs.Lock.Parts.Select(part => part.Data),
            GlyphPaths(count.QuerySelector("svg.d12-icon")!)
        );

        var bar = Bar(canvas);
        Assert.Same(bar.Children[^1], count.Closest(".d12-tooltip-anchor"));
        Assert.Contains("d12-property-bar-divider", bar.Children[^2].ClassList);
        Assert.Equal(
            "1 of 3 locked; changes skip them",
            count.Closest(".d12-tooltip-anchor")!.QuerySelector(".d12-tooltip-above")!.TextContent
        );
        Assert.Equal(["Fill: #FF0000", "Stroke: Theme", "Stroke width"], ControlNames(canvas));
    }

    [Fact]
    public async Task AnUnlockedSelectionShowsNoLockCount()
    {
        var canvas = await RenderWithAGroupSelected(lockedMembers: 0);

        Assert.Null(LockCount(canvas));
        Assert.Empty(canvas.FindAll(".d12-property-bar-divider"));
    }

    [Fact]
    public void AFullyLockedSelectionShowsTheBarWithOnlyAnUnlockButton()
    {
        var locked = AddRectangle(200, locked: true);
        var canvas = RenderCanvas();

        canvas.ContainerOf(locked.Id).Focus();

        Assert.Null(LockCount(canvas));
        Assert.Equal("left: 190px; top: 158px;", Bar(canvas).GetAttribute("style"));
        var unlock = Assert.Single(Controls(canvas));
        Assert.Equal("BUTTON", unlock.TagName);
        Assert.Equal("Unlock", unlock.TextContent.Trim());
        Assert.Equal("-1", unlock.GetAttribute("tabindex"));
        Assert.Equal(
            Glyphs.Unlock.Parts.Select(part => part.Data),
            GlyphPaths(unlock.QuerySelector("svg.d12-icon")!)
        );
        Assert.Empty(canvas.FindAll(".d12-property-bar-cell"));
    }

    [Fact]
    public async Task UnlockUnlocksTheWholeSelectionInOneUndoStep()
    {
        var canvas = await RenderWithAGroupSelected(lockedMembers: 3);
        var members = _board.Components.ToList();

        Assert.Equal(["Unlock"], Controls(canvas).Select(control => control.TextContent.Trim()));

        Controls(canvas).Single().Click(new MouseEventArgs { Detail = 0 });

        Assert.All(members, member => Assert.False(member.Locked));
        Assert.Equal(["Fill: #FF0000", "Stroke: Theme", "Stroke width"], ControlNames(canvas));
        PropertyBarModule.VerifyInvoke("focusFirstControl");

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.All(members, member => Assert.True(member.Locked));
        Assert.Equal(["Unlock"], Controls(canvas).Select(control => control.TextContent.Trim()));
    }

    [Fact]
    public async Task AnUnlockPressedWithThePointerLeavesFocusWhereItFalls()
    {
        var canvas = await RenderWithAGroupSelected(lockedMembers: 3);

        Controls(canvas).Single().Click(new MouseEventArgs { Detail = 1 });

        Assert.All(_board.Components, member => Assert.False(member.Locked));
        Assert.Empty(PropertyBarModule.Invocations["focusFirstControl"]);
    }

    [Fact]
    public void TheTallyCountsAPartialLockOnlyWhenSomeButNotAllAreLocked()
    {
        Assert.False(LockTally.Of([]).IsPartial);
        Assert.False(LockTally.Of([]).IsFull);
        Assert.False(LockTally.Of([false, false]).IsPartial);
        Assert.True(LockTally.Of([true, false, false]).IsPartial);
        Assert.False(LockTally.Of([true, true]).IsPartial);
        Assert.True(LockTally.Of([true, true]).IsFull);
        Assert.Equal(
            "1 of 3 locked; changes skip them",
            LockTally.Of([false, true, false]).Summary
        );
    }

    private static string[] ControlNames(IRenderedComponent<DiagramCanvas> canvas) =>
        Controls(canvas).Select(control => control.GetAttribute("aria-label") ?? "").ToArray();
}
