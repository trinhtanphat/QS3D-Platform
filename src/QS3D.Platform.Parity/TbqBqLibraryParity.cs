using System.Collections.ObjectModel;

namespace QS3D.Platform.Parity;

public enum BqLibraryNodeKind
{
    Category,
    Subcategory,
    Heading,
    Bill
}

public sealed class BqLibraryNode
{
    internal BqLibraryNode(
        string nodeId,
        BqLibraryNodeKind kind,
        string name,
        string? parentNodeId,
        BqLibraryItem? billItem)
    {
        NodeId = Text.Require(nodeId, nameof(nodeId));
        Kind = kind;
        Name = Text.Require(name, nameof(name));
        ParentNodeId = parentNodeId;
        BillItem = billItem;
    }

    public string NodeId { get; }
    public BqLibraryNodeKind Kind { get; }
    public string Name { get; }
    public string? ParentNodeId { get; }
    public BqLibraryItem? BillItem { get; }
}

public sealed class TbqBqLibraryWorkspace
{
    private readonly IReadOnlyList<BqLibraryNode> _nodes;

    private TbqBqLibraryWorkspace(string name, IEnumerable<BqLibraryNode> nodes)
    {
        Name = Text.Require(name, nameof(name));
        _nodes = new ReadOnlyCollection<BqLibraryNode>(
            nodes.OrderBy(static node => node.NodeId, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public string Name { get; }
    public IReadOnlyList<BqLibraryNode> Nodes => _nodes;

    public static TbqBqLibraryWorkspace Create(string name) =>
        new(name, Array.Empty<BqLibraryNode>());

    public TbqBqLibraryWorkspace AddContainer(
        string nodeId,
        BqLibraryNodeKind kind,
        string name,
        string? parentNodeId = null)
    {
        if (kind == BqLibraryNodeKind.Bill)
            throw new ArgumentException("Use AddBill for BQ Library bill nodes.", nameof(kind));
        nodeId = Text.Require(nodeId, nameof(nodeId));
        EnsureNodeIdAvailable(nodeId);
        var parent = ResolveContainer(parentNodeId);
        return WithAdded(new BqLibraryNode(nodeId, kind, name, parent?.NodeId, null));
    }

    public TbqBqLibraryWorkspace AddBill(
        string nodeId,
        BqLibraryItem billItem,
        string? parentNodeId = null)
    {
        nodeId = Text.Require(nodeId, nameof(nodeId));
        if (billItem is null) throw new ArgumentNullException(nameof(billItem));
        EnsureNodeIdAvailable(nodeId);
        EnsureBillCodeAvailable(billItem.ItemCode);
        var parent = ResolveContainer(parentNodeId);
        return WithAdded(new BqLibraryNode(nodeId, BqLibraryNodeKind.Bill, billItem.Description, parent?.NodeId, billItem));
    }

    public TbqBqLibraryWorkspace ImportFromProject(
        IEnumerable<BqLibraryItem> projectBills,
        string destinationNodeId)
    {
        if (projectBills is null) throw new ArgumentNullException(nameof(projectBills));
        var destination = ResolveContainer(Text.Require(destinationNodeId, nameof(destinationNodeId)))!;
        var incoming = new List<BqLibraryItem>();
        var incomingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var incomingNodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var bill in projectBills)
        {
            if (bill is null) throw new ArgumentException("BQ Library project import contains a null bill.", nameof(projectBills));
            if (!incomingCodes.Add(bill.ItemCode))
                throw new ArgumentException("Duplicate project bill item code: " + bill.ItemCode + ".", nameof(projectBills));
            EnsureBillCodeAvailable(bill.ItemCode);
            var nodeId = ProjectBillNodeId(bill.ItemCode);
            EnsureNodeIdAvailable(nodeId);
            if (!incomingNodeIds.Add(nodeId))
                throw new ArgumentException("Duplicate generated BQ Library node id: " + nodeId + ".", nameof(projectBills));
            incoming.Add(bill);
        }

        var next = _nodes.ToList();
        foreach (var bill in incoming.OrderBy(static item => item.ItemCode, StringComparer.OrdinalIgnoreCase))
        {
            next.Add(new BqLibraryNode(
                ProjectBillNodeId(bill.ItemCode),
                BqLibraryNodeKind.Bill,
                bill.Description,
                destination.NodeId,
                bill));
        }
        return new TbqBqLibraryWorkspace(Name, next);
    }

    public IReadOnlyList<BqLibraryNode> ChildrenOf(string? parentNodeId = null)
    {
        string? canonicalParent = null;
        if (parentNodeId is not null)
            canonicalParent = ResolveNode(Text.Require(parentNodeId, nameof(parentNodeId))).NodeId;
        return new ReadOnlyCollection<BqLibraryNode>(_nodes
            .Where(node => StringComparer.OrdinalIgnoreCase.Equals(node.ParentNodeId, canonicalParent))
            .OrderBy(static node => node.NodeId, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    private TbqBqLibraryWorkspace WithAdded(BqLibraryNode node)
    {
        var next = _nodes.ToList();
        next.Add(node);
        return new TbqBqLibraryWorkspace(Name, next);
    }

    private BqLibraryNode? ResolveContainer(string? nodeId)
    {
        if (nodeId is null) return null;
        var node = ResolveNode(Text.Require(nodeId, nameof(nodeId)));
        if (node.Kind == BqLibraryNodeKind.Bill)
            throw new InvalidOperationException("BQ Library bill nodes cannot contain child nodes: " + node.NodeId + ".");
        return node;
    }

    private BqLibraryNode ResolveNode(string nodeId)
    {
        nodeId = Text.Require(nodeId, nameof(nodeId));
        var node = _nodes.FirstOrDefault(candidate => StringComparer.OrdinalIgnoreCase.Equals(candidate.NodeId, nodeId));
        return node ?? throw new InvalidOperationException("BQ Library node was not found: " + nodeId + ".");
    }

    private void EnsureNodeIdAvailable(string nodeId)
    {
        if (_nodes.Any(node => StringComparer.OrdinalIgnoreCase.Equals(node.NodeId, nodeId)))
            throw new InvalidOperationException("BQ Library node id already exists: " + nodeId + ".");
    }

    private void EnsureBillCodeAvailable(string itemCode)
    {
        if (_nodes.Any(node => node.BillItem is not null &&
                              StringComparer.OrdinalIgnoreCase.Equals(node.BillItem.ItemCode, itemCode)))
            throw new InvalidOperationException("BQ Library bill item already exists: " + itemCode + ".");
    }

    private static string ProjectBillNodeId(string itemCode) => "PROJECT:" + Text.Require(itemCode, nameof(itemCode));
}
