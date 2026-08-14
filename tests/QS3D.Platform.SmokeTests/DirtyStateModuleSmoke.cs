using System.Runtime.CompilerServices;
using QS3D.Platform.Application;

namespace QS3D.Platform.SmokeTests;

internal static class DirtyStateModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var graph = new DependencyGraph();
        graph.AddDependency("opening", "wall");
        graph.AddDependency("quantity", "opening");

        var tracker = new DirtyStateTracker();
        var impacted = tracker.MarkImpact(graph, new[] { "wall" }, DirtyReason.SourceGeometryChanged);
        Require(impacted.Count == 3, "wall change must dirty downstream opening and quantity");
        Require(tracker.Get("wall").Reasons == DirtyReason.SourceGeometryChanged, "root reason must be preserved");
        Require(tracker.Get("opening").Reasons == DirtyReason.DependencyChanged, "dependent must carry dependency reason");

        var openingRevision = tracker.Get("opening").DirtyRevision;
        tracker.MarkDirty("opening", DirtyReason.ManualInvalidation);
        Throws<InvalidOperationException>(() => tracker.MarkClean("opening", openingRevision));

        var freshRevision = tracker.Get("opening").DirtyRevision;
        var clean = tracker.MarkClean("opening", freshRevision);
        Require(!clean.IsDirty, "fresh regeneration must clear dirty state");
        Require(clean.Reasons == DirtyReason.None, "clean state must clear reasons");
        Require(tracker.GetDirty().Select(static x => x.NodeId).SequenceEqual(new[] { "quantity", "wall" }, StringComparer.Ordinal), "remaining dirty nodes must be deterministic");

        var combinedTracker = new DirtyStateTracker();
        var combined = combinedTracker.MarkDirty("combined", DirtyReason.DirectMutation | DirtyReason.RuleChanged);
        Require(combined.Reasons == (DirtyReason.DirectMutation | DirtyReason.RuleChanged), "defined dirty flags must compose");
        Throws<ArgumentOutOfRangeException>(() => combinedTracker.MarkDirty("bad", (DirtyReason)(1 << 10)));
        Throws<ArgumentOutOfRangeException>(() => combinedTracker.MarkImpact(graph, new[] { "wall" }, (DirtyReason)(1 << 10)));

        Console.WriteLine("PASS dirty freshness module");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
