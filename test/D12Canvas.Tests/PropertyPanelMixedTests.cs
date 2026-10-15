using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using Bunit;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public partial class PropertyPanelTests
{
    private (
        IRenderedComponent<DiagramCanvas> Canvas,
        IRenderedComponent<PropertyPanel> Panel
    ) RenderWithBothSelected(Board board)
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        return (canvas, panel);
    }

    private static PanelTestProps PropsOf(ComponentInstance instance) =>
        (PanelTestProps)instance.Props;

    [Fact]
    public async Task DifferentColoursShowAHatchedSwatchAndAPickWritesBothInOneEntry()
    {
        var board = new Board();
        var first = AddInstance(board, tint: "#ff0000");
        var second = AddInstance(board, tint: "#00ff00");
        var (canvas, panel) = RenderWithBothSelected(board);

        var swatch = panel.Find("#d12-property-panel-field-Tint");
        Assert.Contains("d12-property-panel-color-mixed", swatch.ClassList);
        Assert.Equal("Mixed", panel.Find(".d12-property-panel-mixed-label").TextContent);

        swatch.Change("#0000ff");

        Assert.Equal("#0000ff", PropsOf(first).Tint);
        Assert.Equal("#0000ff", PropsOf(second).Tint);
        Assert.DoesNotContain(
            "d12-property-panel-color-mixed",
            panel.Find("#d12-property-panel-field-Tint").ClassList
        );

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#ff0000", PropsOf(first).Tint);
        Assert.Equal("#00ff00", PropsOf(second).Tint);
    }

    // The swatch opens on the colour it holds, and the browser fires no change when the pick ends
    // on that same colour, so the last colour the picker reported lands when the swatch is left.
    [Fact]
    public void PickingTheColourAMixedSwatchHoldsStillWritesEveryTarget()
    {
        var board = new Board();
        var first = AddInstance(board, tint: "#ff0000");
        var second = AddInstance(board, tint: "#00ff00");
        var (_, panel) = RenderWithBothSelected(board);
        var swatchValue = panel.Find("#d12-property-panel-field-Tint").GetAttribute("value")!;

        panel.Find("#d12-property-panel-field-Tint").Input("#123456");
        panel.Find("#d12-property-panel-field-Tint").Input(swatchValue);
        panel.Find("#d12-property-panel-field-Tint").Blur();

        Assert.Equal(swatchValue, PropsOf(first).Tint);
        Assert.Equal(swatchValue, PropsOf(second).Tint);
    }

    [Fact]
    public void LeavingAMixedSwatchWithoutPickingWritesNothing()
    {
        var board = new Board();
        var first = AddInstance(board, tint: "#ff0000");
        AddInstance(board, tint: "#00ff00");
        var (_, panel) = RenderWithBothSelected(board);

        panel.Find("#d12-property-panel-field-Tint").Blur();

        Assert.Equal("#ff0000", PropsOf(first).Tint);
    }

    [Fact]
    public void ColoursDifferingOnlyInCaseAreNotMixed()
    {
        var board = new Board();
        AddInstance(board, tint: "#FFEB3B");
        AddInstance(board, tint: "#ffeb3b");
        var (_, panel) = RenderWithBothSelected(board);

        Assert.DoesNotContain(
            "d12-property-panel-color-mixed",
            panel.Find("#d12-property-panel-field-Tint").ClassList
        );
        Assert.Empty(panel.FindAll(".d12-property-panel-mixed-label"));
    }

    [Fact]
    public void AThemedColourBesideALiteralIsMixedRatherThanThemed()
    {
        var board = new Board();
        AddInstance(board, tint: null);
        AddInstance(board, tint: "#ffffff");
        var (_, panel) = RenderWithBothSelected(board);

        var swatch = panel.Find("#d12-property-panel-field-Tint");
        Assert.Contains("d12-property-panel-color-mixed", swatch.ClassList);
        Assert.DoesNotContain("d12-property-panel-color-themed", swatch.ClassList);
        Assert.Empty(panel.FindAll(".d12-property-panel-themed-label"));
        Assert.NotEmpty(panel.FindAll(".d12-property-panel-clear"));
    }

    [Fact]
    public async Task ACommitSkipsATargetAlreadyHoldingTheValueComparingColoursWithoutCase()
    {
        var board = new Board();
        var holder = AddInstance(board, tint: "#00FF00");
        var other = AddInstance(board, tint: "#ff0000");
        var (canvas, panel) = RenderWithBothSelected(board);
        var holderProps = holder.Props;

        panel.Find("#d12-property-panel-field-Tint").Change("#00ff00");

        Assert.Same(holderProps, holder.Props);
        Assert.Equal("#00ff00", PropsOf(other).Tint);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#ff0000", PropsOf(other).Tint);
        Assert.Same(holderProps, holder.Props);
    }

    [Theory]
    [InlineData("#d12-property-panel-field-Label")]
    [InlineData("#d12-property-panel-field-Count")]
    public void AMixedTextOrNumberRowIsEmptyWithAMixedPlaceholder(string selector)
    {
        var board = new Board();
        AddInstance(board, label: "One", count: 1);
        AddInstance(board, label: "Two", count: 2);
        var (_, panel) = RenderWithBothSelected(board);

        var input = panel.Find(selector);
        Assert.Equal("", input.GetAttribute("value"));
        Assert.Equal("Mixed", input.GetAttribute("placeholder"));
    }

    [Fact]
    public void AnAgreeingRowCarriesNoPlaceholder()
    {
        var board = new Board();
        AddInstance(board, label: "Same", count: 1);
        AddInstance(board, label: "Same", count: 1);
        var (_, panel) = RenderWithBothSelected(board);

        var label = panel.Find("#d12-property-panel-field-Label");
        Assert.Equal("Same", label.GetAttribute("value"));
        Assert.False(label.HasAttribute("placeholder"));
        Assert.False(panel.Find("#d12-property-panel-field-Count").HasAttribute("placeholder"));
    }

    [Fact]
    public async Task TypingIntoAMixedTextRowWritesEveryTargetInOneEntry()
    {
        var board = new Board();
        var first = AddInstance(board, label: "One");
        var second = AddInstance(board, label: "Two");
        var (canvas, panel) = RenderWithBothSelected(board);

        panel.Find("#d12-property-panel-field-Label").Change("Both");

        Assert.Equal("Both", PropsOf(first).Label);
        Assert.Equal("Both", PropsOf(second).Label);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("One", PropsOf(first).Label);
        Assert.Equal("Two", PropsOf(second).Label);
    }

    [Fact]
    public void TypingIntoAMixedNumberRowWritesEveryTarget()
    {
        var board = new Board();
        var first = AddInstance(board, count: 1);
        var second = AddInstance(board, count: 2);
        var (_, panel) = RenderWithBothSelected(board);

        panel.Find("#d12-property-panel-field-Count").Change("7");

        Assert.Equal(7, PropsOf(first).Count);
        Assert.Equal(7, PropsOf(second).Count);
    }

    [Fact]
    public void AMixedDropdownSelectsADisabledMixedOptionAndAPickWritesEveryTarget()
    {
        var board = new Board();
        var first = AddInstance(board, mode: "a");
        var second = AddInstance(board, mode: "b");
        var (_, panel) = RenderWithBothSelected(board);

        var select = panel.Find("#d12-property-panel-field-Mode");
        var options = select.QuerySelectorAll("option");
        Assert.Equal(["", "a", "b", "c"], options.Select(option => option.GetAttribute("value")));
        Assert.Equal("Mixed", options[0].TextContent);
        Assert.True(options[0].HasAttribute("disabled"));
        Assert.True(options[0].HasAttribute("selected"));
        Assert.Equal("", ((IHtmlSelectElement)select).Value);

        select.Change("c");

        Assert.Equal("c", PropsOf(first).Mode);
        Assert.Equal("c", PropsOf(second).Mode);
        Assert.Equal(
            ["a", "b", "c"],
            panel
                .Find("#d12-property-panel-field-Mode")
                .QuerySelectorAll("option")
                .Select(option => option.GetAttribute("value"))
        );
    }

    [Fact]
    public void AMixedCheckboxIsSetIndeterminateAndClearedOnceACommitResolvesIt()
    {
        var board = new Board();
        var first = AddInstance(board, flag: true);
        var second = AddInstance(board, flag: false);
        var (_, panel) = RenderWithBothSelected(board);

        var checkbox = panel.Find("#d12-property-panel-field-Flag");
        Assert.False(((IHtmlInputElement)checkbox).IsChecked);
        Assert.True(IndeterminateWrites().Last());

        checkbox.Change(true);

        Assert.True(PropsOf(first).Flag);
        Assert.True(PropsOf(second).Flag);
        Assert.False(IndeterminateWrites().Last());
    }

    [Fact]
    public void AnAgreeingCheckboxIsNeverSetIndeterminate()
    {
        var board = new Board();
        AddInstance(board, flag: true);
        AddInstance(board, flag: true);
        RenderWithBothSelected(board);

        Assert.Empty(IndeterminateWrites());
    }

    [Fact]
    public void ACustomEditorLearnsWhetherItsTargetsDisagree()
    {
        var board = new Board();
        AddInstance(board, customValue: "one");
        AddInstance(board, customValue: "two");
        var (canvas, panel) = RenderWithBothSelected(board);

        Assert.Equal(
            "true",
            panel.Find($"#{PanelTestCustomEditor.CommitButtonId}").GetAttribute("data-mixed")
        );

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        Assert.Equal(
            "false",
            panel.Find($"#{PanelTestCustomEditor.CommitButtonId}").GetAttribute("data-mixed")
        );
    }

    [Fact]
    public void ACommitThroughAMixedCustomEditorWritesEveryTarget()
    {
        var board = new Board();
        var first = AddInstance(board, customValue: "one");
        var second = AddInstance(board, customValue: "two");
        var (_, panel) = RenderWithBothSelected(board);

        panel.Find($"#{PanelTestCustomEditor.CommitButtonId}").Click();

        Assert.Equal(PanelTestCustomEditor.CommittedValue, PropsOf(first).CustomValue);
        Assert.Equal(PanelTestCustomEditor.CommittedValue, PropsOf(second).CustomValue);
    }

    [Fact]
    public void ACrossTypeRoleRowIsMixedWhenTheTypesDisagree()
    {
        var board = new Board();
        AddInstance(board, tint: "#ff0000");
        AddSecondaryInstance(board, accentColor: "#00ff00");
        var (_, panel) = RenderWithBothSelected(board);

        Assert.Contains(
            "d12-property-panel-color-mixed",
            panel.Find("#d12-property-panel-field-Fill").ClassList
        );
    }

    [Fact]
    public void ASelectedGroupShowsItsMembersDisagreementAndACommitWritesEveryMember()
    {
        var board = new Board();
        var first = AddInstance(board, tint: "#ff0000");
        var second = AddInstance(board, tint: "#00ff00");
        board.AddGroup(new Group([first.Id, second.Id]));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        canvas.ClickOn(canvas.ContainerOf(first.Id));

        var swatch = panel.Find("#d12-property-panel-field-Tint");
        Assert.Contains("d12-property-panel-color-mixed", swatch.ClassList);

        swatch.Change("#0000ff");

        Assert.Equal("#0000ff", PropsOf(first).Tint);
        Assert.Equal("#0000ff", PropsOf(second).Tint);
    }

    [Fact]
    public void APartlyLockedGroupCountsItsLockedMemberAsATargetButWritesOnlyTheUnlocked()
    {
        var board = new Board();
        var free = AddInstance(board, tint: "#ff0000");
        var locked = AddInstance(board, tint: "#00ff00");
        locked.Locked = true;
        board.AddGroup(new Group([free.Id, locked.Id]));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        canvas.ClickOn(canvas.ContainerOf(free.Id));

        var swatch = panel.Find("#d12-property-panel-field-Tint");
        Assert.Contains("d12-property-panel-color-mixed", swatch.ClassList);
        Assert.False(swatch.HasAttribute("disabled"));

        swatch.Change("#00ff00");

        Assert.Equal("#00ff00", PropsOf(free).Tint);
        Assert.Equal("#00ff00", PropsOf(locked).Tint);
        Assert.DoesNotContain(
            "d12-property-panel-color-mixed",
            panel.Find("#d12-property-panel-field-Tint").ClassList
        );
    }

    [Fact]
    public void AFullyLockedMixedSelectionShowsItsMixedRowsDisabled()
    {
        var board = new Board();
        var first = AddInstance(board, label: "One", tint: "#ff0000");
        var second = AddInstance(board, label: "Two", tint: "#00ff00");
        first.Locked = true;
        second.Locked = true;
        board.AddGroup(new Group([first.Id, second.Id]));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        canvas.Find(".group-tab-stop").Focus();

        var swatch = panel.Find("#d12-property-panel-field-Tint");
        Assert.Contains("d12-property-panel-color-mixed", swatch.ClassList);
        Assert.True(swatch.HasAttribute("disabled"));
        var label = panel.Find("#d12-property-panel-field-Label");
        Assert.Equal("Mixed", label.GetAttribute("placeholder"));
        Assert.True(label.HasAttribute("disabled"));
    }

    private IEnumerable<bool> IndeterminateWrites() =>
        PropertyPanelModule
            .Invocations["setIndeterminate"]
            .Select(invocation => (bool)invocation.Arguments[1]!);
}
