using System.Collections.ObjectModel;

namespace QS3D.Platform.Parity;

public sealed class ElementCostLine
{
    public ElementCostLine(string lineId, string? elementCode, decimal cost, string nodePath = "Project")
    {
        LineId = Text.Require(lineId, nameof(lineId));
        ElementCode = NormalizeElementCode(elementCode);
        NodePath = Text.Require(nodePath, nameof(nodePath));
        if (cost < 0m) throw new ArgumentOutOfRangeException(nameof(cost));
        Cost = cost;
    }

    public string LineId { get; }
    public string ElementCode { get; }
    public decimal Cost { get; }
    public string NodePath { get; }

    private static string NormalizeElementCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Unclassified" : Text.Require(value, nameof(value));
}

public sealed class ElementCostSummary
{
    internal ElementCostSummary(string elementCode, decimal cost, decimal totalCost, decimal analysisAreaM2, int sourceLineCount)
    {
        ElementCode = elementCode;
        Cost = cost;
        SharePercent = totalCost == 0m ? 0m : checked(cost * 100m / totalCost);
        CostPerM2 = analysisAreaM2 == 0m ? null : cost / analysisAreaM2;
        SourceLineCount = sourceLineCount;
    }

    public string ElementCode { get; }
    public decimal Cost { get; }
    public decimal SharePercent { get; }
    public decimal? CostPerM2 { get; }
    public int SourceLineCount { get; }
}

public sealed class ElementCostAnalysisResult
{
    internal ElementCostAnalysisResult(string? nodePath, decimal analysisAreaM2, decimal totalCost, IReadOnlyList<ElementCostSummary> rows, int sourceLineCount)
    {
        NodePath = nodePath;
        AnalysisAreaM2 = analysisAreaM2;
        TotalCost = totalCost;
        TotalCostPerM2 = analysisAreaM2 == 0m ? null : totalCost / analysisAreaM2;
        Rows = rows;
        SourceLineCount = sourceLineCount;
    }

    public string? NodePath { get; }
    public decimal AnalysisAreaM2 { get; }
    public decimal TotalCost { get; }
    public decimal? TotalCostPerM2 { get; }
    public IReadOnlyList<ElementCostSummary> Rows { get; }
    public int SourceLineCount { get; }
}

public static class ElementCostAnalysisService
{
    public static ElementCostAnalysisResult Analyze(IEnumerable<ElementCostLine> lines, decimal analysisAreaM2, string? nodePath = null)
    {
        if (lines is null) throw new ArgumentNullException(nameof(lines));
        if (analysisAreaM2 < 0m) throw new ArgumentOutOfRangeException(nameof(analysisAreaM2));
        var normalizedNodePath = string.IsNullOrWhiteSpace(nodePath) ? null : Text.Require(nodePath, nameof(nodePath));

        var source = new List<ElementCostLine>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            if (line is null) throw new ArgumentException("Element analysis contains a null line.", nameof(lines));
            if (!ids.Add(line.LineId)) throw new ArgumentException("Duplicate element-analysis line id: " + line.LineId + ".", nameof(lines));
            if (normalizedNodePath is null || IsWithinNode(line.NodePath, normalizedNodePath)) source.Add(line);
        }

        var grouped = source.GroupBy(static line => line.ElementCode, StringComparer.OrdinalIgnoreCase).ToArray();
        var totalCost = source.Aggregate(0m, static (total, line) => checked(total + line.Cost));
        var rows = new List<ElementCostSummary>(grouped.Length);
        foreach (var group in grouped)
        {
            var code = group.Select(static line => line.ElementCode).OrderBy(static value => value, StringComparer.Ordinal).First();
            var cost = group.Aggregate(0m, static (total, line) => checked(total + line.Cost));
            rows.Add(new ElementCostSummary(code, cost, totalCost, analysisAreaM2, group.Count()));
        }
        rows.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.ElementCode, right.ElementCode));

        return new ElementCostAnalysisResult(
            normalizedNodePath,
            analysisAreaM2,
            totalCost,
            new ReadOnlyCollection<ElementCostSummary>(rows),
            source.Count);
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
