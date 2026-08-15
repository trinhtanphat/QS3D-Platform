using System.Collections.ObjectModel;
using QS3D.Platform.Domain;

namespace QS3D.Platform.Parity;

public enum BeamSizeReadMode
{
    WidthByHeight = 0,
    HeightByWidth = 1
}

public enum BeamEndExtensionMode
{
    None = 0,
    WithinTolerance = 1,
    Always = 2
}

public sealed class ColorClassificationRule
{
    public ColorClassificationRule(string id, int colorIndex, string classification, int priority = 0)
    {
        Id = Text.Require(id, nameof(id));
        if (colorIndex < 0 || colorIndex > 257) throw new ArgumentOutOfRangeException(nameof(colorIndex));
        ColorIndex = colorIndex;
        Classification = Text.Require(classification, nameof(classification));
        Priority = priority;
    }

    public string Id { get; }
    public int ColorIndex { get; }
    public string Classification { get; }
    public int Priority { get; }
}

public sealed class CadIdentificationProfile
{
    public CadIdentificationProfile(
        bool ignoreImportedHatches,
        BeamSizeReadMode beamSizeReadMode,
        BeamEndExtensionMode beamEndExtensionMode,
        double beamExtensionToleranceM,
        IEnumerable<ColorClassificationRule>? colorRules = null,
        bool supportsPdfTextRecognition = false,
        bool supportsPdfTextRestore = false)
    {
        if (!Enum.IsDefined(typeof(BeamSizeReadMode), beamSizeReadMode)) throw new ArgumentOutOfRangeException(nameof(beamSizeReadMode));
        if (!Enum.IsDefined(typeof(BeamEndExtensionMode), beamEndExtensionMode)) throw new ArgumentOutOfRangeException(nameof(beamEndExtensionMode));
        IgnoreImportedHatches = ignoreImportedHatches;
        BeamSizeReadMode = beamSizeReadMode;
        BeamEndExtensionMode = beamEndExtensionMode;
        BeamExtensionToleranceM = Numeric.NonNegativeFinite(beamExtensionToleranceM, nameof(beamExtensionToleranceM));
        SupportsPdfTextRecognition = supportsPdfTextRecognition;
        SupportsPdfTextRestore = supportsPdfTextRestore;

        var rules = new List<ColorClassificationRule>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (colorRules is not null)
        {
            foreach (var rule in colorRules)
            {
                if (rule is null) throw new ArgumentException("Color classification rules contain null.", nameof(colorRules));
                if (!ids.Add(rule.Id)) throw new ArgumentException("Duplicate color classification rule id: " + rule.Id + ".", nameof(colorRules));
                rules.Add(rule);
            }
        }
        rules.Sort(static (left, right) =>
        {
            var priority = right.Priority.CompareTo(left.Priority);
            return priority != 0 ? priority : StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id);
        });
        ColorRules = new ReadOnlyCollection<ColorClassificationRule>(rules);
    }

    public bool IgnoreImportedHatches { get; }
    public BeamSizeReadMode BeamSizeReadMode { get; }
    public BeamEndExtensionMode BeamEndExtensionMode { get; }
    public double BeamExtensionToleranceM { get; }
    public IReadOnlyList<ColorClassificationRule> ColorRules { get; }
    public bool SupportsPdfTextRecognition { get; }
    public bool SupportsPdfTextRestore { get; }

    public string? ClassifyColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex > 257) throw new ArgumentOutOfRangeException(nameof(colorIndex));
        for (var i = 0; i < ColorRules.Count; i++)
            if (ColorRules[i].ColorIndex == colorIndex) return ColorRules[i].Classification;
        return null;
    }

    public bool ShouldExtendBeamEnd(double gapM)
    {
        gapM = Numeric.NonNegativeFinite(gapM, nameof(gapM));
        return BeamEndExtensionMode switch
        {
            BeamEndExtensionMode.None => false,
            BeamEndExtensionMode.Always => true,
            BeamEndExtensionMode.WithinTolerance => gapM <= BeamExtensionToleranceM,
            _ => throw new InvalidOperationException("Unsupported beam extension mode.")
        };
    }
}

public enum CoordinationIssueKind
{
    HardClash = 0,
    ClearanceClash = 1,
    ExactHardClash = 2,
    Review = 3
}

public enum CoordinationIssueSeverity
{
    Info = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum CoordinationIssueStatus
{
    Open = 0,
    InReview = 1,
    Resolved = 2,
    Closed = 3
}

public sealed class CoordinationIssueComment
{
    public CoordinationIssueComment(string id, string author, string text, DateTime createdAtUtc)
    {
        Id = Text.Require(id, nameof(id));
        Author = Text.Require(author, nameof(author));
        Text = Text.Require(text, nameof(text));
        if (createdAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Comment timestamp must be UTC.", nameof(createdAtUtc));
        CreatedAtUtc = createdAtUtc;
    }

    public string Id { get; }
    public string Author { get; }
    public string Text { get; }
    public DateTime CreatedAtUtc { get; }
}

public sealed class CoordinationIssue
{
    private readonly List<CoordinationIssueComment> _comments = new();
    private readonly HashSet<string> _commentIds = new(StringComparer.OrdinalIgnoreCase);

    public CoordinationIssue(
        string issueId,
        CoordinationIssueKind kind,
        CoordinationIssueSeverity severity,
        string title,
        string leftSemanticId,
        string rightSemanticId,
        CadReference? leftCadReference,
        CadReference? rightCadReference,
        string disciplineContext,
        string categoryContext,
        string systemContext,
        string regionContext,
        double separationM,
        DateTime createdAtUtc,
        string? assignee = null)
    {
        IssueId = Text.Require(issueId, nameof(issueId));
        if (!Enum.IsDefined(typeof(CoordinationIssueKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(typeof(CoordinationIssueSeverity), severity)) throw new ArgumentOutOfRangeException(nameof(severity));
        Kind = kind;
        Severity = severity;
        Title = Text.Require(title, nameof(title));
        LeftSemanticId = Text.Require(leftSemanticId, nameof(leftSemanticId));
        RightSemanticId = Text.Require(rightSemanticId, nameof(rightSemanticId));
        if (StringComparer.OrdinalIgnoreCase.Equals(LeftSemanticId, RightSemanticId)) throw new ArgumentException("Coordination issue sides must be distinct.");
        LeftCadReference = leftCadReference;
        RightCadReference = rightCadReference;
        DisciplineContext = Text.Require(disciplineContext, nameof(disciplineContext));
        CategoryContext = Text.Require(categoryContext, nameof(categoryContext));
        SystemContext = Text.Require(systemContext, nameof(systemContext));
        RegionContext = Text.Require(regionContext, nameof(regionContext));
        SeparationM = Numeric.NonNegativeFinite(separationM, nameof(separationM));
        if (createdAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Issue timestamp must be UTC.", nameof(createdAtUtc));
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        Status = CoordinationIssueStatus.Open;
        Assignee = string.IsNullOrWhiteSpace(assignee) ? null : assignee.Trim();
    }

    public string IssueId { get; }
    public CoordinationIssueKind Kind { get; }
    public CoordinationIssueSeverity Severity { get; private set; }
    public CoordinationIssueStatus Status { get; private set; }
    public string Title { get; private set; }
    public string LeftSemanticId { get; }
    public string RightSemanticId { get; }
    public CadReference? LeftCadReference { get; }
    public CadReference? RightCadReference { get; }
    public string DisciplineContext { get; }
    public string CategoryContext { get; }
    public string SystemContext { get; }
    public string RegionContext { get; }
    public double SeparationM { get; }
    public string? Assignee { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; private set; }
    public IReadOnlyList<CoordinationIssueComment> Comments => new ReadOnlyCollection<CoordinationIssueComment>(_comments);

    public void Assign(string? assignee, DateTime changedAtUtc)
    {
        ValidateMutationTime(changedAtUtc);
        Assignee = string.IsNullOrWhiteSpace(assignee) ? null : assignee.Trim();
        UpdatedAtUtc = changedAtUtc;
    }

    public void SetSeverity(CoordinationIssueSeverity severity, DateTime changedAtUtc)
    {
        if (!Enum.IsDefined(typeof(CoordinationIssueSeverity), severity)) throw new ArgumentOutOfRangeException(nameof(severity));
        ValidateMutationTime(changedAtUtc);
        Severity = severity;
        UpdatedAtUtc = changedAtUtc;
    }

    public void Rename(string title, DateTime changedAtUtc)
    {
        ValidateMutationTime(changedAtUtc);
        Title = Text.Require(title, nameof(title));
        UpdatedAtUtc = changedAtUtc;
    }

    public void TransitionTo(CoordinationIssueStatus next, DateTime changedAtUtc)
    {
        if (!Enum.IsDefined(typeof(CoordinationIssueStatus), next)) throw new ArgumentOutOfRangeException(nameof(next));
        ValidateMutationTime(changedAtUtc);
        if (!CanTransition(Status, next)) throw new InvalidOperationException("Invalid coordination issue transition: " + Status + " -> " + next + ".");
        Status = next;
        UpdatedAtUtc = changedAtUtc;
    }

    public void AddComment(CoordinationIssueComment comment)
    {
        if (comment is null) throw new ArgumentNullException(nameof(comment));
        ValidateMutationTime(comment.CreatedAtUtc);
        if (!_commentIds.Add(comment.Id)) throw new InvalidOperationException("Duplicate coordination issue comment id: " + comment.Id + ".");
        _comments.Add(comment);
        _comments.Sort(static (left, right) =>
        {
            var time = left.CreatedAtUtc.CompareTo(right.CreatedAtUtc);
            return time != 0 ? time : StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id);
        });
        UpdatedAtUtc = comment.CreatedAtUtc;
    }

    public static bool CanTransition(CoordinationIssueStatus current, CoordinationIssueStatus next)
    {
        if (current == next) return true;
        return current switch
        {
            CoordinationIssueStatus.Open => next == CoordinationIssueStatus.InReview || next == CoordinationIssueStatus.Resolved || next == CoordinationIssueStatus.Closed,
            CoordinationIssueStatus.InReview => next == CoordinationIssueStatus.Open || next == CoordinationIssueStatus.Resolved || next == CoordinationIssueStatus.Closed,
            CoordinationIssueStatus.Resolved => next == CoordinationIssueStatus.Open || next == CoordinationIssueStatus.InReview || next == CoordinationIssueStatus.Closed,
            CoordinationIssueStatus.Closed => next == CoordinationIssueStatus.Open,
            _ => false
        };
    }

    private void ValidateMutationTime(DateTime timestampUtc)
    {
        if (timestampUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Mutation timestamp must be UTC.", nameof(timestampUtc));
        if (timestampUtc < UpdatedAtUtc) throw new InvalidOperationException("Coordination issue mutation timestamp cannot move backwards.");
    }
}

public sealed class CoordinationIssueCatalog
{
    private readonly Dictionary<string, CoordinationIssue> _issues = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<CoordinationIssue> Issues => new ReadOnlyCollection<CoordinationIssue>(_issues.Values.OrderBy(static x => x.IssueId, StringComparer.OrdinalIgnoreCase).ToList());

    public void Add(CoordinationIssue issue)
    {
        if (issue is null) throw new ArgumentNullException(nameof(issue));
        if (_issues.ContainsKey(issue.IssueId)) throw new InvalidOperationException("Duplicate coordination issue id: " + issue.IssueId + ".");
        _issues.Add(issue.IssueId, issue);
    }

    public CoordinationIssue? Find(string issueId)
    {
        issueId = Text.Require(issueId, nameof(issueId));
        return _issues.TryGetValue(issueId, out var issue) ? issue : null;
    }
}
