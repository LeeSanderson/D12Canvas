using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The board is an outer group holding one instance and a nested group of two, and one instance
// outside them. A double-press steps one level inside a group, Escape steps back out one level,
// and a press outside the entered group steps out as far as it needs to.
public class DiagramCanvasEnteredGroupTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    private readonly Board _board = new();
    private readonly ComponentInstance _outerMember;
    private readonly ComponentInstance _nestedFirst;
    private readonly ComponentInstance _nestedSecond;
    private readonly ComponentInstance _outsider;
    private readonly Group _nestedGroup;
    private readonly Group _outerGroup;

    public DiagramCanvasEnteredGroupTests()
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

        _outerMember = AddInstance(0);
        _nestedFirst = AddInstance(100);
        _nestedSecond = AddInstance(200);
        _outsider = AddInstance(400);
        _nestedGroup = new Group([_nestedFirst.Id, _nestedSecond.Id]);
        _outerGroup = new Group([_outerMember.Id, _nestedGroup.Id]);
        _board.AddGroup(_nestedGroup);
        _board.AddGroup(_outerGroup);
    }

    private ComponentInstance AddInstance(double x, double y = 0)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 50, 50)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas() =>
        Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, _board)).ReturnToOrigin();

    private static IElement ContainerOf(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.Find($".component-container[data-d12-entity='{id}']");

    private static bool IsSelected(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        ContainerOf(canvas, id).GetAttribute("aria-selected") == "true";

    private static bool IsUnaddressable(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        ContainerOf(canvas, id).HasAttribute("data-d12-unaddressable");

    private static void DoubleClick(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.DoubleClickElement(ContainerOf(canvas, id), (10, 10));

    private static Task PressEscape(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

    private static (double Left, double Top, double Width, double Height) OutlineBox(
        IRenderedComponent<DiagramCanvas> canvas
    )
    {
        var style = canvas.Find(".entered-group-outline").GetAttribute("style")!;
        double Read(string name)
        {
            var start = style.IndexOf($"{name}: ", StringComparison.Ordinal) + name.Length + 2;
            var end = style.IndexOf("px", start, StringComparison.Ordinal);
            return double.Parse(
                style[start..end],
                System.Globalization.CultureInfo.InvariantCulture
            );
        }

        return (Read("left"), Read("top"), Read("width"), Read("height"));
    }

    [Fact]
    public void ADoublePressOnAMemberEntersItsGroupAndSelectsThatMember()
    {
        var canvas = RenderCanvas();

        DoubleClick(canvas, _outerMember.Id);

        Assert.True(IsSelected(canvas, _outerMember.Id));
        Assert.False(IsSelected(canvas, _nestedFirst.Id));
        Assert.Equal([_outerMember.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));
    }

    [Fact]
    public void ADoublePressOnANestedMemberSelectsItsGroupAndASecondEntersOneLevelDeeper()
    {
        var canvas = RenderCanvas();

        DoubleClick(canvas, _nestedSecond.Id);

        Assert.Equal(
            new HashSet<Guid> { _nestedFirst.Id, _nestedSecond.Id },
            canvas.Instance.SelectedComponents.Select(i => i.Id).ToHashSet()
        );

        DoubleClick(canvas, _nestedSecond.Id);

        Assert.Equal([_nestedSecond.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));
    }

    [Fact]
    public async Task ADoublePressOnTheSelectionBoxOverASelectedGroupEntersItAtTheMemberUnderIt()
    {
        var canvas = RenderCanvas();
        canvas.ClickElement(ContainerOf(canvas, _outerMember.Id), at: (10, 10));

        await canvas.InvokeAsync(() =>
        {
            canvas.Instance.OnPointerPressed(
                PointerEvents.Press(
                    HitRole.SelectionBounds,
                    PointerPress.PrimaryButton,
                    110,
                    10,
                    pressCount: 2
                )
            );
            canvas.Instance.OnPointerReleased(
                PointerEvents.Release(PointerPress.PrimaryButton, 110, 10)
            );
        });

        Assert.Equal(
            new HashSet<Guid> { _nestedFirst.Id, _nestedSecond.Id },
            canvas.Instance.SelectedComponents.Select(i => i.Id).ToHashSet()
        );
        Assert.False(IsUnaddressable(canvas, _outerMember.Id));
    }

    [Fact]
    public void InsideAGroupAPlainPressSelectsOnlyThatMemberAndADragMovesOnlyIt()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);

        canvas.DragOn(ContainerOf(canvas, _nestedFirst.Id), (110, 10), (110, 70));

        Assert.Equal(new Bounds(100, 60, 50, 50), _nestedFirst.Bounds);
        Assert.Equal(new Bounds(200, 60, 50, 50), _nestedSecond.Bounds);
        Assert.Equal(new Bounds(0, 0, 50, 50), _outerMember.Bounds);
        Assert.False(IsSelected(canvas, _outerMember.Id));
    }

    [Fact]
    public async Task AMarqueeInsideTheEnteredGroupsBoundsSelectsItsMembersAndNothingOutside()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);

        await canvas.Marquee((5, 45), (450, 48));

        Assert.True(IsSelected(canvas, _outerMember.Id));
        Assert.True(IsSelected(canvas, _nestedFirst.Id));
        Assert.True(IsSelected(canvas, _nestedSecond.Id));
        Assert.False(IsSelected(canvas, _outsider.Id));
        Assert.Single(canvas.FindAll(".entered-group-outline"));
    }

    [Fact]
    public async Task AClickOnEmptyCanvasInsideTheGroupsBoundsKeepsTheScopeAndClearsTheSelection()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);

        await canvas.ClickCanvas(75, 25);

        Assert.Empty(canvas.Instance.SelectedComponents);
        Assert.Single(canvas.FindAll(".entered-group-outline"));
        Assert.False(IsUnaddressable(canvas, _outerMember.Id));
    }

    [Fact]
    public async Task EscapeStepsOutOneLevelAndSelectsTheGroupJustLeft()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _nestedSecond.Id);
        DoubleClick(canvas, _nestedSecond.Id);

        await PressEscape(canvas);

        Assert.Equal(
            new HashSet<Guid> { _nestedFirst.Id, _nestedSecond.Id },
            canvas.Instance.SelectedComponents.Select(i => i.Id).ToHashSet()
        );
        Assert.False(IsSelected(canvas, _outerMember.Id));

        await PressEscape(canvas);

        Assert.Equal(3, canvas.Instance.SelectedComponents.Count);
        Assert.Empty(canvas.FindAll(".entered-group-outline"));

        await PressEscape(canvas);

        Assert.Empty(canvas.Instance.SelectedComponents);
    }

    [Fact]
    public async Task APressOnCanvasOutsideTheGroupStepsAllTheWayOut()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _nestedSecond.Id);
        DoubleClick(canvas, _nestedSecond.Id);

        await canvas.ClickCanvas(600, 300);

        Assert.Empty(canvas.FindAll(".entered-group-outline"));
        Assert.True(IsUnaddressable(canvas, _outerMember.Id));
        Assert.True(IsUnaddressable(canvas, _nestedSecond.Id));
    }

    [Fact]
    public async Task ARightDragPanFromOutsideTheGroupLeavesTheScopeButARightClickThereStepsOut()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);

        await canvas.Drag((600, 300), (620, 300), PointerPress.SecondaryButton);

        Assert.Single(canvas.FindAll(".entered-group-outline"));
        Assert.Equal([_outerMember.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));

        await canvas.ClickCanvas(600, 300, PointerPress.SecondaryButton);

        Assert.Empty(canvas.FindAll(".entered-group-outline"));
        Assert.Empty(canvas.Instance.SelectedComponents);
    }

    [Fact]
    public async Task AMarqueeInsideTheEnteredGroupTakesNoEdge()
    {
        _board.AddEdge(
            new Edge(
                new PortEndpoint(_outerMember.Id, PortId.Right),
                new PortEndpoint(_nestedFirst.Id, PortId.Left)
            )
        );
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);

        await canvas.Marquee((5, 45), (240, 48));

        Assert.Empty(canvas.Instance.SelectedEdges);
        Assert.True(IsSelected(canvas, _nestedFirst.Id));
    }

    [Fact]
    public async Task EscapeMidPressRestoresTheGroupThePressSteppedOutOf()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _nestedSecond.Id);
        DoubleClick(canvas, _nestedSecond.Id);

        canvas.PressOn(ContainerOf(canvas, _outsider.Id), (410, 10));
        canvas.MoveTo((430, 30));
        await PressEscape(canvas);
        canvas.ReleaseAt((430, 30));

        Assert.Equal([_nestedSecond.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));
        Assert.Single(canvas.FindAll(".entered-group-outline"));
        Assert.False(IsUnaddressable(canvas, _nestedFirst.Id));
        Assert.Equal(new Bounds(400, 0, 50, 50), _outsider.Bounds);
    }

    [Fact]
    public async Task APointerCancelRestoresTheGroupThePressSteppedOutOf()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);

        await canvas.Press(600, 300);
        await canvas.Move(650, 350);
        await canvas.Cancel("pointercancel");

        Assert.Equal([_outerMember.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));
        Assert.Single(canvas.FindAll(".entered-group-outline"));
    }

    [Fact]
    public void FocusLandingOnAStopOutsideTheEnteredGroupStepsOutToIt()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _nestedFirst.Id);
        DoubleClick(canvas, _nestedFirst.Id);

        ContainerOf(canvas, _outsider.Id).Focus();

        Assert.Equal([_outsider.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));
        Assert.Empty(canvas.FindAll(".entered-group-outline"));
    }

    [Fact]
    public void APressOnAnEntityOutsideTheEnteredGroupStepsOutUntilItIsInside()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _nestedSecond.Id);
        DoubleClick(canvas, _nestedSecond.Id);

        canvas.ClickElement(ContainerOf(canvas, _outerMember.Id), at: (10, 10));

        Assert.Equal([_outerMember.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));
        Assert.True(IsUnaddressable(canvas, _nestedFirst.Id));

        canvas.ClickElement(ContainerOf(canvas, _outsider.Id), at: (410, 10));

        Assert.Equal([_outsider.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));
        Assert.True(IsUnaddressable(canvas, _outerMember.Id));
        Assert.Empty(canvas.FindAll(".entered-group-outline"));
    }

    [Fact]
    public void OnlyTopLevelInstancesAndTheEnteredGroupsDirectMembersAreAddressable()
    {
        var canvas = RenderCanvas();

        Assert.True(IsUnaddressable(canvas, _outerMember.Id));
        Assert.True(IsUnaddressable(canvas, _nestedFirst.Id));
        Assert.False(IsUnaddressable(canvas, _outsider.Id));

        DoubleClick(canvas, _outerMember.Id);

        Assert.False(IsUnaddressable(canvas, _outerMember.Id));
        Assert.True(IsUnaddressable(canvas, _nestedFirst.Id));
        Assert.True(IsUnaddressable(canvas, _nestedSecond.Id));
        Assert.False(IsUnaddressable(canvas, _outsider.Id));

        DoubleClick(canvas, _nestedFirst.Id);

        Assert.True(IsUnaddressable(canvas, _outerMember.Id));
        Assert.False(IsUnaddressable(canvas, _nestedFirst.Id));
        Assert.False(IsUnaddressable(canvas, _nestedSecond.Id));
    }

    [Fact]
    public void TheEnteredGroupsStopGivesWayToStopsForItsDirectMembers()
    {
        var canvas = RenderCanvas();

        Assert.Single(canvas.FindAll(".group-tab-stop"));
        Assert.Null(ContainerOf(canvas, _outerMember.Id).GetAttribute("tabindex"));

        DoubleClick(canvas, _outerMember.Id);

        var groupStop = Assert.Single(canvas.FindAll(".group-tab-stop"));
        Assert.Equal("Group (2 items)", groupStop.GetAttribute("aria-label"));
        Assert.Equal("0", ContainerOf(canvas, _outerMember.Id).GetAttribute("tabindex"));
        Assert.Null(ContainerOf(canvas, _nestedFirst.Id).GetAttribute("tabindex"));
        Assert.Equal("0", ContainerOf(canvas, _outsider.Id).GetAttribute("tabindex"));
    }

    [Fact]
    public void TheDashedOutlineIsDrawnAroundTheInnermostEnteredGroupOnly()
    {
        var canvas = RenderCanvas();
        Assert.Empty(canvas.FindAll(".entered-group-outline"));

        DoubleClick(canvas, _outerMember.Id);

        Assert.Equal((-4, -4, 258, 58), OutlineBox(canvas));

        DoubleClick(canvas, _nestedFirst.Id);

        Assert.Single(canvas.FindAll(".entered-group-outline"));
        Assert.Equal((96, -4, 158, 58), OutlineBox(canvas));
    }

    [Fact]
    public async Task EnterOnAGroupsTabStopEntersItAndSelectsItsFirstMember()
    {
        _board.AddEdge(
            new Edge(
                new PortEndpoint(_outerMember.Id, PortId.Right),
                new PortEndpoint(_outsider.Id, PortId.Left)
            )
        );
        var canvas = RenderCanvas();
        canvas.Find(".group-tab-stop").Focus();

        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

        Assert.Equal([_outerMember.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));
        Assert.Single(canvas.FindAll(".entered-group-outline"));
        Assert.Empty(canvas.FindAll(".edge-band [tabindex]"));
    }

    [Fact]
    public async Task EscapeAfterAKeyboardEntryHandsFocusBackToTheGroupsStop()
    {
        var canvas = RenderCanvas();
        canvas.Find(".group-tab-stop").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        var focusCallsBefore = JSInterop.Invocations["focusTabStopAt"].Count;

        await PressEscape(canvas);

        Assert.Equal(focusCallsBefore + 1, JSInterop.Invocations["focusTabStopAt"].Count);
        Assert.Equal("true", canvas.Find(".group-tab-stop").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task GroupingTwoMembersInsideAnEnteredGroupNestsTheNewGroupInItAndUndoRestoresIt()
    {
        var loose = AddInstance(0, 100);
        _board.RemoveGroup(_outerGroup.Id);
        var outer = new Group([_outerMember.Id, loose.Id, _nestedGroup.Id]);
        _board.AddGroup(outer);
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);
        canvas.ClickElement(ContainerOf(canvas, loose.Id), at: (10, 110), shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        var nested = Assert.Single(
            _board.Groups,
            group => group.Id != outer.Id && group.Id != _nestedGroup.Id
        );
        Assert.Equal(new HashSet<Guid> { _outerMember.Id, loose.Id }, nested.MemberIds.ToHashSet());
        Assert.Equal([nested.Id, _nestedGroup.Id], _board.GetGroup(outer.Id)!.MemberIds);
        Assert.Single(canvas.FindAll(".entered-group-outline"));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Null(_board.GetGroup(nested.Id));
        Assert.Equal(
            [_outerMember.Id, loose.Id, _nestedGroup.Id],
            _board.GetGroup(outer.Id)!.MemberIds
        );
    }

    [Fact]
    public async Task GroupIsUnavailableWhenEveryDirectMemberOfTheEnteredGroupIsSelected()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);
        await canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        Assert.Equal(2, _board.Groups.Count);
        Assert.Equal(
            [_outerMember.Id, _nestedGroup.Id],
            _board.GetGroup(_outerGroup.Id)!.MemberIds
        );
    }

    [Fact]
    public async Task UngroupingANestedGroupSplicesItsMembersIntoTheParentWhereItWas()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _nestedFirst.Id);

        await canvas.InvokeAsync(() => canvas.Instance.OnUngroupPressed());

        Assert.Null(_board.GetGroup(_nestedGroup.Id));
        Assert.Equal(
            [_outerMember.Id, _nestedFirst.Id, _nestedSecond.Id],
            _board.GetGroup(_outerGroup.Id)!.MemberIds
        );
        Assert.True(IsSelected(canvas, _nestedFirst.Id));
        Assert.True(IsSelected(canvas, _nestedSecond.Id));
        Assert.False(IsSelected(canvas, _outerMember.Id));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(
            [_outerMember.Id, _nestedGroup.Id],
            _board.GetGroup(_outerGroup.Id)!.MemberIds
        );
    }

    [Fact]
    public async Task TheScopeFallsBackToTheNearestGroupStillInPlaceWhenAnUndoRemovesTheEnteredOne()
    {
        var loose = AddInstance(0, 100);
        _board.RemoveGroup(_outerGroup.Id);
        var outer = new Group([_outerMember.Id, loose.Id, _nestedGroup.Id]);
        _board.AddGroup(outer);
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);
        canvas.ClickElement(ContainerOf(canvas, loose.Id), at: (10, 110), shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        DoubleClick(canvas, _outerMember.Id);
        Assert.Equal((-4, -4, 58, 158), OutlineBox(canvas));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal((-4, -4, 258, 158), OutlineBox(canvas));
        Assert.False(IsUnaddressable(canvas, _outerMember.Id));
    }

    [Fact]
    public void ADoublePressOnAnAddressableMemberDoesNotEnterAnything()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);

        DoubleClick(canvas, _outerMember.Id);

        Assert.Equal([_outerMember.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));
        Assert.Equal((-4, -4, 258, 58), OutlineBox(canvas));
    }

    [Fact]
    public async Task AMiddlePressOutsideTheGroupLeavesTheScopeAlone()
    {
        var canvas = RenderCanvas();
        DoubleClick(canvas, _outerMember.Id);

        await canvas.Pan((600, 300), (620, 300));

        Assert.Single(canvas.FindAll(".entered-group-outline"));
    }
}
