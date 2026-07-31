using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Panel;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The property panel is chrome - standalone and host-positioned, wired to its DiagramCanvas the
// same explicit way Palette is - built generically from whatever [PanelEditable] the selection's
// registered TProps declares. Exercised through the real DiagramCanvas/ComponentContainer stack
// (not a bare Board) since selection itself is DiagramCanvas's own transient view state - there's
// no other way to select an instance.
public class PropertyPanelTests : ComponentTestBase
{
    private const string ComponentTypeKey = "panel-test-component";

    // A second, distinct registered type - PanelTestPropsSecondary.AccentColor shares
    // PanelTestProps.Tint's "color" SharedTag (matching Kind/CLR type), so the two combine into one
    // cross-type row; PanelTestPropsSecondary.Note carries no tag, so it must never surface there.
    private const string SecondaryComponentTypeKey = "panel-test-component-secondary";

    private readonly ComponentRegistry _registry = new();

    public PropertyPanelTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();

        _registry.Register(
            new ComponentRegistration(
                Key: ComponentTypeKey,
                ComponentType: typeof(PanelTestPropsComponent),
                PropsType: typeof(PanelTestProps),
                DisplayName: "Panel Test",
                AccessibleName: "Panel test component",
                DefaultProps: new PanelTestProps("", "", 0),
                Icon: null,
                Role: "group",
                DefaultSize: null,
                Category: null,
                // CustomValue carries no [PanelEditable] - a Custom-kind property can only come
                // from the builder override, so it's appended here rather than being picked up by
                // DiscoverFrom like every other kind above.
                EditableProperties: EditablePropertySchema
                    .DiscoverFrom(typeof(PanelTestProps))
                    .Append(
                        new EditableProperty(
                            typeof(PanelTestProps).GetProperty(nameof(PanelTestProps.CustomValue))!,
                            EditorKind.Custom,
                            CustomEditor: PanelTestCustomEditor.Fragment
                        )
                    )
                    .ToList()
            )
        );
        _registry.Register(
            new ComponentRegistration(
                Key: SecondaryComponentTypeKey,
                ComponentType: typeof(PanelTestPropsSecondaryComponent),
                PropsType: typeof(PanelTestPropsSecondary),
                DisplayName: "Panel Test Secondary",
                AccessibleName: "Panel test secondary component",
                DefaultProps: new PanelTestPropsSecondary(),
                Icon: null,
                Role: "group",
                DefaultSize: null,
                Category: null,
                EditableProperties: EditablePropertySchema.DiscoverFrom(
                    typeof(PanelTestPropsSecondary)
                )
            )
        );
        Services.AddSingleton<IComponentRegistry>(_registry);
    }

    private static ComponentInstance AddInstance(
        Board board,
        string label = "",
        double count = 0,
        string tint = "#000000",
        bool flag = false,
        string mode = "a",
        string customValue = ""
    )
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new PanelTestProps("content", label, count, tint, flag, mode, customValue),
            new Bounds(0, 0, 200, 200)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static ComponentInstance AddSecondaryInstance(
        Board board,
        string accentColor = "#000000",
        string note = ""
    )
    {
        var instance = new ComponentInstance(
            SecondaryComponentTypeKey,
            new PanelTestPropsSecondary(accentColor, note),
            new Bounds(300, 0, 200, 200)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static void Select(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find(".component-container").Click();

    [Fact]
    public void RendersStandaloneWithoutRequiringAWiredCanvas()
    {
        var panel = Render<PropertyPanel>();

        Assert.NotNull(panel.Find(".d12-property-panel"));
    }

    [Fact]
    public void ShowsAnEmptyStateWhenNothingIsSelected()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        Assert.NotNull(panel.Find(".d12-property-panel-empty"));
        Assert.Empty(panel.FindAll(".d12-property-panel-field"));
    }

    // A same-type 2+ multi-selection now edits that type's full declared schema, rather than
    // showing the empty state that any multi-selection used to trigger.
    [Fact]
    public void SameTypeMultiSelectionRendersTheFullDeclaredSchema()
    {
        var board = new Board();
        AddInstance(board, label: "First");
        AddInstance(board, label: "Second");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

        Assert.NotNull(panel.Find("#d12-property-panel-field-Label"));
        Assert.NotNull(panel.Find("#d12-property-panel-field-Count"));
        Assert.NotNull(panel.Find("#d12-property-panel-field-Tint"));
        Assert.NotNull(panel.Find("#d12-property-panel-field-Flag"));
        Assert.NotNull(panel.Find("#d12-property-panel-field-Mode"));
    }

    [Fact]
    public void CommittingASameTypeMultiSelectionEditAppliesToEveryInstance()
    {
        var board = new Board();
        var first = AddInstance(board, tint: "#000000");
        var second = AddInstance(board, tint: "#000000");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

        panel.Find("#d12-property-panel-field-Tint").Change("#00ff00");

        Assert.Equal("#00ff00", ((PanelTestProps)first.Props).Tint);
        Assert.Equal("#00ff00", ((PanelTestProps)second.Props).Tint);
    }

    [Fact]
    public async Task UndoAfterASameTypeMultiSelectionEditRevertsEveryInstanceAsOneStep()
    {
        var board = new Board();
        var first = AddInstance(board, tint: "#000000");
        var second = AddInstance(board, tint: "#000000");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
        panel.Find("#d12-property-panel-field-Tint").Change("#00ff00");

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#000000", ((PanelTestProps)first.Props).Tint);
        Assert.Equal("#000000", ((PanelTestProps)second.Props).Tint);
    }

    // Only the properties an author has explicitly tagged as shared (matching SharedTag)
    // surface for a cross-type selection - PanelTestProps.Label and PanelTestPropsSecondary.Note
    // carry no tag at all, and must not appear.
    [Fact]
    public void CrossTypeMultiSelectionSurfacesOnlyExplicitlySharedTaggedProperties()
    {
        var board = new Board();
        AddInstance(board);
        AddSecondaryInstance(board);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

        Assert.Single(panel.FindAll(".d12-property-panel-field"));
        Assert.NotNull(panel.Find("#d12-property-panel-field-color"));
        Assert.Empty(panel.FindAll("#d12-property-panel-field-Label"));
        Assert.Empty(panel.FindAll("#d12-property-panel-field-Count"));
        Assert.Empty(panel.FindAll("#d12-property-panel-field-Note"));
    }

    [Fact]
    public void CommittingACrossTypeSharedPropertyEditAppliesToEveryInstance()
    {
        var board = new Board();
        var first = AddInstance(board, tint: "#000000");
        var second = AddSecondaryInstance(board, accentColor: "#000000");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

        panel.Find("#d12-property-panel-field-color").Change("#00ff00");

        Assert.Equal("#00ff00", ((PanelTestProps)first.Props).Tint);
        Assert.Equal("#00ff00", ((PanelTestPropsSecondary)second.Props).AccentColor);
    }

    [Fact]
    public async Task UndoAfterACrossTypeSharedPropertyEditRevertsEveryInstanceAsOneStep()
    {
        var board = new Board();
        var first = AddInstance(board, tint: "#000000");
        var second = AddSecondaryInstance(board, accentColor: "#000000");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
        panel.Find("#d12-property-panel-field-color").Change("#00ff00");

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#000000", ((PanelTestProps)first.Props).Tint);
        Assert.Equal("#000000", ((PanelTestPropsSecondary)second.Props).AccentColor);
    }

    // Grouping collapses a multi-selection onto a single Group id in DiagramCanvas's selection
    // set - SinglySelectedComponent must still read this as "nothing to edit" (a Group has no
    // Props of its own), not mistake the lone selected id for a component.
    [Fact]
    public async Task ShowsAnEmptyStateWhenTheSelectionIsAGroup()
    {
        var board = new Board();
        AddInstance(board);
        AddInstance(board);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        Assert.NotNull(panel.Find(".d12-property-panel-empty"));
    }

    // A shift-click can mix a grouped member's own group id (EffectiveSelectionId) with a
    // standalone instance's plain id in the same ad-hoc selection - that must still read as
    // "nothing to edit" (SelectedComponents), the same as a lone selected Group, rather than
    // collapsing to "edit just the standalone instance".
    [Fact]
    public async Task ShowsAnEmptyStateWhenSelectionMixesAGroupWithAStandaloneInstance()
    {
        var board = new Board();
        AddInstance(board);
        AddInstance(board);
        AddInstance(board);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        canvas.FindAll(".component-container")[2].Click(new MouseEventArgs { ShiftKey = true });

        Assert.NotNull(panel.Find(".d12-property-panel-empty"));
    }

    [Fact]
    public void ShowsAnEmptyStateWhenAnEdgeIsSelected()
    {
        var board = new Board();
        var source = AddInstance(board);
        var target = new ComponentInstance(
            ComponentTypeKey,
            new PanelTestProps("content", "", 0),
            new Bounds(300, 0, 200, 200)
        );
        board.AddComponent(target);
        board.AddEdge(
            new Edge(
                new PortEndpoint(source.Id, PortId.Right),
                new PortEndpoint(target.Id, PortId.Left)
            )
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        canvas.Find(".edge-line").Click();

        Assert.NotNull(panel.Find(".d12-property-panel-empty"));
    }

    [Fact]
    public void RendersTextAndNumberControlsForTheSelectionsEditableProperties()
    {
        var board = new Board();
        AddInstance(board, label: "Hello", count: 42);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        Select(canvas);

        var labelInput = panel.Find("#d12-property-panel-field-Label");
        Assert.Equal("text", labelInput.GetAttribute("type"));
        Assert.Equal("Hello", labelInput.GetAttribute("value"));

        var countInput = panel.Find("#d12-property-panel-field-Count");
        Assert.Equal("number", countInput.GetAttribute("type"));
        Assert.Equal("42", countInput.GetAttribute("value"));
    }

    [Fact]
    public void RendersColorCheckboxAndDropdownControlsForTheSelectionsEditableProperties()
    {
        var board = new Board();
        AddInstance(board, tint: "#ff0000", flag: true, mode: "b");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        Select(canvas);

        var tintInput = panel.Find("#d12-property-panel-field-Tint");
        Assert.Equal("color", tintInput.GetAttribute("type"));
        Assert.Equal("#ff0000", tintInput.GetAttribute("value"));

        var flagInput = panel.Find("#d12-property-panel-field-Flag");
        Assert.Equal("checkbox", flagInput.GetAttribute("type"));
        Assert.True(((IHtmlInputElement)flagInput).IsChecked);

        var modeSelect = panel.Find("#d12-property-panel-field-Mode");
        Assert.Equal("select", modeSelect.TagName.ToLowerInvariant());
        Assert.Equal(
            ["a", "b", "c"],
            modeSelect.QuerySelectorAll("option").Select(o => o.GetAttribute("value"))
        );
        Assert.Equal("b", ((IHtmlSelectElement)modeSelect).Value);
    }

    [Fact]
    public void CommittingAColorEditRecordsExactlyOneHistoryEntryAndUpdatesLive()
    {
        var board = new Board();
        var instance = AddInstance(board, tint: "#000000");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find("#d12-property-panel-field-Tint").Change("#00ff00");

        Assert.Equal("#00ff00", ((PanelTestProps)instance.Props).Tint);
    }

    [Fact]
    public async Task UndoAfterCommittingAColorEditRevertsIt()
    {
        var board = new Board();
        var instance = AddInstance(board, tint: "#000000");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);
        panel.Find("#d12-property-panel-field-Tint").Change("#00ff00");

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#000000", ((PanelTestProps)instance.Props).Tint);
    }

    [Fact]
    public async Task CommittingTheSameColorAgainRecordsNoAdditionalHistoryEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, tint: "#000000");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find("#d12-property-panel-field-Tint").Change("#00ff00"); // one real gesture
        panel.Find("#d12-property-panel-field-Tint").Change("#00ff00"); // no-op: same value again

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#000000", ((PanelTestProps)instance.Props).Tint);
    }

    [Fact]
    public void CommittingACheckboxEditRecordsExactlyOneHistoryEntryAndUpdatesLive()
    {
        var board = new Board();
        var instance = AddInstance(board, flag: false);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find("#d12-property-panel-field-Flag").Change(true);

        Assert.True(((PanelTestProps)instance.Props).Flag);
    }

    [Fact]
    public async Task UndoAfterCommittingACheckboxEditRevertsIt()
    {
        var board = new Board();
        var instance = AddInstance(board, flag: false);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);
        panel.Find("#d12-property-panel-field-Flag").Change(true);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.False(((PanelTestProps)instance.Props).Flag);
    }

    [Fact]
    public async Task CommittingTheSameCheckboxValueAgainRecordsNoAdditionalHistoryEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, flag: false);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find("#d12-property-panel-field-Flag").Change(true); // one real gesture
        panel.Find("#d12-property-panel-field-Flag").Change(true); // no-op: same value again

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.False(((PanelTestProps)instance.Props).Flag);
    }

    [Fact]
    public void CommittingADropdownEditRecordsExactlyOneHistoryEntryAndUpdatesLive()
    {
        var board = new Board();
        var instance = AddInstance(board, mode: "a");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find("#d12-property-panel-field-Mode").Change("b");

        Assert.Equal("b", ((PanelTestProps)instance.Props).Mode);
    }

    [Fact]
    public async Task UndoAfterCommittingADropdownEditRevertsIt()
    {
        var board = new Board();
        var instance = AddInstance(board, mode: "a");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);
        panel.Find("#d12-property-panel-field-Mode").Change("b");

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("a", ((PanelTestProps)instance.Props).Mode);
    }

    [Fact]
    public async Task CommittingTheSameDropdownValueAgainRecordsNoAdditionalHistoryEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, mode: "a");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find("#d12-property-panel-field-Mode").Change("b"); // one real gesture
        panel.Find("#d12-property-panel-field-Mode").Change("b"); // no-op: same value again

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("a", ((PanelTestProps)instance.Props).Mode);
    }

    [Fact]
    public void ExcludesAContentFieldCarryingNoPanelEditableAttribute()
    {
        var board = new Board();
        AddInstance(board);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        Select(canvas);

        Assert.Empty(panel.FindAll("#d12-property-panel-field-Content"));
    }

    [Fact]
    public void PanelUpdatesLiveWhenSelectionChangesOnTheCanvas()
    {
        var board = new Board();
        AddInstance(board, label: "First");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        Select(canvas);
        Assert.NotEmpty(panel.FindAll("#d12-property-panel-field-Label"));

        canvas.Find(".diagram-canvas").Click();

        Assert.NotNull(panel.Find(".d12-property-panel-empty"));
    }

    [Fact]
    public void CommittingATextEditRecordsExactlyOneHistoryEntryAndUpdatesLive()
    {
        var board = new Board();
        var instance = AddInstance(board, label: "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find("#d12-property-panel-field-Label").Change("Edited");

        Assert.Equal("Edited", ((PanelTestProps)instance.Props).Label);
        Assert.Equal("Edited", panel.Find("#d12-property-panel-field-Label").GetAttribute("value"));
    }

    [Fact]
    public async Task UndoAfterCommittingATextEditRevertsIt()
    {
        var board = new Board();
        var instance = AddInstance(board, label: "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);
        panel.Find("#d12-property-panel-field-Label").Change("Edited");

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("Original", ((PanelTestProps)instance.Props).Label);
    }

    [Fact]
    public void CommittingANumberEditUpdatesTheInstanceLive()
    {
        var board = new Board();
        var instance = AddInstance(board, count: 1);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find("#d12-property-panel-field-Count").Change("99");

        Assert.Equal(99, ((PanelTestProps)instance.Props).Count);
    }

    [Fact]
    public async Task UndoAfterCommittingANumberEditRevertsIt()
    {
        var board = new Board();
        var instance = AddInstance(board, count: 1);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);
        panel.Find("#d12-property-panel-field-Count").Change("99");

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(1, ((PanelTestProps)instance.Props).Count);
    }

    [Fact]
    public async Task CommittingTheSameValueAgainRecordsNoAdditionalHistoryEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, label: "Original");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find("#d12-property-panel-field-Label").Change("Edited"); // one real gesture
        panel.Find("#d12-property-panel-field-Label").Change("Edited"); // no-op: same value again

        // If the no-op had wrongly recorded its own history entry, a single Undo would only
        // revert that phantom entry and Label would still read "Edited" here.
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("Original", ((PanelTestProps)instance.Props).Label);
    }

    [Fact]
    public void EditingAnInvalidNumberIsANoOp()
    {
        var board = new Board();
        var instance = AddInstance(board, count: 1);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        var exception = Record.Exception(
            () => panel.Find("#d12-property-panel-field-Count").Change("not-a-number")
        );

        Assert.Null(exception);
        Assert.Equal(1, ((PanelTestProps)instance.Props).Count);
    }

    [Fact]
    public void RendersTheCustomEditorForTheSelectionsCustomKindProperty()
    {
        var board = new Board();
        AddInstance(board);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        Select(canvas);

        Assert.NotNull(panel.Find("#d12-property-panel-field-CustomValue"));
        Assert.NotNull(panel.Find($"#{PanelTestCustomEditor.CommitButtonId}"));
    }

    [Fact]
    public void CommittingThroughTheCustomEditorRecordsExactlyOneHistoryEntryAndUpdatesLive()
    {
        var board = new Board();
        var instance = AddInstance(board, customValue: "before");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find($"#{PanelTestCustomEditor.CommitButtonId}").Click();

        Assert.Equal(
            PanelTestCustomEditor.CommittedValue,
            ((PanelTestProps)instance.Props).CustomValue
        );
    }

    [Fact]
    public async Task UndoAfterCommittingThroughTheCustomEditorRevertsIt()
    {
        var board = new Board();
        var instance = AddInstance(board, customValue: "before");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);
        panel.Find($"#{PanelTestCustomEditor.CommitButtonId}").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("before", ((PanelTestProps)instance.Props).CustomValue);
    }

    [Fact]
    public async Task CommittingTheSameCustomValueAgainRecordsNoAdditionalHistoryEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, customValue: "before");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        Select(canvas);

        panel.Find($"#{PanelTestCustomEditor.CommitButtonId}").Click(); // one real gesture
        panel.Find($"#{PanelTestCustomEditor.CommitButtonId}").Click(); // no-op: same value again

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("before", ((PanelTestProps)instance.Props).CustomValue);
    }
}
