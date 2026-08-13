namespace QS3D.Platform.Families;

public interface IFamilySchemaMigration
{
    string SchemaId { get; }
    int FromVersion { get; }
    int ToVersion { get; }
    FamilyParameterSet Apply(FamilyParameterSet source);
}

public sealed class FamilySchemaMigrationRegistry
{
    private readonly Dictionary<string, IFamilySchemaMigration> _steps = new(StringComparer.Ordinal);

    public FamilySchemaMigrationRegistry(IEnumerable<IFamilySchemaMigration>? steps = null)
    {
        if (steps is null) return;
        foreach (var step in steps) Add(step);
    }

    public void Add(IFamilySchemaMigration step)
    {
        if (step is null) throw new ArgumentNullException(nameof(step));
        if (string.IsNullOrWhiteSpace(step.SchemaId)) throw new InvalidOperationException("Family schema ID must not be blank.");
        if (step.FromVersion < 1 || step.ToVersion != step.FromVersion + 1) throw new InvalidOperationException("Family migration must advance exactly one positive version.");
        var key = Key(step.SchemaId, step.FromVersion);
        if (_steps.ContainsKey(key)) throw new InvalidOperationException($"Duplicate family migration '{key}'.");
        _steps.Add(key, step);
    }

    public FamilyParameterSet Migrate(FamilyParameterSet source, FamilySchemaDefinition target)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (!StringComparer.Ordinal.Equals(source.SchemaId, target.SchemaId)) throw new InvalidOperationException("Family migration cannot change schema identity.");
        if (source.SchemaVersion > target.Version) throw new InvalidOperationException("Implicit family downgrade is not supported.");
        var current = source;
        while (current.SchemaVersion < target.Version)
        {
            if (!_steps.TryGetValue(Key(current.SchemaId, current.SchemaVersion), out var step))
                throw new InvalidOperationException($"Missing family migration for '{current.SchemaId}' v{current.SchemaVersion}.");
            var next = step.Apply(current) ?? throw new InvalidOperationException("Family migration returned null.");
            if (!StringComparer.Ordinal.Equals(next.SchemaId, current.SchemaId) || next.SchemaVersion != current.SchemaVersion + 1)
                throw new InvalidOperationException("Family migration returned invalid identity/version.");
            current = next;
        }
        current = FamilySchemaValidator.ApplyDefaults(target, current);
        FamilySchemaValidator.Validate(target, current);
        return current;
    }

    private static string Key(string id, int version)
        => id.Trim().ToLowerInvariant() + "|" + version.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
