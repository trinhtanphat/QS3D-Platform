namespace QS3D.Platform.Application;

public sealed class DependencyPlan
{
    public DependencyPlan(IEnumerable<string> orderedNodeIds)
    {
        if (orderedNodeIds is null) throw new ArgumentNullException(nameof(orderedNodeIds));
        OrderedNodeIds = orderedNodeIds.ToArray();
    }

    public IReadOnlyList<string> OrderedNodeIds { get; }
}

public sealed class DependencyGraph
{
    private readonly HashSet<string> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _dependenciesByNode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _dependentsByNode = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Nodes => _nodes.OrderBy(static x => x, StringComparer.Ordinal).ToArray();

    public void AddNode(string nodeId)
    {
        var id = Normalize(nodeId, nameof(nodeId));
        if (!_nodes.Add(id)) return;
        _dependenciesByNode.Add(id, new HashSet<string>(StringComparer.Ordinal));
        _dependentsByNode.Add(id, new HashSet<string>(StringComparer.Ordinal));
    }

    public void AddDependency(string nodeId, string dependencyNodeId)
    {
        var node = Normalize(nodeId, nameof(nodeId));
        var dependency = Normalize(dependencyNodeId, nameof(dependencyNodeId));
        if (StringComparer.Ordinal.Equals(node, dependency))
            throw new InvalidOperationException($"Node '{node}' cannot depend on itself.");
        AddNode(node);
        AddNode(dependency);
        if (_dependenciesByNode[node].Add(dependency))
            _dependentsByNode[dependency].Add(node);
    }

    public bool RemoveDependency(string nodeId, string dependencyNodeId)
    {
        var node = Normalize(nodeId, nameof(nodeId));
        var dependency = Normalize(dependencyNodeId, nameof(dependencyNodeId));
        if (!_nodes.Contains(node) || !_nodes.Contains(dependency)) return false;
        if (!_dependenciesByNode[node].Remove(dependency)) return false;
        _dependentsByNode[dependency].Remove(node);
        return true;
    }

    public IReadOnlyList<string> GetDependencies(string nodeId)
    {
        var node = RequireNode(nodeId);
        return _dependenciesByNode[node].OrderBy(static x => x, StringComparer.Ordinal).ToArray();
    }

    public IReadOnlyList<string> GetDependents(string nodeId)
    {
        var node = RequireNode(nodeId);
        return _dependentsByNode[node].OrderBy(static x => x, StringComparer.Ordinal).ToArray();
    }

    public DependencyPlan PlanImpact(IEnumerable<string> changedNodeIds)
    {
        if (changedNodeIds is null) throw new ArgumentNullException(nameof(changedNodeIds));
        var changed = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var raw in changedNodeIds)
            changed.Add(RequireNode(raw));
        if (changed.Count == 0) return new DependencyPlan(Array.Empty<string>());

        var impacted = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>(changed);
        while (queue.Count != 0)
        {
            var current = queue.Dequeue();
            if (!impacted.Add(current)) continue;
            foreach (var dependent in _dependentsByNode[current].OrderBy(static x => x, StringComparer.Ordinal))
                queue.Enqueue(dependent);
        }

        var indegree = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in impacted)
        {
            var count = 0;
            foreach (var dependency in _dependenciesByNode[node])
            {
                if (impacted.Contains(dependency)) count++;
            }
            indegree.Add(node, count);
        }

        var ready = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var pair in indegree)
        {
            if (pair.Value == 0) ready.Add(pair.Key);
        }

        var ordered = new List<string>(impacted.Count);
        while (ready.Count != 0)
        {
            var current = ready.Min!;
            ready.Remove(current);
            ordered.Add(current);
            foreach (var dependent in _dependentsByNode[current].OrderBy(static x => x, StringComparer.Ordinal))
            {
                if (!impacted.Contains(dependent)) continue;
                var next = indegree[dependent] - 1;
                indegree[dependent] = next;
                if (next == 0) ready.Add(dependent);
            }
        }

        if (ordered.Count != impacted.Count)
        {
            var cyclic = indegree.Where(static pair => pair.Value > 0)
                .Select(static pair => pair.Key)
                .OrderBy(static x => x, StringComparer.Ordinal);
            throw new InvalidOperationException($"Dependency cycle detected among: {string.Join(", ", cyclic)}.");
        }

        return new DependencyPlan(ordered);
    }

    public void ValidateAcyclic()
    {
        if (_nodes.Count == 0) return;
        PlanImpact(_nodes);
    }

    private string RequireNode(string nodeId)
    {
        var id = Normalize(nodeId, nameof(nodeId));
        if (!_nodes.Contains(id)) throw new KeyNotFoundException($"Dependency node '{id}' does not exist.");
        return id;
    }

    private static string Normalize(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Dependency node ID must not be blank.", parameterName);
        return value.Trim();
    }
}
