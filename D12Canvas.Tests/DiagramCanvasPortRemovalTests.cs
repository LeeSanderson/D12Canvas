using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The two routes that remove a custom port: the pointer's Remove port row on a custom port's
// span, and Delete while port picking highlights a custom port. The real key path is in
// PortRemovalProbes.
public class DiagramCanvasPortRemovalTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";
    private const string AddPortHere = "Add port here";
    private const string RemovePort = "Remove port";
    private const int SpacesToPinnedPort = 6;
    private const int StandardPortCount = 4;

    private readonly Board _board = new();
    private readonly PortDef _earlier = new(0, 0.75);
    private readonly PortDef _port = new(0.25, 0);
    private readonly PortDef _later = new(1, 0.25);
    private readonly ComponentInstance _shape;
    private readonly ComponentInstance _other;
    private readonly Edge _pinned;

    public DiagramCanvasPortRemovalTests()
    {
        SetupDiagramCanvasJsModule();

        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: ComponentTypeKey,
                ComponentType: typeof(TestPropsComponent),
                PropsType: typeof(TestProps),
                DisplayName: "Test Props",
                AccessibleName: "Test props component",
                DefaultProps: new TestProps(),
                Icon: null,
                Role: "group",
                DefaultSize: null,
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);

        _shape = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(100, 100, 200, 100),
            customPorts: [_earlier, _port, _later]
        );
        _other = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(400, 100, 200, 100)
        );
        _board.AddComponent(_shape);
        _board.AddComponent(_other);
        _pinned = new Edge(PinnedEnd, new PortEndpoint(_other.Id, PortId.Left));
        _board.AddEdge(_pinned);
    }

    private CustomPortEndpoint PinnedEnd => new(_shape.Id, _port.Id);

    private IRenderedComponent<DiagramCanvas> RenderCanvas() =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, _board).Add(p => p.SnapToGrid, false)
        );

    private static string[] MenuLabels(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-context-menu-label").Select(label => label.TextContent).ToArray();

    private static IElement MenuRow(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas
            .FindAll(".d12-context-menu-item")
            .Single(item => item.QuerySelector(".d12-context-menu-label")!.TextContent == label);

    private async Task RightClickOnSpan(IRenderedComponent<DiagramCanvas> canvas, string part)
    {
        canvas.ClickOn(canvas.ContainerOf(_shape.Id));
        await canvas.Press(
            150,
            100,
            PointerPress.SecondaryButton,
            role: HitRole.Port,
            entityId: _shape.Id,
            part: part
        );
        await canvas.Release(150, 100, PointerPress.SecondaryButton);
    }

    private Task RightClickOnCustomSpan(IRenderedComponent<DiagramCanvas> canvas) =>
        RightClickOnSpan(canvas, _port.Id.ToString());

    private static Task Run(IRenderedComponent<DiagramCanvas> canvas, Action<DiagramCanvas> act) =>
        canvas.InvokeAsync(() => act(canvas.Instance));

    private async Task StartPicking(IRenderedComponent<DiagramCanvas> canvas, int spaces)
    {
        canvas.ContainerOf(_shape.Id).Focus();
        await Run(canvas, c => c.OnEnterPressed());
        for (var i = 0; i < spaces; i++)
        {
            await Run(canvas, c => c.OnSpacePressed());
        }
    }

    private void AssertUntouched()
    {
        Assert.NotNull(_board.GetComponent(_shape.Id));
        Assert.Equal(new[] { _earlier, _port, _later }, _shape.CustomPorts);
        Assert.Equal(PinnedEnd, _pinned.Source);
        Assert.Single(_board.Edges);
    }

    [Fact]
    public async Task RemovePortOnACustomPortsSpanRemovesItAndAutoAttachesThePinnedEdge()
    {
        var canvas = RenderCanvas();

        await RightClickOnCustomSpan(canvas);
        Assert.Contains(RemovePort, MenuLabels(canvas));
        Assert.DoesNotContain(AddPortHere, MenuLabels(canvas));
        MenuRow(canvas, RemovePort).Click();

        Assert.Equal(new[] { _earlier, _later }, _shape.CustomPorts);
        Assert.Equal(new AutoPortEndpoint(_shape.Id), _pinned.Source);
        Assert.Equal(new PortEndpoint(_other.Id, PortId.Left), _pinned.Target);
        Assert.NotNull(canvas.Instance.Board!.ResolveEnd(_pinned, isSource: true));
    }

    [Fact]
    public async Task OneUndoRestoresThePortAtItsIndexAndRepinsTheEdge()
    {
        var canvas = RenderCanvas();
        await RightClickOnCustomSpan(canvas);
        MenuRow(canvas, RemovePort).Click();

        await Run(canvas, c => c.OnUndoPressed());

        AssertUntouched();
        await Run(canvas, c => c.OnRedoPressed());
        Assert.Equal(new[] { _earlier, _later }, _shape.CustomPorts);
        Assert.Equal(new AutoPortEndpoint(_shape.Id), _pinned.Source);
    }

    [Fact]
    public async Task AStandardPortsSpanOffersAddPortHereAndNotRemovePort()
    {
        var canvas = RenderCanvas();

        await RightClickOnSpan(canvas, "Top");

        Assert.Contains(AddPortHere, MenuLabels(canvas));
        Assert.DoesNotContain(RemovePort, MenuLabels(canvas));
    }

    [Fact]
    public async Task ALockedShapeOffersNeitherRow()
    {
        _shape.Locked = true;
        var canvas = RenderCanvas();

        await RightClickOnCustomSpan(canvas);

        Assert.DoesNotContain(RemovePort, MenuLabels(canvas));
        Assert.DoesNotContain(AddPortHere, MenuLabels(canvas));
    }

    [Fact]
    public async Task ALockedPinnedEdgeLeavesNeitherRow()
    {
        _pinned.Locked = true;
        var canvas = RenderCanvas();

        await RightClickOnCustomSpan(canvas);

        Assert.DoesNotContain(RemovePort, MenuLabels(canvas));
        Assert.DoesNotContain(AddPortHere, MenuLabels(canvas));
    }

    [Fact]
    public async Task DeleteWhilePickingACustomPortRemovesItAndKeepsTheShape()
    {
        var canvas = RenderCanvas();
        await StartPicking(canvas, SpacesToPinnedPort);

        await Run(canvas, c => c.OnDeletePressed());

        Assert.NotNull(_board.GetComponent(_shape.Id));
        Assert.Equal(new[] { _earlier, _later }, _shape.CustomPorts);
        Assert.Equal(new AutoPortEndpoint(_shape.Id), _pinned.Source);
        await Run(canvas, c => c.OnUndoPressed());
        AssertUntouched();
    }

    [Fact]
    public async Task ThePickStaysOpenOnTheAutoStageAfterARemoval()
    {
        var canvas = RenderCanvas();
        await StartPicking(canvas, SpacesToPinnedPort);

        await Run(canvas, c => c.OnDeletePressed());

        Assert.Equal(
            StandardPortCount,
            canvas.ContainerOf(_shape.Id).QuerySelectorAll(".port-focused").Length
        );
        await Run(canvas, c => c.OnSpacePressed());
        Assert.Contains(
            "port-focused",
            canvas.ContainerOf(_shape.Id).QuerySelector(".port-top")!.ClassName
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task DeleteWhilePickingAutoOrAStandardPortDoesNothing(int spaces)
    {
        var canvas = RenderCanvas();
        await StartPicking(canvas, spaces);

        await Run(canvas, c => c.OnDeletePressed());
        await Run(canvas, c => c.OnUndoPressed());

        AssertUntouched();
    }

    [Fact]
    public async Task DeleteWhilePickingOnALockedPinnedEdgesPortDoesNothing()
    {
        _pinned.Locked = true;
        var canvas = RenderCanvas();
        await StartPicking(canvas, SpacesToPinnedPort);

        await Run(canvas, c => c.OnDeletePressed());

        AssertUntouched();
    }

    [Fact]
    public async Task RemovingTheArmedSourcesPortDisarmsIt()
    {
        var canvas = RenderCanvas();
        await StartPicking(canvas, SpacesToPinnedPort);
        await Run(canvas, c => c.OnEnterPressed());
        await StartPicking(canvas, SpacesToPinnedPort);

        await Run(canvas, c => c.OnDeletePressed());
        canvas.ContainerOf(_other.Id).Focus();
        await Run(canvas, c => c.OnEnterPressed());
        await Run(canvas, c => c.OnEnterPressed());

        Assert.Single(_board.Edges);
        Assert.Empty(canvas.ContainerOf(_shape.Id).QuerySelectorAll(".port-focused"));
    }
}
