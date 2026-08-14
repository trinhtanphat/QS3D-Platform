using System.Runtime.CompilerServices;
using QS3D.Platform.Diagnostics;
using QS3D.Platform.Domain;
using QS3D.Platform.Parity;
using QS3D.Platform.Persistence;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class ParityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var projectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var floorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var zoneId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var familyId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var elementId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var drawingId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var snapshot = new SemanticProjectSnapshot(
            1, projectId, "Golden",
            new[] { new FloorSnapshot(floorId, "Ground", 0d) },
            new[] { new ZoneSnapshot(zoneId, "A") },
            new[] { new FamilySnapshot(familyId, SemanticElementKind.Wall, "Wall") },
            new[]
            {
                new ElementSnapshot(elementId, SemanticElementKind.Wall, "Wall A", familyId, floorId, zoneId,
                    new CadReferenceSnapshot(drawingId, "000a"), null,
                    new Dictionary<string, string> { ["LengthMm"] = "2500", ["HeightMm"] = "3000" })
            });
        var rules = new[]
        {
            new QuantityRuleDefinition(SemanticElementKind.Wall, "WALL.AREA", QuantityDimension.Area, new[]
            {
                new QuantityFactor("LengthMm", QuantityUnit.Millimeter),
                new QuantityFactor("HeightMm", QuantityUnit.Millimeter)
            })
        };
        var result = GoldenParityRunner.Run(new GoldenParityFixture("wall-area", snapshot, rules,
            expectedQuantities: new[] { new GoldenQuantityExpectation(elementId, "WALL.AREA", QuantityDimension.Area, 7.5d) }));
        if (!result.Passed) throw new InvalidOperationException(string.Join("; ", result.Failures));
        if (result.Project.Elements.Single().SourceReference!.Value.Handle.Value != "A")
            throw new InvalidOperationException("Golden parity must preserve canonical CAD handle identity.");
        var mismatch = GoldenParityRunner.Run(new GoldenParityFixture("wall-area-mismatch", snapshot, rules,
            expectedQuantities: new[] { new GoldenQuantityExpectation(elementId, "WALL.AREA", QuantityDimension.Area, 8d) }));
        if (mismatch.Passed) throw new InvalidOperationException("Golden parity must reject an incorrect quantity expectation.");

        Throws<ArgumentOutOfRangeException>(() => new GoldenDiagnosticExpectation("BAD", (DiagnosticSeverity)999));
        Throws<ArgumentOutOfRangeException>(() => new GoldenQuantityExpectation(elementId, "BAD", (QuantityDimension)999, 1d));

        Console.WriteLine("PASS cross-product golden parity runner");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
