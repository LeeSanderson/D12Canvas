using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The listener hands a press on the selection box, or an Alt press, the hit stack at the press
// point, topmost first. A click inside the selection box selects the top entity beneath it, and an
// Alt click selects the next entity down from the one selected before the press, wrapping at the
// bottom. The stack here is supplied as the listener would send it; the board holds three
// instances stacked top, middle, bottom, a group of two members, an instance outside everything
// and one edge.
public class DiagramCanvasHitStackTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    private readonly Board _board = new();
    private readonly ComponentInstance _top;
    private readonly ComponentInstance _middle;
    private readonly ComponentInstance _bottom;
    private readonly ComponentInstance _firstMember;
    private readonly ComponentInstance _secondMember;
    private readonly ComponentInstance _outsider;
    private readonly Group _group;
    private readonly Edge _edge;

    public DiagramCanvasHitStackTests()
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

        _bottom = AddInstance(0);
        _middle = AddInstance(20);
        _top = AddInstance(40);
        _firstMember = AddInstance(300);
        _secondMember = AddInstance(320);
        _outsider = AddInstance(500);
        _group = new Group([_firstMember.Id, _secondMember.Id]);
        _board.AddGroup(_group);
        _edge = new Edge(new FloatingEndpoint(0, 200), new FloatingEndpoint(400, 200));
        _board.AddEdge(_edge);
    }

    private ComponentInstance AddInstance(double x)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, 0, 100, 100)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas() =>
        Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, _board));

    private static PointerHit InstanceHit(ComponentInstance instance) =>
        new(HitRole.Instance, instance.Id, null);

    private static PointerHit EdgeHit(Edge edge) => new(HitRole.Edge, edge.Id, null);

    private static readonly PointerHit SelectionBoxHit = new(HitRole.SelectionBounds, null, null);

    private static PointerPress PressWith(
        string role,
        Guid? entityId,
        IReadOnlyList<PointerHit>? hits,
        bool alt = false,
        bool shift = false,
        bool ctrl = false
    ) =>
        PointerEvents.Press(role, PointerPress.PrimaryButton, 10, 10, entityId, shift) with
        {
            AltKey = alt,
            CtrlKey = ctrl,
            Hits = hits,
        };

    private static async Task Click(IRenderedComponent<DiagramCanvas> canvas, PointerPress press)
    {
        await canvas.InvokeAsync(() => canvas.Instance.OnPointerPressed(press));
        await canvas.Release(press.X, press.Y);
    }

    private Task AltClickOnTopOfTheStack(IRenderedComponent<DiagramCanvas> canvas) =>
        Click(
            canvas,
            PressWith(
                HitRole.Instance,
                _top.Id,
                [InstanceHit(_top), InstanceHit(_middle), InstanceHit(_bottom)],
                alt: true
            )
        );

    private static async Task Select(
        IRenderedComponent<DiagramCanvas> canvas,
        params ComponentInstance[] instances
    )
    {
        await Click(canvas, PressWith(HitRole.Instance, instances[0].Id, null));
        foreach (var instance in instances.Skip(1))
        {
            await Click(canvas, PressWith(HitRole.Instance, instance.Id, null, shift: true));
        }
    }

    private static HashSet<Guid> SelectedIds(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Instance.SelectedComponents.Select(instance => instance.Id).ToHashSet();

    private static HashSet<Guid> SelectedEdgeIds(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Instance.SelectedEdges.Select(edge => edge.Id).ToHashSet();

    [Fact]
    public async Task AClickInsideTheSelectionBoxSelectsTheShapeBeneathIt()
    {
        var canvas = RenderCanvas();
        await Select(canvas, _bottom, _top);

        await Click(
            canvas,
            PressWith(HitRole.SelectionBounds, null, [SelectionBoxHit, InstanceHit(_middle)])
        );

        Assert.Equal([_middle.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task AShiftClickInsideTheSelectionBoxAddsTheShapeBeneathIt()
    {
        var canvas = RenderCanvas();
        await Select(canvas, _bottom, _top);

        await Click(
            canvas,
            PressWith(
                HitRole.SelectionBounds,
                null,
                [SelectionBoxHit, InstanceHit(_middle)],
                shift: true
            )
        );

        Assert.Equal(new HashSet<Guid> { _bottom.Id, _middle.Id, _top.Id }, SelectedIds(canvas));
    }

    [Fact]
    public async Task AShiftClickInsideTheSelectionBoxOnASelectedShapeTogglesItOut()
    {
        var canvas = RenderCanvas();
        await Select(canvas, _bottom, _top);

        await Click(
            canvas,
            PressWith(
                HitRole.SelectionBounds,
                null,
                [SelectionBoxHit, InstanceHit(_top), InstanceHit(_bottom)],
                shift: true
            )
        );

        Assert.Equal([_bottom.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task AClickInsideTheSelectionBoxOverNothingKeepsTheSelection()
    {
        var canvas = RenderCanvas();
        await Select(canvas, _bottom, _top);

        await Click(canvas, PressWith(HitRole.SelectionBounds, null, [SelectionBoxHit]));

        Assert.Equal(new HashSet<Guid> { _bottom.Id, _top.Id }, SelectedIds(canvas));
    }

    [Fact]
    public async Task ADragFromInsideTheSelectionBoxMovesTheSelectionAndNotTheShapeBeneath()
    {
        var canvas = RenderCanvas();
        await Select(canvas, _bottom, _top);
        var middleBefore = _middle.Bounds;

        await canvas.InvokeAsync(
            () =>
                canvas.Instance.OnPointerPressed(
                    PressWith(
                        HitRole.SelectionBounds,
                        null,
                        [SelectionBoxHit, InstanceHit(_middle)]
                    )
                )
        );
        await canvas.Move(60, 10);
        await canvas.Release(60, 10);

        Assert.Equal(new HashSet<Guid> { _bottom.Id, _top.Id }, SelectedIds(canvas));
        Assert.Equal(90, _top.Bounds.X);
        Assert.Equal(middleBefore, _middle.Bounds);
    }

    [Fact]
    public async Task AltClicksOnAStackOfThreeStepDownOneAtATimeAndWrapToTheTop()
    {
        var canvas = RenderCanvas();

        await AltClickOnTopOfTheStack(canvas);
        Assert.Equal([_middle.Id], SelectedIds(canvas));

        await AltClickOnTopOfTheStack(canvas);
        Assert.Equal([_bottom.Id], SelectedIds(canvas));

        await AltClickOnTopOfTheStack(canvas);
        Assert.Equal([_top.Id], SelectedIds(canvas));

        await AltClickOnTopOfTheStack(canvas);
        Assert.Equal([_middle.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task AQuickSecondAltClickCyclesRatherThanCountingAsADoublePress()
    {
        var canvas = RenderCanvas();
        var stack = new[] { InstanceHit(_top), InstanceHit(_middle), InstanceHit(_bottom) };

        await Click(canvas, PressWith(HitRole.Instance, _top.Id, stack, alt: true));
        await Click(
            canvas,
            PressWith(HitRole.Instance, _top.Id, stack, alt: true) with
            {
                PressCount = 2,
            }
        );

        Assert.Equal([_bottom.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task AnAltClickFromAMultiSelectionSelectsTheEntryBelowTheTop()
    {
        var canvas = RenderCanvas();
        await Select(canvas, _bottom, _top);

        await AltClickOnTopOfTheStack(canvas);

        Assert.Equal([_middle.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task ShiftAltClickCyclesAsAltClickDoes()
    {
        var canvas = RenderCanvas();
        await Select(canvas, _middle);

        await Click(
            canvas,
            PressWith(
                HitRole.Instance,
                _top.Id,
                [InstanceHit(_top), InstanceHit(_middle), InstanceHit(_bottom)],
                alt: true,
                shift: true
            )
        );

        Assert.Equal([_bottom.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task AnAltClickOnAStackOfOneSelectsThatEntry()
    {
        var canvas = RenderCanvas();
        await Select(canvas, _outsider);

        await Click(canvas, PressWith(HitRole.Instance, _top.Id, [InstanceHit(_top)], alt: true));

        Assert.Equal([_top.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task AnAltClickOnAGroupedMemberResolvesTheStackThroughItsGroup()
    {
        var canvas = RenderCanvas();
        var groupMembers = new HashSet<Guid> { _firstMember.Id, _secondMember.Id };
        var stack = new[]
        {
            InstanceHit(_top),
            InstanceHit(_secondMember),
            InstanceHit(_firstMember),
            InstanceHit(_bottom),
        };
        await Select(canvas, _top);

        await Click(canvas, PressWith(HitRole.Instance, _top.Id, stack, alt: true));
        Assert.Equal(groupMembers, SelectedIds(canvas));

        await Click(canvas, PressWith(HitRole.Instance, _top.Id, stack, alt: true));
        Assert.Equal([_bottom.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task InsideAnEnteredGroupAnAltClickReachesTheMemberBeneath()
    {
        var canvas = RenderCanvas();
        await Click(canvas, PressWith(HitRole.Instance, _secondMember.Id, null));
        await Click(
            canvas,
            PressWith(HitRole.Instance, _secondMember.Id, null) with
            {
                PressCount = 2,
            }
        );
        Assert.Equal([_secondMember.Id], SelectedIds(canvas));

        await Click(
            canvas,
            PressWith(
                HitRole.Instance,
                _secondMember.Id,
                [InstanceHit(_secondMember), InstanceHit(_firstMember)],
                alt: true
            )
        );

        Assert.Equal([_firstMember.Id], SelectedIds(canvas));
        Assert.Single(canvas.FindAll(".entered-group-outline"));
    }

    [Fact]
    public async Task AnAltClickReachingOutsideTheEnteredGroupStepsOutOfIt()
    {
        var canvas = RenderCanvas();
        await Click(canvas, PressWith(HitRole.Instance, _secondMember.Id, null));
        await Click(
            canvas,
            PressWith(HitRole.Instance, _secondMember.Id, null) with
            {
                PressCount = 2,
            }
        );

        await Click(
            canvas,
            PressWith(
                HitRole.Instance,
                _secondMember.Id,
                [InstanceHit(_secondMember), InstanceHit(_outsider)],
                alt: true
            )
        );

        Assert.Equal([_outsider.Id], SelectedIds(canvas));
        Assert.Empty(canvas.FindAll(".entered-group-outline"));
    }

    [Fact]
    public async Task AnAltClickThatStepsOutCyclesFromTheGroupHoldingTheMemberSelectedBefore()
    {
        var canvas = RenderCanvas();
        await Click(canvas, PressWith(HitRole.Instance, _secondMember.Id, null));
        await Click(
            canvas,
            PressWith(HitRole.Instance, _secondMember.Id, null) with
            {
                PressCount = 2,
            }
        );

        await Click(
            canvas,
            PressWith(
                HitRole.Instance,
                _outsider.Id,
                [InstanceHit(_outsider), InstanceHit(_secondMember)],
                alt: true
            )
        );

        Assert.Equal([_outsider.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task ACtrlAltClickSelectsThePressedShapeAsAPlainClickDoes()
    {
        var canvas = RenderCanvas();

        await Click(
            canvas,
            PressWith(
                HitRole.Instance,
                _top.Id,
                [InstanceHit(_top), InstanceHit(_middle)],
                alt: true,
                ctrl: true
            )
        );

        Assert.Equal([_top.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task EdgesStayInTheStackAndAnAltClickOnAnEdgeCyclesOnFromIt()
    {
        var canvas = RenderCanvas();
        var stack = new[] { InstanceHit(_top), EdgeHit(_edge) };
        await Select(canvas, _top);

        await Click(canvas, PressWith(HitRole.Instance, _top.Id, stack, alt: true));
        Assert.Empty(SelectedIds(canvas));
        Assert.Equal([_edge.Id], SelectedEdgeIds(canvas));

        await Click(
            canvas,
            PressWith(HitRole.Edge, _edge.Id, [EdgeHit(_edge), InstanceHit(_bottom)], alt: true)
        );
        Assert.Equal([_bottom.Id], SelectedIds(canvas));
        Assert.Empty(SelectedEdgeIds(canvas));
    }

    [Fact]
    public async Task AnAltPressSelectsThePressedShapeAtPressAsAPlainPressDoes()
    {
        var canvas = RenderCanvas();

        await canvas.InvokeAsync(
            () =>
                canvas.Instance.OnPointerPressed(
                    PressWith(
                        HitRole.Instance,
                        _top.Id,
                        [InstanceHit(_top), InstanceHit(_middle)],
                        alt: true
                    )
                )
        );

        Assert.Equal([_top.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task AnAltDragMovesThePressedShapeAndCyclesNothing()
    {
        var canvas = RenderCanvas();

        await canvas.InvokeAsync(
            () =>
                canvas.Instance.OnPointerPressed(
                    PressWith(
                        HitRole.Instance,
                        _top.Id,
                        [InstanceHit(_top), InstanceHit(_middle)],
                        alt: true
                    )
                )
        );
        await canvas.Move(30, 10);
        await canvas.Release(30, 10);

        Assert.Equal([_top.Id], SelectedIds(canvas));
        Assert.Equal(60, _top.Bounds.X);
    }

    [Fact]
    public async Task ACtrlClickOnAStackSelectsThePressedShapeAsAPlainClickDoes()
    {
        var canvas = RenderCanvas();
        await Select(canvas, _middle);

        await Click(
            canvas,
            PressWith(
                HitRole.Instance,
                _top.Id,
                [InstanceHit(_top), InstanceHit(_middle), InstanceHit(_bottom)],
                ctrl: true
            )
        );

        Assert.Equal([_top.Id], SelectedIds(canvas));
    }

    [Fact]
    public async Task ACancelledAltPressRestoresTheSelectionFromBeforeIt()
    {
        var canvas = RenderCanvas();
        await Select(canvas, _middle);

        await canvas.InvokeAsync(
            () =>
                canvas.Instance.OnPointerPressed(
                    PressWith(
                        HitRole.Instance,
                        _top.Id,
                        [InstanceHit(_top), InstanceHit(_middle)],
                        alt: true
                    )
                )
        );
        await canvas.Cancel("pointercancel");
        await canvas.Release(10, 10);

        Assert.Equal([_middle.Id], SelectedIds(canvas));
    }
}
