using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Families;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class ProjectFamilySchemaCatalogSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var invalidKind = (SemanticElementKind)int.MaxValue;
        var invalidType = (FamilyParameterType)int.MaxValue;
        var invalidDimension = (QuantityDimension)int.MaxValue;
        Throws<ArgumentOutOfRangeException>(() => new FamilyParameterDefinition("Invalid", invalidType));
        Throws<ArgumentOutOfRangeException>(() => new FamilyParameterDefinition(
            "InvalidQuantity", FamilyParameterType.Quantity, quantityDimension: invalidDimension));
        Throws<ArgumentOutOfRangeException>(() => new FamilySchemaDefinition(
            "invalid.kind", 1, invalidKind, "Invalid", Array.Empty<FamilyParameterDefinition>()));

        var project = new SemanticProject(ProjectId.New(), "Family Project");
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Basic Wall");
        project.AddFamily(family);

        var v1 = new FamilySchemaDefinition("wall.basic", 1, SemanticElementKind.Wall, "Basic Wall v1", new[]
        {
            new FamilyParameterDefinition("Width", FamilyParameterType.Quantity, true, quantityDimension: QuantityDimension.Length)
        });
        var v2 = new FamilySchemaDefinition("wall.basic", 2, SemanticElementKind.Wall, "Basic Wall v2", new[]
        {
            new FamilyParameterDefinition("Thickness", FamilyParameterType.Quantity, true, quantityDimension: QuantityDimension.Length)
        });
        var values = new FamilyParameterSet("wall.basic", 1, new[]
        {
            new KeyValuePair<string, FamilyParameterValue>("Width", FamilyParameterValue.FromQuantity(new QuantityValue(QuantityDimension.Length, 0.2d)))
        });
        var catalog = new ProjectFamilySchemaCatalog();
        var bound = catalog.Bind(project, family.Id, v1, values);
        if (bound.FamilyId != family.Id || bound.Values.SchemaVersion != 1) throw new InvalidOperationException("Family binding mismatch.");

        var upgraded = catalog.Upgrade(project, family.Id, v2, new FamilySchemaMigrationRegistry(new[]
        {
            new RenameFamilyParameterStep("wall.basic", 1, "Width", "Thickness")
        }));
        if (upgraded.Values.SchemaVersion != 2 || upgraded.Values.Values["Thickness"].Quantity.Value != 0.2d)
            throw new InvalidOperationException("Family schema upgrade mismatch.");

        var beamSchema = new FamilySchemaDefinition("beam.basic", 1, SemanticElementKind.Beam, "Beam", Array.Empty<FamilyParameterDefinition>());
        Throws<InvalidOperationException>(() => catalog.Bind(project, family.Id, beamSchema, new FamilyParameterSet("beam.basic", 1)));
        Throws<InvalidOperationException>(() => catalog.Bind(project, FamilyId.New(), v1, values));
        Console.WriteLine("PASS semantic project family schema binding");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
