using System.Collections;
using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;
using QS3D.Platform.Quantity;

namespace QS3D.Platform.SmokeTests;

internal static class QuantityScheduleProjectRowAdmissionModuleSmoke
{
    private const int MaximumEntries = 100_000;
    private const long MaximumRejectedRequestAllocationBytes = 1_000_000;

    [ModuleInitializer]
    internal static void Run()
    {
        WarmProjectionPath();

        var oversized = CreateProject(MaximumEntries + 1, "Oversized include-empty schedule");
        var hostileFacts = new ThrowIfEnumeratedFacts();
        var allocatedBeforeReject = GC.GetAllocatedBytesForCurrentThread();
        ExpectInvalidOperation(() => QuantityScheduleProjector.Project(
            oversized,
            hostileFacts,
            includeElementsWithoutQuantities: true));
        var rejectedRequestAllocation = GC.GetAllocatedBytesForCurrentThread() - allocatedBeforeReject;
        if (hostileFacts.WasEnumerated)
            throw new InvalidOperationException("Oversized include-empty schedule touched facts before rejecting impossible row cardinality.");
        if (rejectedRequestAllocation > MaximumRejectedRequestAllocationBytes)
            throw new InvalidOperationException(
                $"Oversized include-empty schedule allocated {rejectedRequestAllocation} bytes before rejecting known-impossible row cardinality; " +
                $"expected no allocation proportional to {MaximumEntries + 1} project elements.");

        var sparseFromOversizedProject = QuantityScheduleProjector.Project(
            oversized,
            Array.Empty<QuantityFact>(),
            includeElementsWithoutQuantities: false);
        if (sparseFromOversizedProject.Rows.Count != 0)
            throw new InvalidOperationException("Large project with no facts must remain admissible when empty elements are excluded.");

        var exact = CreateProject(MaximumEntries, "Exact-limit include-empty schedule");
        var exactSchedule = QuantityScheduleProjector.Project(
            exact,
            Array.Empty<QuantityFact>(),
            includeElementsWithoutQuantities: true);
        if (exactSchedule.Rows.Count != MaximumEntries)
            throw new InvalidOperationException($"Exact row ceiling expected {MaximumEntries} rows, got {exactSchedule.Rows.Count}.");

        Console.WriteLine("PASS quantity schedule project-row admission boundary");
    }

    private static void WarmProjectionPath()
    {
        var warm = CreateProject(1, "Warm include-empty schedule");
        var schedule = QuantityScheduleProjector.Project(
            warm,
            Array.Empty<QuantityFact>(),
            includeElementsWithoutQuantities: true);
        if (schedule.Rows.Count != 1)
            throw new InvalidOperationException("Projection warm-up did not preserve the admitted include-empty row.");
    }

    private static SemanticProject CreateProject(int elementCount, string name)
    {
        var familyId = new FamilyId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        var project = new SemanticProject(
            new ProjectId(Guid.Parse("11111111-2222-3333-4444-555555555555")),
            name);
        project.AddFamily(new Family(familyId, SemanticElementKind.Wall, "Wall family"));
        for (var index = 0; index < elementCount; index++)
        {
            var elementId = new ElementId(new Guid(index + 1, 0, 0, new byte[8]));
            project.AddElement(new SemanticElement(elementId, SemanticElementKind.Wall, "W", familyId));
        }
        return project;
    }

    private static void ExpectInvalidOperation(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("Expected impossible include-empty schedule to fail with InvalidOperationException.");
    }

    private sealed class ThrowIfEnumeratedFacts : IEnumerable<QuantityFact>
    {
        public bool WasEnumerated { get; private set; }

        public IEnumerator<QuantityFact> GetEnumerator()
        {
            WasEnumerated = true;
            throw new FactsEnumerationException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class FactsEnumerationException : Exception
    {
    }
}
