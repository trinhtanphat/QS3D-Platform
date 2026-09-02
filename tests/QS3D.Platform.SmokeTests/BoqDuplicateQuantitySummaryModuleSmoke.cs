using System.Runtime.CompilerServices;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class BoqDuplicateQuantitySummaryModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var summaries = new[]
        {
            new QuantitySummary("VOL", QuantityDimension.Volume, 2d, factCount: 1, elementCount: 1),
            new QuantitySummary("VOL", QuantityDimension.Volume, 3d, factCount: 1, elementCount: 1)
        };
        var rates = new[]
        {
            new UnitRate("VOL", QuantityDimension.Volume, 10m, "USD")
        };

        try
        {
            var projection = BoqProjector.Project(summaries, rates, "USD");
            throw new InvalidOperationException(
                $"Duplicate aggregate quantity key was accepted as {projection.Lines.Count} BOQ lines totaling {projection.Total.Amount} USD.");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Duplicate quantity summary for", StringComparison.Ordinal))
        {
        }

        Console.WriteLine("PASS BOQ duplicate quantity summary safety");
    }
}
