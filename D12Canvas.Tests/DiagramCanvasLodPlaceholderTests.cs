using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// LOD placeholder: below a host-configurable on-screen-size threshold, a component instance
// renders a generic placeholder (built from its registration's DisplayName/Icon) instead of
// mounting its full ComponentContainer/DynamicComponent tree. The placeholder is still a hit
// target, so a zoomed-out board stays reachable by click and marquee.
public class DiagramCanvasLodPlaceholderTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasLodPlaceholderTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();
    }

    private void RegisterTestComponent(string? icon = "★")
    {
        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: ComponentTypeKey,
                ComponentType: typeof(TestPropsComponent),
                PropsType: typeof(TestProps),
                DisplayName: "Test Props",
                AccessibleName: "Test props component",
                DefaultProps: new TestProps(),
                Icon: icon,
                Role: "group",
                DefaultSize: null,
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    private static ComponentInstance AddInstance(Board board, Bounds bounds, int zIndex = 0)
    {
        var instance = new ComponentInstance(ComponentTypeKey, new TestProps(), bounds, zIndex);
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void NormalSizedInstanceRendersAsFullComponentContainerNotAPlaceholder()
    {
        RegisterTestComponent();
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 100, 100));

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Single(canvas.FindAll(".component-container"));
        Assert.Empty(canvas.FindAll(".lod-placeholder"));
    }

    [Fact]
    public void InstanceBelowTheDefaultThresholdRendersAsAPlaceholderInstead()
    {
        RegisterTestComponent();
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 10, 10));

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Empty(canvas.FindAll(".component-container"));
        var placeholder = canvas.Find(".lod-placeholder");
        Assert.Equal("★", placeholder.QuerySelector(".lod-placeholder-icon")!.TextContent);
        Assert.Equal(
            "Test Props",
            placeholder.QuerySelector(".lod-placeholder-label")!.TextContent
        );
    }

    [Fact]
    public void PlaceholderOmitsTheIconElementWhenTheRegistrationHasNone()
    {
        RegisterTestComponent(icon: null);
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 10, 10));

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var placeholder = canvas.Find(".lod-placeholder");
        Assert.Null(placeholder.QuerySelector(".lod-placeholder-icon"));
        Assert.Equal(
            "Test Props",
            placeholder.QuerySelector(".lod-placeholder-label")!.TextContent
        );
    }

    [Fact]
    public void PlaceholderIsAHitTargetForItsInstanceButHiddenFromAssistiveTech()
    {
        RegisterTestComponent();
        var board = new Board();
        var instance = AddInstance(board, new Bounds(0, 0, 10, 10));

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var placeholder = canvas.Find(".lod-placeholder");
        Assert.Equal("instance", placeholder.GetAttribute("data-d12-role"));
        Assert.Equal(instance.Id.ToString(), placeholder.GetAttribute("data-d12-entity"));
        Assert.Equal("true", placeholder.GetAttribute("aria-hidden"));
        Assert.Null(placeholder.GetAttribute("tabindex"));
    }

    [Fact]
    public void AClickOnAPlaceholderSelectsItsInstance()
    {
        RegisterTestComponent();
        var board = new Board();
        var instance = AddInstance(board, new Bounds(0, 0, 10, 10));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickOn(canvas.Find(".lod-placeholder"));

        Assert.Equal(instance, Assert.Single(canvas.Instance.SelectedComponents));
    }

    [Fact]
    public void APlaceholderedParticipantStaysAPlaceholderWhenAZoomMidDragWouldSwapIt()
    {
        RegisterTestComponent();
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 50, 50));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        for (var i = 0; i < 4; i++)
        {
            canvas.ZoomOut();
        }

        canvas.PressOn(canvas.Find(".lod-placeholder"), (10, 10));
        canvas.MoveTo((40, 40));
        for (var i = 0; i < 4; i++)
        {
            canvas.ZoomIn();
        }

        Assert.Single(canvas.FindAll(".lod-placeholder"));
        Assert.Empty(canvas.FindAll(".component-container"));

        canvas.ReleaseAt((40, 40));

        Assert.Empty(canvas.FindAll(".lod-placeholder"));
        Assert.Single(canvas.FindAll(".component-container"));
    }

    [Fact]
    public void AMountedParticipantStaysMountedWhenAZoomMidDragWouldPlaceholderIt()
    {
        RegisterTestComponent();
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 50, 50));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.PressOn(canvas.Find(".component-container"), (10, 10));
        canvas.MoveTo((40, 40));
        for (var i = 0; i < 4; i++)
        {
            canvas.ZoomOut();
        }

        Assert.Single(canvas.FindAll(".component-container"));
        Assert.Empty(canvas.FindAll(".lod-placeholder"));

        canvas.ReleaseAt((40, 40));

        Assert.Single(canvas.FindAll(".lod-placeholder"));
    }

    [Fact]
    public void PlaceholderKeepsTheInstancesPositionAndZIndex()
    {
        RegisterTestComponent();
        var board = new Board();
        AddInstance(board, new Bounds(15, 25, 10, 10), zIndex: 4);

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var style = canvas.Find(".lod-placeholder").GetAttribute("style");
        Assert.Contains("left: 15px", style);
        Assert.Contains("top: 25px", style);
        Assert.Contains("z-index: 4", style);
    }

    [Fact]
    public void ZoomingOutPastThresholdThenBackInSwapsToAndFromThePlaceholderSeamlessly()
    {
        RegisterTestComponent();
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 50, 50));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Assert.Single(canvas.FindAll(".component-container"));

        // Default threshold is 32; the larger dimension (50) crosses it once scale drops below
        // 0.64 - four zoom-out notches (-0.1 each) lands at 0.6.
        for (var i = 0; i < 4; i++)
        {
            canvas.ZoomOut();
        }

        Assert.Empty(canvas.FindAll(".component-container"));
        Assert.Single(canvas.FindAll(".lod-placeholder"));

        for (var i = 0; i < 4; i++)
        {
            canvas.ZoomIn();
        }

        Assert.Single(canvas.FindAll(".component-container"));
        Assert.Empty(canvas.FindAll(".lod-placeholder"));
    }

    [Fact]
    public void HostConfigurableThresholdCanPlaceholderAnInstanceThatWouldOtherwiseFitFine()
    {
        RegisterTestComponent();
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 100, 100));

        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.LodSizeThreshold, 200)
        );

        Assert.Empty(canvas.FindAll(".component-container"));
        Assert.Single(canvas.FindAll(".lod-placeholder"));
    }

    [Fact]
    public async Task CtrlTabSkipsPlaceholderedInstancesTheSameWayItSkipsGroupedMembers()
    {
        RegisterTestComponent();
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 60, 60));
        AddInstance(board, new Bounds(50, 0, 10, 10)); // below threshold - excluded from tab stops
        AddInstance(board, new Bounds(200, 0, 60, 60));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Focus();

        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());

        var invocation = Assert.Single(JSInterop.Invocations["focusTabStopAt"]);
        Assert.Equal(1, invocation.Arguments[1]);
    }

    [Fact]
    public async Task AMarqueeTakesAnInstanceBelowTheLodThresholdLikeAnyOther()
    {
        RegisterTestComponent();
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 60, 60));
        AddInstance(board, new Bounds(100, 0, 10, 10)); // below threshold

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        await canvas.Marquee(from: (0, 0), to: (200, 200));

        Assert.Equal(2, canvas.Instance.SelectedComponents.Count);
    }
}
