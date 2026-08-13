using QS3D.Platform.Domain;

namespace QS3D.Platform.Families;

public sealed class ProjectFamilySchemaBinding
{
    internal ProjectFamilySchemaBinding(FamilyId familyId, FamilySchemaDefinition schema, FamilyParameterSet values)
    {
        FamilyId = familyId;
        Schema = schema;
        Values = values;
    }

    public FamilyId FamilyId { get; }
    public FamilySchemaDefinition Schema { get; }
    public FamilyParameterSet Values { get; }
}

public sealed class ProjectFamilySchemaCatalog
{
    private readonly Dictionary<FamilyId, ProjectFamilySchemaBinding> _bindings = new();

    public IReadOnlyCollection<ProjectFamilySchemaBinding> Bindings => _bindings.Values;

    public ProjectFamilySchemaBinding Bind(
        SemanticProject project,
        FamilyId familyId,
        FamilySchemaDefinition schema,
        FamilyParameterSet values)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));
        if (schema is null) throw new ArgumentNullException(nameof(schema));
        if (values is null) throw new ArgumentNullException(nameof(values));
        if (!project.TryGetFamily(familyId, out var family) || family is null)
            throw new InvalidOperationException($"Family {familyId.Value:D} does not belong to project {project.Id.Value:D}.");
        if (family.Kind != schema.Kind)
            throw new InvalidOperationException($"Family kind {family.Kind} does not match schema kind {schema.Kind}.");
        var normalized = FamilySchemaValidator.ApplyDefaults(schema, values);
        FamilySchemaValidator.Validate(schema, normalized);
        var binding = new ProjectFamilySchemaBinding(familyId, schema, normalized);
        _bindings[familyId] = binding;
        return binding;
    }

    public ProjectFamilySchemaBinding Upgrade(
        SemanticProject project,
        FamilyId familyId,
        FamilySchemaDefinition targetSchema,
        FamilySchemaMigrationRegistry migrations)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));
        if (targetSchema is null) throw new ArgumentNullException(nameof(targetSchema));
        if (migrations is null) throw new ArgumentNullException(nameof(migrations));
        var current = Get(familyId);
        if (!project.TryGetFamily(familyId, out var family) || family is null)
            throw new InvalidOperationException($"Family {familyId.Value:D} does not belong to project {project.Id.Value:D}.");
        if (family.Kind != targetSchema.Kind || current.Schema.Kind != targetSchema.Kind)
            throw new InvalidOperationException("Family schema upgrade cannot change semantic family kind.");
        if (!StringComparer.Ordinal.Equals(current.Schema.SchemaId, targetSchema.SchemaId))
            throw new InvalidOperationException("Family schema upgrade cannot change schema identity.");
        var migrated = migrations.Migrate(current.Values, targetSchema);
        return Bind(project, familyId, targetSchema, migrated);
    }

    public ProjectFamilySchemaBinding Get(FamilyId familyId)
        => _bindings.TryGetValue(familyId, out var binding)
            ? binding
            : throw new KeyNotFoundException($"Family {familyId.Value:D} has no schema binding.");

    public bool Remove(FamilyId familyId) => _bindings.Remove(familyId);
}
