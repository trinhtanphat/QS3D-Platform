namespace QS3D.Platform.Application;

[Flags]
public enum DirtyReason
{
    None = 0,
    DirectMutation = 1 << 0,
    DependencyChanged = 1 << 1,
    SourceGeometryChanged = 1 << 2,
    RuleChanged = 1 << 3,
    ManualInvalidation = 1 << 4
}

public sealed record DirtyStateSnapshot(
    string NodeId,
    bool IsDirty,
    long DirtyRevision,
    long CleanRevision,
    DirtyReason Reasons);

public sealed class DirtyStateTracker
{
    private readonly Dictionary<string, State> _states = new(StringComparer.Ordinal);
    private long _revision;

    public long Revision => _revision;

    public DirtyStateSnapshot Get(string nodeId)
    {
        var id = Normalize(nodeId, nameof(nodeId));
        if (!_states.TryGetValue(id, out var state))
            return new DirtyStateSnapshot(id, false, 0, 0, DirtyReason.None);
        return state.Snapshot(id);
    }

    public DirtyStateSnapshot MarkDirty(string nodeId, DirtyReason reason)
    {
        var id = Normalize(nodeId, nameof(nodeId));
        if (reason == DirtyReason.None) throw new ArgumentOutOfRangeException(nameof(reason), "Dirty reason must not be None.");
        if (!_states.TryGetValue(id, out var state))
        {
            state = new State();
            _states.Add(id, state);
        }

        _revision = checked(_revision + 1);
        state.IsDirty = true;
        state.DirtyRevision = _revision;
        state.Reasons |= reason;
        return state.Snapshot(id);
    }

    public DirtyStateSnapshot MarkClean(string nodeId, long expectedDirtyRevision)
    {
        var id = Normalize(nodeId, nameof(nodeId));
        if (!_states.TryGetValue(id, out var state) || !state.IsDirty)
            throw new InvalidOperationException($"Node '{id}' is not dirty.");
        if (state.DirtyRevision != expectedDirtyRevision)
        {
            throw new InvalidOperationException(
                $"Node '{id}' changed after regeneration began. Expected dirty revision {expectedDirtyRevision} but current dirty revision is {state.DirtyRevision}.");
        }

        _revision = checked(_revision + 1);
        state.IsDirty = false;
        state.CleanRevision = _revision;
        state.Reasons = DirtyReason.None;
        return state.Snapshot(id);
    }

    public IReadOnlyList<DirtyStateSnapshot> GetDirty()
        => _states
            .Where(static pair => pair.Value.IsDirty)
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .Select(static pair => pair.Value.Snapshot(pair.Key))
            .ToArray();

    public IReadOnlyList<DirtyStateSnapshot> MarkImpact(
        DependencyGraph graph,
        IEnumerable<string> changedNodeIds,
        DirtyReason rootReason = DirtyReason.DirectMutation)
    {
        if (graph is null) throw new ArgumentNullException(nameof(graph));
        if (changedNodeIds is null) throw new ArgumentNullException(nameof(changedNodeIds));
        if (rootReason == DirtyReason.None) throw new ArgumentOutOfRangeException(nameof(rootReason));

        var roots = new HashSet<string>(changedNodeIds.Select(static id => Normalize(id, nameof(changedNodeIds))), StringComparer.Ordinal);
        if (roots.Count == 0) return Array.Empty<DirtyStateSnapshot>();
        var plan = graph.PlanImpact(roots);
        var result = new List<DirtyStateSnapshot>(plan.OrderedNodeIds.Count);
        foreach (var nodeId in plan.OrderedNodeIds)
        {
            result.Add(MarkDirty(
                nodeId,
                roots.Contains(nodeId) ? rootReason : DirtyReason.DependencyChanged));
        }
        return result;
    }

    private static string Normalize(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Node ID must not be blank.", parameterName);
        return value.Trim();
    }

    private sealed class State
    {
        public bool IsDirty { get; set; }
        public long DirtyRevision { get; set; }
        public long CleanRevision { get; set; }
        public DirtyReason Reasons { get; set; }

        public DirtyStateSnapshot Snapshot(string nodeId)
            => new(nodeId, IsDirty, DirtyRevision, CleanRevision, Reasons);
    }
}
