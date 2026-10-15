using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Copy hands the browser a board envelope of the selection, cut removes exactly what it carried in
// one history entry, and a paste lands centred on the Paste anchor: the pointer over the canvas,
// the press that opened the menu, or the viewport centre, cascading while the anchor stays put.
public class DiagramCanvasClipboardTests : ComponentTestBase
{
    private const double ContainerCentreX = 400;
    private const double ContainerCentreY = 300;

    public DiagramCanvasClipboardTests()
    {
        SetupDiagramCanvasJsModule();
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        Services.AddSingleton(options.Registry);
    }

    private static ComponentInstance AddShape(Board board, double x, double y = 0)
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps(null, null, 2),
            new Bounds(x, y, 100, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static Edge Connect(Board board, ComponentInstance from, ComponentInstance to)
    {
        var edge = new Edge(
            new PortEndpoint(from.Id, PortId.Right),
            new PortEndpoint(to.Id, PortId.Left)
        );
        board.AddEdge(edge);
        return edge;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board, bool snapToGrid = false) =>
        Render<DiagramCanvas>(parameters =>
                parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, snapToGrid)
            )
            .ReturnToOrigin();

    private static Task<string?> Copy(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnCopyRequested());

    private static Task<string?> Cut(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnCutRequested());

    private static Task Paste(
        IRenderedComponent<DiagramCanvas> canvas,
        string text,
        double? pointerX = null,
        double? pointerY = null
    ) => canvas.InvokeAsync(() => canvas.Instance.OnPasteReceived(text, pointerX, pointerY));

    private static Task SelectAll(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());

    private static Task Undo(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

    private static List<ComponentInstance> AddedSince(Board board, IEnumerable<Guid> before) =>
        board.Components.Where(instance => !before.Contains(instance.Id)).ToList();

    [Fact]
    public async Task CopyWithNothingSelectedLeavesTheBrowsersOwnCopyAlone()
    {
        var board = new Board();
        AddShape(board, 0);
        var canvas = RenderCanvas(board);

        Assert.Null(await Copy(canvas));
    }

    [Fact]
    public async Task CopyWritesTheSelectionAsABoardEnvelope()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));

        var payload = await Copy(canvas);

        Assert.True(ClipboardPayload.IsBoard(payload!));
        var copied = new BoardJsonSerializer(
            Services.GetRequiredService<IComponentRegistry>()
        ).Deserialize(payload!);
        Assert.Equal(shape.Id, Assert.Single(copied.Components).Id);
    }

    [Fact]
    public async Task APasteLandsCentredOnThePointerWithNewIdsAndBecomesTheSelection()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        var payload = await Copy(canvas);

        await Paste(canvas, payload!, 300, 200);

        var pasted = Assert.Single(AddedSince(board, [shape.Id]));
        Assert.Equal(new Bounds(250, 175, 100, 50), pasted.Bounds);
        Assert.Equal("true", canvas.ContainerOf(pasted.Id).GetAttribute("aria-selected"));
        Assert.NotEqual("true", canvas.ContainerOf(shape.Id).GetAttribute("aria-selected"));
        Assert.Equal([pasted], canvas.Instance.SelectedComponents);
    }

    [Fact]
    public async Task WithThePointerOffTheCanvasAPasteLandsAtTheViewportCentre()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        var payload = await Copy(canvas);

        await Paste(canvas, payload!);

        var pasted = Assert.Single(AddedSince(board, [shape.Id]));
        Assert.Equal(
            new Bounds(ContainerCentreX - 50, ContainerCentreY - 25, 100, 50),
            pasted.Bounds
        );
    }

    [Fact]
    public async Task FivePastesAtOneSpotCascadeAndMovingThePointerResetsTheCascade()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        var payload = await Copy(canvas);

        var lefts = new List<double>();
        for (var i = 0; i < 5; i++)
        {
            var before = board.Components.Select(c => c.Id).ToList();
            await Paste(canvas, payload!, 300, 200);
            lefts.Add(Assert.Single(AddedSince(board, before)).Bounds.X);
        }

        var beforeMove = board.Components.Select(c => c.Id).ToList();
        await Paste(canvas, payload!, 500, 200);

        Assert.Equal([250, 270, 290, 310, 330], lefts);
        Assert.Equal(new Bounds(450, 175, 100, 50), AddedSince(board, beforeMove).Single().Bounds);
    }

    [Fact]
    public async Task WithSnapToGridOnThePastedTopLeftLandsOnTheGrid()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board, snapToGrid: true);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        var payload = await Copy(canvas);

        await Paste(canvas, payload!, 307, 209);

        var pasted = Assert.Single(AddedSince(board, [shape.Id]));
        Assert.Equal(new Bounds(260, 180, 100, 50), pasted.Bounds);
    }

    [Fact]
    public async Task WithSnapToGridOnAPointerNudgedWithinOneCellStillCascades()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board, snapToGrid: true);
        canvas.ClickOn(canvas.ContainerOf(shape.Id));
        var payload = await Copy(canvas);

        await Paste(canvas, payload!, 307, 209);
        var first = board.Components.Select(c => c.Id).ToList();
        await Paste(canvas, payload!, 309, 211);

        Assert.Equal(new Bounds(280, 200, 100, 50), AddedSince(board, first).Single().Bounds);
    }

    [Fact]
    public async Task PastingASavedBoardFileMergesItsContentCentredOnTheAnchor()
    {
        var saved = new Board();
        var left = AddShape(saved, 1000, 1000);
        var right = AddShape(saved, 1300, 1150);
        Connect(saved, left, right);
        var fileText = new BoardJsonSerializer(
            Services.GetRequiredService<IComponentRegistry>()
        ).Serialize(saved);
        var board = new Board();
        var existing = AddShape(board, 0);
        var canvas = RenderCanvas(board);

        await Paste(canvas, fileText, 400, 300);

        var merged = AddedSince(board, [existing.Id]).OrderBy(c => c.Bounds.X).ToList();
        Assert.Equal(new Bounds(200, 200, 100, 50), merged[0].Bounds);
        Assert.Equal(new Bounds(500, 350, 100, 50), merged[1].Bounds);
        Assert.Empty(merged.Select(c => c.Id).Intersect([left.Id, right.Id]));
        Assert.Single(board.Edges);
    }

    [Fact]
    public async Task TwoConnectedShapesPasteWithTheirEdgeAsOneRigidBody()
    {
        var board = new Board();
        var left = AddShape(board, 0);
        var right = AddShape(board, 200, 100);
        Connect(board, left, right);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(left.Id));
        canvas.ClickOn(canvas.ContainerOf(right.Id), shift: true);
        var payload = await Copy(canvas);

        await Paste(canvas, payload!, 650, 375);

        var pasted = AddedSince(board, [left.Id, right.Id]).OrderBy(c => c.Bounds.X).ToList();
        Assert.Equal(new Bounds(500, 300, 100, 50), pasted[0].Bounds);
        Assert.Equal(new Bounds(700, 400, 100, 50), pasted[1].Bounds);
        var pastedEdge = Assert.Single(
            board.Edges,
            edge => edge.Source.ComponentId == pasted[0].Id
        );
        Assert.Equal(pasted[1].Id, pastedEdge.Target.ComponentId);
        Assert.Equal([pastedEdge], canvas.Instance.SelectedEdges);
    }

    [Fact]
    public async Task CutRemovesTheSelectionAndItsCarriedEdgeInOneEntryAndThePasteRestoresIt()
    {
        var board = new Board();
        var left = AddShape(board, 0);
        var right = AddShape(board, 200);
        Connect(board, left, right);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.ContainerOf(left.Id));
        canvas.ClickOn(canvas.ContainerOf(right.Id), shift: true);

        var payload = await Cut(canvas);

        Assert.Empty(board.Components);
        Assert.Empty(board.Edges);
        Assert.Empty(canvas.Instance.SelectedComponents);

        await Paste(canvas, payload!, 500, 400);

        Assert.Equal(2, board.Components.Count);
        Assert.Single(board.Edges);

        await Undo(canvas);
        await Undo(canvas);

        Assert.Equal(
            new[] { left.Id, right.Id }.ToHashSet(),
            board.Components.Select(c => c.Id).ToHashSet()
        );
        Assert.Single(board.Edges);
    }

    [Fact]
    public async Task ALoneSelectedEdgeCutsAndPastesBackAsAFloatingLine()
    {
        var board = new Board();
        var left = AddShape(board, 0);
        var right = AddShape(board, 200);
        var edge = Connect(board, left, right);
        var canvas = RenderCanvas(board);
        await canvas.Press(150, 25, role: HitRole.Edge, entityId: edge.Id);
        await canvas.Release(150, 25);

        var payload = await Cut(canvas);

        Assert.Null(board.GetEdge(edge.Id));
        Assert.Equal(2, board.Components.Count);

        await Paste(canvas, payload!, 150, 300);

        var pasted = Assert.Single(board.Edges);
        Assert.Equal(new FloatingEndpoint(100, 300), pasted.Source);
        Assert.Equal(new FloatingEndpoint(200, 300), pasted.Target);
        Assert.Equal([pasted], canvas.Instance.SelectedEdges);
    }

    [Fact]
    public async Task UndoingAPasteRemovesEverythingItAdded()
    {
        var board = new Board();
        var left = AddShape(board, 0);
        var right = AddShape(board, 200);
        Connect(board, left, right);
        var canvas = RenderCanvas(board);
        await SelectAll(canvas);
        var payload = await Copy(canvas);
        await Paste(canvas, payload!, 300, 300);

        await Undo(canvas);

        Assert.Equal(2, board.Components.Count);
        Assert.Single(board.Edges);
    }

    [Fact]
    public async Task PlainTextPastesAsATextShapeCentredOnTheAnchor()
    {
        var board = new Board();
        var canvas = RenderCanvas(board);

        await Paste(canvas, "Remember the milk", 300, 200);

        var text = Assert.Single(board.Components);
        Assert.Equal("text", text.ComponentTypeKey);
        Assert.Equal("Remember the milk", ((TextProps)text.Props).Text);
        Assert.Equal(300, text.Bounds.X + text.Bounds.Width / 2);
        Assert.Equal(200, text.Bounds.Y + text.Bounds.Height / 2);
    }

    [Fact]
    public async Task APasteNamingAnUnknownTypeRaisesTheWarningsEventWithTheTypeKey()
    {
        var board = new Board();
        var canvas = RenderCanvas(board);
        IReadOnlyList<BoardDeserializeWarning>? raised = null;
        canvas.Instance.PasteWarnings += (_, args) => raised = args.Warnings;
        var json = $$"""
            {
              "SchemaVersion": 1,
              "Components": [
                { "Id": "{{Guid.NewGuid()}}", "ComponentTypeKey": "rectangle", "Props": { "FillColor": null, "StrokeColor": null, "StrokeWidth": 2 }, "Bounds": { "X": 0, "Y": 0, "Width": 100, "Height": 50 }, "ZIndex": 0 },
                { "Id": "{{Guid.NewGuid()}}", "ComponentTypeKey": "mystery-widget", "Props": {}, "Bounds": { "X": 0, "Y": 0, "Width": 10, "Height": 10 }, "ZIndex": 0 }
              ]
            }
            """;

        await Paste(canvas, json, 300, 200);

        Assert.Single(board.Components);
        Assert.Contains(raised!, warning => warning.Reason.Contains("'mystery-widget'"));
    }

    private static async Task OpenObjectMenu(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance shape
    )
    {
        await canvas.Press(
            10,
            10,
            PointerPress.SecondaryButton,
            role: HitRole.Instance,
            entityId: shape.Id
        );
        await canvas.Release(10, 10, PointerPress.SecondaryButton);
    }

    private static async Task OpenCanvasMenu(
        IRenderedComponent<DiagramCanvas> canvas,
        double x,
        double y
    )
    {
        await canvas.Press(x, y, PointerPress.SecondaryButton);
        await canvas.Release(x, y, PointerPress.SecondaryButton);
    }

    private static string[] MenuLabels(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-context-menu-label").Select(label => label.TextContent).ToArray();

    private static Task ChooseRow(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas
            .FindAll(".d12-context-menu-item")
            .Single(item => item.QuerySelector(".d12-context-menu-label")!.TextContent == label)
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

    [Fact]
    public async Task OutsideASecureContextTheMenuHasNoClipboardRows()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);

        await OpenObjectMenu(canvas, shape);

        Assert.DoesNotContain("Copy", MenuLabels(canvas));
        Assert.DoesNotContain("Cut", MenuLabels(canvas));
        Assert.DoesNotContain("Paste", MenuLabels(canvas));
    }

    [Fact]
    public async Task TheObjectMenuLeadsWithCutCopyPasteAndDuplicate()
    {
        ReportPlatform(applePlatform: false, asyncClipboard: true);
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);

        await OpenObjectMenu(canvas, shape);

        Assert.Equal(["Cut", "Copy", "Paste", "Duplicate", "Delete"], MenuLabels(canvas).Take(5));
    }

    [Fact]
    public async Task TheCanvasMenuLeadsWithPasteAndOffersNoCutOrCopy()
    {
        ReportPlatform(applePlatform: false, asyncClipboard: true);
        var canvas = RenderCanvas(new Board());

        await OpenCanvasMenu(canvas, 300, 200);

        Assert.Equal("Paste", MenuLabels(canvas)[0]);
        Assert.DoesNotContain("Copy", MenuLabels(canvas));
        Assert.DoesNotContain("Cut", MenuLabels(canvas));
    }

    [Fact]
    public async Task TheCopyRowWritesTheSelectionThroughTheAsyncClipboard()
    {
        ReportPlatform(applePlatform: false, asyncClipboard: true);
        var write = CanvasModule.Setup<bool>("writeClipboardText", _ => true);
        write.SetResult(true);
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        await OpenObjectMenu(canvas, shape);

        await ChooseRow(canvas, "Copy");

        var written = (string)Assert.Single(write.Invocations).Arguments[0]!;
        Assert.True(ClipboardPayload.IsBoard(written));
        Assert.Single(board.Components);
    }

    [Fact]
    public async Task TheCutRowRemovesNothingWhenTheClipboardRefusedTheWrite()
    {
        ReportPlatform(applePlatform: false, asyncClipboard: true);
        CanvasModule.Setup<bool>("writeClipboardText", _ => true).SetResult(false);
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        await OpenObjectMenu(canvas, shape);

        await ChooseRow(canvas, "Cut");

        Assert.Single(board.Components);
    }

    [Fact]
    public async Task TheCutRowRemovesTheSelectionOnceTheWriteSucceeded()
    {
        ReportPlatform(applePlatform: false, asyncClipboard: true);
        CanvasModule.Setup<bool>("writeClipboardText", _ => true).SetResult(true);
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        await OpenObjectMenu(canvas, shape);

        await ChooseRow(canvas, "Cut");

        Assert.Empty(board.Components);
    }

    [Fact]
    public async Task AMenuPasteLandsAtThePressThatOpenedTheMenu()
    {
        ReportPlatform(applePlatform: false, asyncClipboard: true);
        CanvasModule.Setup<string?>("readClipboardText", _ => true).SetResult("Pinned note");
        var board = new Board();
        var canvas = RenderCanvas(board);
        await OpenCanvasMenu(canvas, 520, 410);

        await ChooseRow(canvas, "Paste");

        var text = Assert.Single(board.Components);
        Assert.Equal(520, text.Bounds.X + text.Bounds.Width / 2);
        Assert.Equal(410, text.Bounds.Y + text.Bounds.Height / 2);
    }

    [Fact]
    public async Task AKeyboardOpenedMenuPastesAtTheViewportCentre()
    {
        ReportPlatform(applePlatform: false, asyncClipboard: true);
        CanvasModule.Setup<string?>("readClipboardText", _ => true).SetResult("Pinned note");
        var board = new Board();
        var canvas = RenderCanvas(board);
        await OpenCanvasMenu(canvas, 520, 410);
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnContextMenuKeyPressed());

        await ChooseRow(canvas, "Paste");

        var text = Assert.Single(board.Components);
        Assert.Equal(ContainerCentreX, text.Bounds.X + text.Bounds.Width / 2);
        Assert.Equal(ContainerCentreY, text.Bounds.Y + text.Bounds.Height / 2);
    }

    [Fact]
    public async Task APastedColourThatCouldEscapeItsDeclarationNeverReachesTheStyle()
    {
        var board = new Board();
        var canvas = RenderCanvas(board);
        var json = $$"""
            {
              "SchemaVersion": 1,
              "Components": [
                { "Id": "{{Guid.NewGuid()}}", "ComponentTypeKey": "rectangle", "Props": { "FillColor": "red; background-image: url(https://example.invalid/x)", "StrokeColor": null, "StrokeWidth": 2 }, "Bounds": { "X": 0, "Y": 0, "Width": 100, "Height": 50 }, "ZIndex": 0 }
              ]
            }
            """;

        await Paste(canvas, json, 300, 200);

        Assert.DoesNotContain(
            "example.invalid",
            canvas.Find(".d12-rectangle").GetAttribute("style")
        );
    }

    [Fact]
    public async Task ACleanPasteRaisesNoWarnings()
    {
        var board = new Board();
        var canvas = RenderCanvas(board);
        var raised = false;
        canvas.Instance.PasteWarnings += (_, _) => raised = true;

        await Paste(canvas, "plain", 300, 200);

        Assert.False(raised);
    }
}
