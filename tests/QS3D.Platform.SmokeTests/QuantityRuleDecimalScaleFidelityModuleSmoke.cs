using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityRuleDecimalScaleFidelityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var familyId = new FamilyId(Guid.Parse("6357d3ac-d95c-4d68-afab-81d1b100a48a"));
        var element = new SemanticElement(
            new ElementId(Guid.Parse("b789efc2-23bf-4d05-b404-a923c14b5251")),
            SemanticElementKind.Wall,
            "Exact decimal-scale wall",
            familyId);

        Set(element, "MM", 69789978031.23123d);
        Set(element, "CM", 3.75015129991706e182d);
        Set(element, "MM2", 6.35142695953089e39d);
        Set(element, "CM2", 1.1012848168869247e-144d);
        Set(element, "MM3", 2.7402992211211728e-154d);
        Set(element, "CM3", 5.862813159411555e86d);
        Set(element, "G", 1.836449551960462e85d);
        Set(element, "T", 1.23456789012345d);

        var project = new SemanticProject(
            new ProjectId(Guid.Parse("3047bf5a-ea56-41c7-b35f-577b11d525ab")),
            "Quantity rule decimal scale fidelity smoke");
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        project.AddElement(element);

        AssertRuleBits(project, "MM", QuantityUnit.Millimeter, QuantityDimension.Length, 0x4190A3A4681FFB14L);
        AssertRuleBits(project, "CM", QuantityUnit.Centimeter, QuantityDimension.Length, 0x656CEB9290DB946FL);
        AssertRuleBits(project, "MM2", QuantityUnit.SquareMillimeter, QuantityDimension.Area, 0x46F3926475AF877BL);
        AssertRuleBits(project, "CM2", QuantityUnit.SquareCentimeter, QuantityDimension.Area, 0x213687E587B483D1L);
        AssertRuleBits(project, "MM3", QuantityUnit.CubicMillimeter, QuantityDimension.Volume, 0x1E2F8F854237445EL);
        AssertRuleBits(project, "CM3", QuantityUnit.CubicCentimeter, QuantityDimension.Volume, 0x50B3C7396971347FL);
        AssertRuleBits(project, "G", QuantityUnit.Gram, QuantityDimension.Mass, 0x51035C3707A70B93L);

        var tonneRaw = double.Parse(element.Properties["T"], NumberStyles.Float, CultureInfo.InvariantCulture);
        var tonneExpected = QuantityUnits.ToCanonical(tonneRaw, QuantityUnit.Tonne);
        AssertRuleBits(
            project,
            "T",
            QuantityUnit.Tonne,
            QuantityDimension.Mass,
            BitConverter.DoubleToInt64Bits(tonneExpected));

        Console.WriteLine("PASS quantity rule exact decimal-scale fidelity");
    }

    private static void Set(SemanticElement element, string propertyName, double value)
        => element.SetProperty(propertyName, value.ToString("R", CultureInfo.InvariantCulture));

    private static void AssertRuleBits(
        SemanticProject project,
        string propertyName,
        QuantityUnit unit,
        QuantityDimension dimension,
        long expectedBits)
    {
        var rule = new QuantityRuleDefinition(
            SemanticElementKind.Wall,
            "SCALE." + propertyName,
            dimension,
            new[] { new QuantityFactor(propertyName, unit) });
        var facts = QuantityRuleEngine.Evaluate(project, new QuantityRuleCatalog(new[] { rule }));
        if (facts.Count != 1)
            throw new InvalidOperationException($"Expected one quantity fact for {propertyName}, got {facts.Count}.");

        var actual = facts[0].Quantity.Value;
        var actualBits = BitConverter.DoubleToInt64Bits(actual);
        if (actualBits != expectedBits)
            throw new InvalidOperationException(
                $"Rule unit scaling for {unit} rounded to {actual:R} (0x{actualBits:X16}); expected exact decimal-scale bits 0x{expectedBits:X16}.");

        var raw = double.Parse(project.Elements.Single().Properties[propertyName], NumberStyles.Float, CultureInfo.InvariantCulture);
        var standalone = QuantityUnits.ToCanonical(raw, unit);
        if (BitConverter.DoubleToInt64Bits(standalone) != expectedBits)
            throw new InvalidOperationException($"Standalone QuantityUnits oracle for {unit} no longer matches the pinned exact-scale fixture.");
    }
}
