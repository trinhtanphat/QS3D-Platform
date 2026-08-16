using System.Collections.ObjectModel;

namespace QS3D.Platform.Parity;

public sealed class ResourceLibraryBatchImportResult
{
    internal ResourceLibraryBatchImportResult(
        string libraryId,
        string sourceProjectId,
        IReadOnlyList<CostRateBuildUp> rates,
        IReadOnlyList<string> sourceRateIds)
    {
        LibraryId = libraryId;
        SourceProjectId = sourceProjectId;
        Rates = rates;
        SourceRateIds = sourceRateIds;
    }

    public string LibraryId { get; }
    public string SourceProjectId { get; }
    public IReadOnlyList<CostRateBuildUp> Rates { get; }
    public IReadOnlyList<string> SourceRateIds { get; }
}

public sealed class TbqResourceLibrary
{
    private readonly IReadOnlyList<CostRateBuildUp> _rates;
    private readonly Dictionary<string, CostRateBuildUp> _rateById;

    private TbqResourceLibrary(string libraryId, string sourceProjectId, IEnumerable<CostRateBuildUp> projectRates)
    {
        LibraryId = Text.Require(libraryId, nameof(libraryId));
        SourceProjectId = Text.Require(sourceProjectId, nameof(sourceProjectId));
        if (projectRates is null) throw new ArgumentNullException(nameof(projectRates));

        _rateById = new Dictionary<string, CostRateBuildUp>(StringComparer.OrdinalIgnoreCase);
        foreach (var rate in projectRates)
        {
            if (rate is null) throw new ArgumentException("Resource Library project payload contains a null rate.", nameof(projectRates));
            if (!_rateById.TryAdd(rate.Id, rate))
                throw new ArgumentException("Duplicate Resource Library rate id: " + rate.Id + ".", nameof(projectRates));
        }

        _rates = new ReadOnlyCollection<CostRateBuildUp>(
            _rateById.Values.OrderBy(static rate => rate.Id, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public string LibraryId { get; }
    public string SourceProjectId { get; }
    public IReadOnlyList<CostRateBuildUp> Rates => _rates;

    public static TbqResourceLibrary ImportFromProject(
        string libraryId,
        string sourceProjectId,
        IEnumerable<CostRateBuildUp> projectRates) =>
        new(libraryId, sourceProjectId, projectRates);

    public ResourceLibraryBatchImportResult BatchImport(IEnumerable<string> rateIds)
    {
        if (rateIds is null) throw new ArgumentNullException(nameof(rateIds));

        var requestedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selected = new List<CostRateBuildUp>();
        foreach (var requestedIdValue in rateIds)
        {
            var requestedId = Text.Require(requestedIdValue, nameof(rateIds));
            if (!requestedIds.Add(requestedId))
                throw new ArgumentException("Duplicate Resource Library batch request id: " + requestedId + ".", nameof(rateIds));
            if (!_rateById.TryGetValue(requestedId, out var rate))
                throw new InvalidOperationException("Resource Library rate was not found: " + requestedId + ".");
            selected.Add(rate);
        }

        if (selected.Count == 0)
            throw new ArgumentException("Resource Library batch import requires at least one explicit rate selection.", nameof(rateIds));

        selected.Sort(static (left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id));
        var rates = new ReadOnlyCollection<CostRateBuildUp>(selected);
        var sourceIds = new ReadOnlyCollection<string>(selected.Select(static rate => rate.Id).ToList());
        return new ResourceLibraryBatchImportResult(LibraryId, SourceProjectId, rates, sourceIds);
    }
}
