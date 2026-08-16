using System.Collections.ObjectModel;

namespace QS3D.Platform.Parity;

public sealed class TradeAnalysisLine
{
    public TradeAnalysisLine(string lineId, string? tradeCode, decimal cost, string nodePath = "Project")
    {
        LineId = Text.Require(lineId, nameof(lineId));
        TradeCode = string.IsNullOrWhiteSpace(tradeCode) ? "Unclassified" : Text.Require(tradeCode, nameof(tradeCode));
        NodePath = Text.Require(nodePath, nameof(nodePath));
        if (cost < 0m) throw new ArgumentOutOfRangeException(nameof(cost));
        Cost = cost;
    }

    public string LineId { get; }
    public string TradeCode { get; }
    public decimal Cost { get; }
    public string NodePath { get; }
}

public sealed class TradeAnalysisSnapshot
{
    internal TradeAnalysisSnapshot(
        string? nodePath,
        decimal cfaM2,
        decimal totalCost,
        IReadOnlyList<TradeCostSummary> rows,
        int sourceLineCount)
    {
        NodePath = nodePath;
        CfaM2 = cfaM2;
        TotalCost = totalCost;
        TotalCostPerM2 = cfaM2 == 0m ? null : totalCost / cfaM2;
        Rows = rows;
        SourceLineCount = sourceLineCount;
    }

    public string? NodePath { get; }
    public decimal CfaM2 { get; }
    public decimal TotalCost { get; }
    public decimal? TotalCostPerM2 { get; }
    public IReadOnlyList<TradeCostSummary> Rows { get; }
    public int SourceLineCount { get; }
}

public sealed class TradeAnalysisWorkspace
{
    public TradeAnalysisSnapshot? Current { get; private set; }

    public TradeAnalysisSnapshot Refresh(IEnumerable<TradeAnalysisLine> lines, decimal cfaM2, string? nodePath = null)
    {
        if (lines is null) throw new ArgumentNullException(nameof(lines));
        if (cfaM2 < 0m) throw new ArgumentOutOfRangeException(nameof(cfaM2));
        var normalizedNodePath = string.IsNullOrWhiteSpace(nodePath) ? null : Text.Require(nodePath, nameof(nodePath));

        var source = new List<TradeAnalysisLine>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            if (line is null) throw new ArgumentException("Trade analysis contains a null line.", nameof(lines));
            if (!ids.Add(line.LineId))
                throw new ArgumentException("Duplicate trade-analysis line id: " + line.LineId + ".", nameof(lines));
            if (normalizedNodePath is null || IsWithinNode(line.NodePath, normalizedNodePath)) source.Add(line);
        }

        var rows = TradeCostAnalysisService.Analyze(
            source.Select(static line => new TradeCostLine(line.TradeCode, line.Cost)),
            cfaM2);
        var totalCost = source.Aggregate(0m, static (total, line) => checked(total + line.Cost));
        var snapshot = new TradeAnalysisSnapshot(
            normalizedNodePath,
            cfaM2,
            totalCost,
            new ReadOnlyCollection<TradeCostSummary>(rows.ToList()),
            source.Count);
        Current = snapshot;
        return snapshot;
    }

    private static bool IsWithinNode(string candidatePath, string selectedPath)
    {
        if (StringComparer.OrdinalIgnoreCase.Equals(candidatePath, selectedPath)) return true;
        if (candidatePath.Length <= selectedPath.Length ||
            !candidatePath.StartsWith(selectedPath, StringComparison.OrdinalIgnoreCase)) return false;
        var boundary = candidatePath[selectedPath.Length];
        return boundary == '/' || boundary == '\\';
    }
}
