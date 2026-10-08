using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;

namespace D12Canvas.Tests;

// The canvas-side seam a pointer gesture sees, replaced by plain state so a gesture can be driven
// with presses, moves and a release and asked what it published and what it committed.
internal sealed class FakeGestureContext : IGestureContext
{
    public FakeGestureContext(Board board, ZoomPanTracker? zoomPan = null)
    {
        Board = board;
        ZoomPan = zoomPan ?? new ZoomPanTracker();
        ZoomPan.SetContainerSize(800, 600);
        SelectionSnapshot = new SelectionSnapshot([], []);
    }

    public Board? Board { get; }
    public ZoomPanTracker ZoomPan { get; }
    public SelectionSnapshot SelectionSnapshot { get; set; }

    public HashSet<Guid> SelectedInstanceIds { get; } = new();
    public HashSet<Guid> SelectedEdgeIds { get; } = new();
    public Bounds? Marquee { get; private set; }
    public List<(double X, double Y, bool PressHitEntity)> ContextMenuOpenings { get; } = new();
    public Func<Guid, Guid> EffectiveId { get; set; } = id => id;
    public double? GridSpacing { get; set; }
    public bool ObjectSnapping { get; set; }
    public IReadOnlyList<SnapGuide> Guides { get; private set; } = [];

    public IReadOnlyList<Bounds> SnapCandidates(IReadOnlyCollection<Guid> excluded) =>
        Board!
            .GetVisible(ZoomPan.Viewport)
            .Where(instance => !excluded.Contains(instance.Id))
            .Select(instance => instance.Bounds)
            .ToList();

    public void PublishGuides(IReadOnlyList<SnapGuide> guides) => Guides = guides;

    public IReadOnlyDictionary<Guid, Bounds> Preview { get; private set; } =
        new Dictionary<Guid, Bounds>();
    public List<IReadOnlyDictionary<Guid, Bounds>> Commits { get; } = new();
    public List<Guid> InlineEditRequests { get; } = new();

    public (double X, double Y) ToBoardPoint(double containerX, double containerY) =>
        ((containerX - ZoomPan.PanX) / ZoomPan.Scale, (containerY - ZoomPan.PanY) / ZoomPan.Scale);

    public Guid EffectiveSelectionId(Guid entityId) => EffectiveId(entityId);

    public Func<Guid, bool> InScope { get; set; } = _ => true;
    public List<Guid> EnteredGroups { get; } = new();

    public bool IsInScope(Guid entityId) => InScope(entityId);

    public HashSet<Guid> WithoutHitRegion { get; } = new();

    public bool HasHitRegion(Guid entityId) =>
        !WithoutHitRegion.Contains(entityId) && !Board!.IsLocked(entityId);

    public void EnterGroup(Guid groupId) => EnteredGroups.Add(groupId);

    public bool HasEnteredGroup { get; set; }
    public List<PointerPress> StepOutPresses { get; } = new();

    public void StepOutFor(PointerPress press) => StepOutPresses.Add(press);

    public IReadOnlyList<HitStackEntry> HitStackOf(PointerPress press) =>
        (press.Hits ?? [])
            .Where(hit =>
                hit.EntityId is not null
                && hit.Role is not (HitRole.SelectionBounds or HitRole.SelectionHandle)
            )
            .Select(hit =>
                Board!.GetEdge(hit.EntityId!.Value) is not null
                    ? new HitStackEntry(hit.EntityId.Value, IsEdge: true)
                    : new HitStackEntry(EffectiveId(hit.EntityId.Value), IsEdge: false)
            )
            .Distinct()
            .ToList();

    public void SelectHitStackEntry(HitStackEntry entry) =>
        ReplaceSelection(entry.IsEdge ? [] : [entry.Id], entry.IsEdge ? [entry.Id] : []);

    public void ToggleHitStackEntry(HitStackEntry entry)
    {
        var toggled = entry.IsEdge ? SelectedEdgeIds : SelectedInstanceIds;
        if (!toggled.Remove(entry.Id))
        {
            toggled.Add(entry.Id);
        }
    }

    public bool IsSelected(Guid effectiveId) => SelectedInstanceIds.Contains(effectiveId);

    public IReadOnlyList<ComponentInstance> SelectedInstances() =>
        SelectedInstanceIds
            .SelectMany(ExpandedIds)
            .Distinct()
            .Select(id => Board!.GetComponent(id))
            .OfType<ComponentInstance>()
            .ToList();

    private IEnumerable<Guid> ExpandedIds(Guid id) =>
        Board!.GetGroup(id) is { } group ? group.MemberIds.SelectMany(ExpandedIds) : [id];

    public bool IsEdgeSelected(Guid edgeId) => SelectedEdgeIds.Contains(edgeId);

    public IReadOnlyList<Edge> SelectedEdges() =>
        SelectedEdgeIds.Select(id => Board!.GetEdge(id)).OfType<Edge>().ToList();

    public void ReplaceSelection(IEnumerable<Guid> effectiveIds, IEnumerable<Guid> edgeIds)
    {
        var instances = effectiveIds.ToList();
        var edges = edgeIds.ToList();
        SelectedInstanceIds.Clear();
        SelectedInstanceIds.UnionWith(instances);
        SelectedEdgeIds.Clear();
        SelectedEdgeIds.UnionWith(edges);
    }

    public void AddToSelection(Guid effectiveId) => SelectedInstanceIds.Add(effectiveId);

    public void RemoveFromSelection(Guid effectiveId) => SelectedInstanceIds.Remove(effectiveId);

    public void SelectEdge(Guid edgeId) => ReplaceSelection([], [edgeId]);

    public void ToggleEdge(Guid edgeId)
    {
        if (!SelectedEdgeIds.Remove(edgeId))
        {
            SelectedEdgeIds.Add(edgeId);
        }
    }

    public void ClearSelection() => ReplaceSelection([], []);

    public void ShowMarquee(Bounds? boardBounds) => Marquee = boardBounds;

    public void PublishPreview(IReadOnlyDictionary<Guid, Bounds> boundsOverrides) =>
        Preview = boundsOverrides;

    public IReadOnlyDictionary<EdgeEnd, FloatingEndpoint> MovedEndpoints { get; private set; } =
        new Dictionary<EdgeEnd, FloatingEndpoint>();

    public void PublishMovedEndpoints(
        IReadOnlyDictionary<EdgeEnd, FloatingEndpoint> movedEndpoints
    ) => MovedEndpoints = movedEndpoints;

    public void CommitPreview()
    {
        Commits.Add(Preview);
        CommittedFragments.Add(PendingFragment);
    }

    public List<Board?> CommittedFragments { get; } = new();

    public Board? PendingFragment { get; private set; }

    public int CopiesBuilt { get; private set; }

    public Board? CopyOfSelection()
    {
        var fragment = BoardFragment.Of(Board!, SelectedInstanceIds, SelectedEdgeIds, Registry);
        if (fragment.Components.Count == 0 && fragment.Edges.Count == 0)
        {
            return null;
        }

        CopiesBuilt++;
        var copies = BoardFragment.WithFreshIds(fragment);
        BoardFragment.StackAbove(Board!, copies);
        return copies;
    }

    public void PublishPendingFragment(Board? pendingFragment) => PendingFragment = pendingFragment;

    public Bounds? SelectionFrame { get; private set; }

    public void PublishSelectionFrame(Bounds frame) => SelectionFrame = frame;

    private static readonly IComponentRegistry Registry = BuildRegistry();

    private static IComponentRegistry BuildRegistry()
    {
        var options = new D12CanvasOptions();
        options.RegisterComponent<TestComponentDouble, TestProps>(
            "test-props",
            builder =>
            {
                builder.DisplayName = "Test Props";
                builder.AccessibleName = "Test props component";
                builder.DefaultProps = new TestProps();
            }
        );
        return options.Registry;
    }

    public PendingEdge? PendingEdge { get; private set; }
    public List<(IEdgeEndpoint Source, IEdgeEndpoint Target)> AddedEdges { get; } = new();
    public List<(Guid EdgeId, bool IsSource, IEdgeEndpoint Endpoint)> EndpointChanges { get; } =
        new();
    public List<Guid> LabelsAdded { get; } = new();
    public List<Guid> LabelEditRequests { get; } = new();

    public void PublishPendingEdge(PendingEdge pendingEdge) => PendingEdge = pendingEdge;

    public void AddEdge(IEdgeEndpoint source, IEdgeEndpoint target) =>
        AddedEdges.Add((source, target));

    public void ChangeEdgeEndpoint(Guid edgeId, bool isSource, IEdgeEndpoint endpoint) =>
        EndpointChanges.Add((edgeId, isSource, endpoint));

    public void AddEdgeLabel(Guid edgeId) => LabelsAdded.Add(edgeId);

    public void BeginLabelEdit(Guid edgeId) => LabelEditRequests.Add(edgeId);

    public void BeginInlineEdit(Guid instanceId) => InlineEditRequests.Add(instanceId);

    public void OpenContextMenuAt(double containerX, double containerY, bool pressHitEntity) =>
        ContextMenuOpenings.Add((containerX, containerY, pressHitEntity));
}
