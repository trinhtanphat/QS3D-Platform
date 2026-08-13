namespace QS3D.Platform.Persistence;

public interface ISemanticSnapshotMigration
{
    int FromVersion { get; }
    int ToVersion { get; }
    SemanticProjectSnapshot Apply(SemanticProjectSnapshot source);
}

public sealed class SemanticSnapshotMigrator
{
    private readonly Dictionary<int, ISemanticSnapshotMigration> _steps;

    public SemanticSnapshotMigrator(IEnumerable<ISemanticSnapshotMigration> steps)
    {
        if (steps is null) throw new ArgumentNullException(nameof(steps));
        _steps = new Dictionary<int, ISemanticSnapshotMigration>();
        foreach (var step in steps)
        {
            if (step is null) throw new ArgumentException("Migration chain must not contain null steps.", nameof(steps));
            if (step.FromVersion < 1) throw new ArgumentOutOfRangeException(nameof(steps), "Migration source version must be positive.");
            if (step.ToVersion <= step.FromVersion) throw new ArgumentException("Migration target version must be greater than source version.", nameof(steps));
            if (_steps.ContainsKey(step.FromVersion))
                throw new InvalidOperationException($"Multiple semantic migrations start from schema {step.FromVersion}.");
            _steps.Add(step.FromVersion, step);
        }
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
            if (++guard > 256) throw new InvalidOperationException("Semantic migration chain exceeded the safety step limit.");
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
}
