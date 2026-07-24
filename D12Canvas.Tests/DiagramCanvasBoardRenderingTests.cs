using System.Linq;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasBoardRenderingTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasBoardRenderingTests()
    {
        SetupDiagramCanvasJsModule();
        JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");
    }

    private void RegisterTestComponent(
        string role = "group",
        string accessibleName = "Test props component"
    )
    {
        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: ComponentTypeKey,
                ComponentType: typeof(TestPropsComponent),
                PropsType: typeof(TestProps),
                DisplayName: "Test Props",
                AccessibleName: accessibleName,
                DefaultProps: new TestProps(),
                Icon: null,
                Role: role,
                DefaultSize: null,
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    [Fact]
    public void RendersEachComponentInstanceViaRegistryResolvedComponentAndProps()
    {
        RegisterTestComponent();
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(
                ComponentTypeKey,
                new TestProps("hello"),
                new Bounds(10, 20, 100, 50)
            )
        );

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Equal("hello", canvas.Find(".test-props-component").TextContent);
    }

    [Fact]
    public void RendersOneComponentContainerPerBoardInstance()
    {
        RegisterTestComponent();
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps("a"), new Bounds(0, 0, 10, 10))
        );
        board.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps("b"), new Bounds(20, 20, 10, 10))
        );

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Equal(2, canvas.FindAll(".component-container").Count);
    }

    [Fact]
    public void AppliesAccessibleNameAndRoleAsAriaOnTheInstanceContainer()
    {
        RegisterTestComponent(role: "figure", accessibleName: "A demo note");
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps(), new Bounds(0, 0, 100, 50))
        );

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var container = canvas.Find(".component-container");
        Assert.Equal("figure", container.GetAttribute("role"));
        Assert.Equal("A demo note", container.GetAttribute("aria-label"));
    }

    [Fact]
    public void RespectsZIndexInVisualStacking()
    {
        RegisterTestComponent();
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(
                ComponentTypeKey,
                new TestProps(),
                new Bounds(0, 0, 100, 50),
                zIndex: 7
            )
        );
        board.AddComponent(
            new ComponentInstance(
                ComponentTypeKey,
                new TestProps(),
                new Bounds(200, 0, 100, 50),
                zIndex: 2
            )
        );

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        var atZeroLeft = containers.Single(c => c.GetAttribute("style")!.Contains("left: 0px"));
        var atTwoHundredLeft = containers.Single(c =>
            c.GetAttribute("style")!.Contains("left: 200px")
        );

        Assert.Contains("z-index: 7", atZeroLeft.GetAttribute("style"));
        Assert.Contains("z-index: 2", atTwoHundredLeft.GetAttribute("style"));
    }

    [Fact]
    public void UnknownComponentTypeKeyThrowsInsteadOfSilentlySkippingTheInstance()
    {
        RegisterTestComponent();
        var board = new Board();
        board.AddComponent(
            new ComponentInstance("no-such-key", new TestProps(), new Bounds(0, 0, 100, 50))
        );

        Assert.Throws<UnknownComponentKeyException>(
            () => Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board))
        );
    }

    [Fact]
    public void InstanceBoundsStayFixedInCanvasSpaceWhilePanTranslatesTheSharedSurface()
    {
        RegisterTestComponent();
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps(), new Bounds(10, 20, 100, 50))
        );

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas
            .Find(".diagram-canvas")
            .MouseDown(
                new MouseEventArgs
                {
                    Button = 0,
                    ClientX = 100,
                    ClientY = 100,
                }
            );
        canvas.Find(".diagram-canvas").MouseMove(new MouseEventArgs { ClientX = 50, ClientY = 40 });

        Assert.Contains(
            "translate(-50px, -60px)",
            canvas.Find(".canvas-content").GetAttribute("style")
        );

        var container = canvas.Find(".component-container");
        Assert.Contains("left: 10px", container.GetAttribute("style"));
        Assert.Contains("top: 20px", container.GetAttribute("style"));
    }

    [Fact]
    public void InstanceBoundsStayFixedInCanvasSpaceWhileZoomScalesTheSharedSurface()
    {
        RegisterTestComponent();
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps(), new Bounds(10, 20, 100, 50))
        );

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 });

        Assert.Contains("scale(1.1)", canvas.Find(".canvas-content").GetAttribute("style"));

        var container = canvas.Find(".component-container");
        Assert.Contains("left: 10px", container.GetAttribute("style"));
        Assert.Contains("top: 20px", container.GetAttribute("style"));
    }
}
