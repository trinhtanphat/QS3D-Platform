using System.Runtime.CompilerServices;
using QS3D.Platform.Application;

namespace QS3D.Platform.SmokeTests;

internal static class DependencyPlanningModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var graph = new DependencyGraph();
        graph.AddDependency("opening", "wall");
        graph.AddDependency("finish", "wall");
        graph.AddDependency("quantity", "opening");
        graph.AddDependency("quantity", "finish");
        graph.AddDependency("schedule", "quantity");

        var plan = graph.PlanImpact(new[] { "wall" }).OrderedNodeIds;
        Require(plan.Count == 6, "wall impact must include all downstream nodes");
        Require(plan[0] == "wall", "source node must plan first");
        Require(Index(plan, "opening") < Index(plan, "quantity"), "opening must precede quantity");
        Require(Index(plan, "finish") < Index(plan, "quantity"), "finish must precede quantity");
        Require(Index(plan, "quantity") < Index(plan, "schedule"), "quantity must precede schedule");

        var deterministic = graph.PlanImpact(new[] { "wall" }).OrderedNodeIds;
        Require(plan.SequenceEqual(deterministic, StringComparer.Ordinal), "impact plan must be deterministic");

        var cyclic = new DependencyGraph();
        cyclic.AddDependency("A", "B");
        cyclic.AddDependency("B", "C");
        cyclic.AddDependency("C", "A");
        Throws<InvalidOperationException>(cyclic.ValidateAcyclic);

        Console.WriteLine("PASS dependency impact planning module");
    }

    private static int Index(IReadOnlyList<string> items, string value)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (StringComparer.Ordinal.Equals(items[i], value)) return i;
        }
        return -1;
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
