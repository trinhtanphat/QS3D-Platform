namespace QS3D.Platform.Families;

public sealed class RenameFamilyParameterStep : IFamilySchemaMigration
{
    public RenameFamilyParameterStep(string schemaId, int fromVersion, string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(schemaId)) throw new ArgumentException("Schema ID must not be blank.", nameof(schemaId));
        if (fromVersion < 1) throw new ArgumentOutOfRangeException(nameof(fromVersion));
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Parameter names must not be blank.");
        SchemaId = schemaId.Trim().ToLowerInvariant(); FromVersion = fromVersion; OldName = oldName.Trim(); NewName = newName.Trim();
    }
    public string SchemaId { get; }
    public int FromVersion { get; }
    public int ToVersion => FromVersion + 1;
    public string OldName { get; }
    public string NewName { get; }
    public FamilyParameterSet Apply(FamilyParameterSet source)
    {
        Validate(source);
        if (!source.Values.TryGetValue(OldName, out var value)) throw new InvalidOperationException($"Missing parameter '{OldName}'.");
        if (source.Values.ContainsKey(NewName)) throw new InvalidOperationException($"Parameter '{NewName}' already exists.");
        return source.With(NewName, value).Without(OldName).AtVersion(ToVersion);
    }
    private void Validate(FamilyParameterSet source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (!StringComparer.Ordinal.Equals(source.SchemaId, SchemaId) || source.SchemaVersion != FromVersion) throw new InvalidOperationException("Family migration source mismatch.");
    }
}

public sealed class AddFamilyParameterStep : IFamilySchemaMigration
{
    private readonly FamilyParameterValue _value;
    public AddFamilyParameterStep(string schemaId, int fromVersion, string name, FamilyParameterValue value)
    {
        if (string.IsNullOrWhiteSpace(schemaId)) throw new ArgumentException("Schema ID must not be blank.", nameof(schemaId));
        if (fromVersion < 1) throw new ArgumentOutOfRangeException(nameof(fromVersion));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Parameter name must not be blank.", nameof(name));
        SchemaId = schemaId.Trim().ToLowerInvariant(); FromVersion = fromVersion; Name = name.Trim(); _value = value ?? throw new ArgumentNullException(nameof(value));
    }
    public string SchemaId { get; }
    public int FromVersion { get; }
    public int ToVersion => FromVersion + 1;
    public string Name { get; }
    public FamilyParameterSet Apply(FamilyParameterSet source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (!StringComparer.Ordinal.Equals(source.SchemaId, SchemaId) || source.SchemaVersion != FromVersion) throw new InvalidOperationException("Family migration source mismatch.");
        if (source.Values.ContainsKey(Name)) throw new InvalidOperationException($"Parameter '{Name}' already exists.");
        return source.With(Name, _value).AtVersion(ToVersion);
    }
}
