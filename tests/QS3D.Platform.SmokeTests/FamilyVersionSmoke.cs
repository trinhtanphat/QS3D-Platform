using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Families;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class FamilyVersionSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var target = new FamilySchemaDefinition("wall.basic", 3, SemanticElementKind.Wall, "Basic Wall", new[]
        {
            new FamilyParameterDefinition("Thickness", FamilyParameterType.Quantity, true, quantityDimension: QuantityDimension.Length, minimum: 0.05d, maximum: 1d),
            new FamilyParameterDefinition("IsStructural", FamilyParameterType.Boolean, true, FamilyParameterValue.FromBoolean(false))
        });
        var source = new FamilyParameterSet("wall.basic", 1, new[]
        {
            new KeyValuePair<string, FamilyParameterValue>("Width", FamilyParameterValue.FromQuantity(new QuantityValue(QuantityDimension.Length, 0.2d)))
        });
        var chain = new FamilySchemaMigrationRegistry(new IFamilySchemaMigration[]
        {
            new RenameFamilyParameterStep("wall.basic", 1, "Width", "Thickness"),
            new AddFamilyParameterStep("wall.basic", 2, "IsStructural", FamilyParameterValue.FromBoolean(false))
        });
        var result = chain.Migrate(source, target);
        if (result.SchemaVersion != 3 || result.Values["Thickness"].Quantity.Value != 0.2d || result.Values["IsStructural"].Boolean)
            throw new InvalidOperationException("Family version smoke failed.");
        FamilySchemaValidator.Validate(target, result);
        try
        {
            new FamilySchemaMigrationRegistry().Migrate(source, target);
            throw new InvalidOperationException("Missing family step was accepted.");
        }
        catch (InvalidOperationException ex) when (!ex.Message.Contains("accepted", StringComparison.Ordinal)) { }
        Console.WriteLine("PASS family version schema");
    }
}
