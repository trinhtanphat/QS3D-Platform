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

        var staleSource = new CadReference(DrawingId.New(), new CadHandle("20"));
        var fact = new QuantityFact(
            element.Id,
            "WALL.LENGTH",
            new QuantityValue(QuantityDimension.Length, 1d),
            staleSource);

        try
        {
            QuantityScheduleProjector.Project(project, new[] { fact });
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("PASS stale quantity fact CAD provenance rejected");
            return;
        }

        throw new InvalidOperationException("Quantity schedule accepted a fact whose CAD source reference does not match the current semantic element.");
    }
}
