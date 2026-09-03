using System.Collections;

namespace QS3D.Platform.Persistence;

public interface ISemanticSnapshotMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    SemanticProjectSnapshot Apply(SemanticProjectSnapshot source);
}

public sealed class SemanticSnapshotMigrator
{
    private const int MaxMigrationSteps = 256;
    private readonly Dictionary<int, ISemanticSnapshotMigration> _steps;

    public SemanticSnapshotMigrator(IEnumerable<ISemanticSnapshotMigration> steps)
    {
        if (steps is null) throw new ArgumentNullException(nameof(steps));

        var advertisedCount = ReadAdvertisedCount(steps);
        _steps = advertisedCount.HasValue
            ? new Dictionary<int, ISemanticSnapshotMigration>(advertisedCount.Value)
            : new Dictionary<int, ISemanticSnapshotMigration>();

        var enumeratedCount = 0;
        foreach (var step in steps)
        {
            if (enumeratedCount >= MaxMigrationSteps)
                throw new ArgumentException($"Migration registry exceeds the {MaxMigrationSteps} step limit.", nameof(steps));
            enumeratedCount++;

            if (step is null) throw new ArgumentException("Migration chain must not contain null steps.", nameof(steps));
            if (step.FromVersion < 1) throw new ArgumentOutOfRangeException(nameof(steps), "Migration source version must be positive.");
            if (step.ToVersion <= step.FromVersion) throw new ArgumentException("Migration target version must be greater than source version.", nameof(steps));
            if (_steps.ContainsKey(step.FromVersion))
                throw new InvalidOperationException($"Multiple semantic migrations start from schema {step.FromVersion}.");
            _steps.Add(step.FromVersion, step);
        }

        if (advertisedCount.HasValue && enumeratedCount != advertisedCount.Value)
            throw new ArgumentException("Migration registry Count does not match enumeration.", nameof(steps));

        var finalCount = ReadAdvertisedCount(steps);
        if (advertisedCount != finalCount || (finalCount.HasValue && finalCount.Value != enumeratedCount))
            throw new ArgumentException("Migration registry Count changed during materialization.", nameof(steps));
    }

    public SemanticProjectSnapshot Migrate(SemanticProjectSnapshot source, int targetVersion)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (targetVersion < source.SchemaVersion)
            throw new InvalidOperationException($"Semantic snapshot downgrade from {source.SchemaVersion} to {targetVersion} is not supported.");
        if (targetVersion == source.SchemaVersion) return source;

        var current = source;
        var guard = 0;
        while (current.SchemaVersion < targetVersion)
        {
            if (++guard > MaxMigrationSteps) throw new InvalidOperationException("Semantic migration chain exceeded the safety step limit.");
            if (!_steps.TryGetValue(current.SchemaVersion, out var step))
                throw new InvalidOperationException($"No semantic migration is registered from schema {current.SchemaVersion} toward {targetVersion}.");
            if (step.ToVersion > targetVersion)
                throw new InvalidOperationException($"Migration {step.FromVersion}->{step.ToVersion} overshoots requested target schema {targetVersion}.");

            var migrated = step.Apply(current) ?? throw new InvalidOperationException($"Migration {step.FromVersion}->{step.ToVersion} returned null.");
            if (migrated.SchemaVersion != step.ToVersion)
                throw new InvalidOperationException($"Migration {step.FromVersion}->{step.ToVersion} returned schema {migrated.SchemaVersion}.");
            if (migrated.ProjectId != source.ProjectId)
                throw new InvalidOperationException($"Migration {step.FromVersion}->{step.ToVersion} changed stable project identity.");
            current = migrated;
        }
        return current;
    }

    private static int? ReadAdvertisedCount(IEnumerable<ISemanticSnapshotMigration> steps)
    {
        int? count = null;
        if (steps is ICollection<ISemanticSnapshotMigration> collection) MergeCount(ref count, collection.Count);
        if (steps is IReadOnlyCollection<ISemanticSnapshotMigration> readOnlyCollection) MergeCount(ref count, readOnlyCollection.Count);
        if (steps is ICollection nonGenericCollection) MergeCount(ref count, nonGenericCollection.Count);
        return count;
    }

    private static void MergeCount(ref int? observed, int candidate)
    {
        if (candidate < 0)
            throw new ArgumentException("Migration registry Count must not be negative.", "steps");
        if (candidate > MaxMigrationSteps)
            throw new ArgumentException($"Migration registry exceeds the {MaxMigrationSteps} step limit.", "steps");
        if (observed.HasValue && observed.Value != candidate)
            throw new ArgumentException("Migration registry exposes conflicting Count values.", "steps");
        observed = candidate;
    }
}
