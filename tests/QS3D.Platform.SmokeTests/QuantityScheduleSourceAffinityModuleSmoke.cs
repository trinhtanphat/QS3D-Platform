using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleSourceAffinityModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var project = new SemanticProject(ProjectId.New(), "P");
        var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall");
        project.AddFamily(family);

        var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
        var currentSource = new CadReference(DrawingId.New(), new CadHandle("10"));
        element.SetSource(currentSource);
        project.AddElement(element);

        ExpectRejected(
            project,
            new QuantityFact(element.Id, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d), new CadReference(DrawingId.New(), new CadHandle("20"))),
            "mismatched CAD source");
        ExpectRejected(
            project,
            new QuantityFact(element.Id, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d)),
            "missing fact provenance");

        var matching = new QuantityFact(element.Id, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 2d), currentSource);
        var schedule = QuantityScheduleProjector.Project(project, new[] { matching });
        if (schedule.Rows.Count != 1 || schedule.Rows[0].Quantities.Count != 1 || schedule.Rows[0].Quantities[0].Quantity.Value != 2d)
            throw new InvalidOperationException("Quantity schedule rejected or corrupted matching CAD provenance.");

        var noSourceElement = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W2", family.Id);
        project.AddElement(noSourceElement);
        var noSourceFact = new QuantityFact(noSourceElement.Id, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 3d));
        var nullAffinitySchedule = QuantityScheduleProjector.Project(project, new[] { noSourceFact });
        if (nullAffinitySchedule.Rows.Count != 1 || nullAffinitySchedule.Rows[0].ElementId != noSourceElement.Id)
            throw new InvalidOperationException("Quantity schedule rejected matching null/null provenance.");

        ExpectRejected(
            project,
            new QuantityFact(noSourceElement.Id, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1d), currentSource),
            "unexpected fact provenance");

        Console.WriteLine("PASS quantity schedule CAD provenance affinity enforced");
    }

    private static void ExpectRejected(SemanticProject project, QuantityFact fact, string scenario)
    {
        try
        {
            QuantityScheduleProjector.Project(project, new[] { fact });
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException($"Quantity schedule accepted {scenario}.");
    }
}
