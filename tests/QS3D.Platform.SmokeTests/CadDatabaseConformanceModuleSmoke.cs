using System.Runtime.CompilerServices;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Geometry;
using QS3D.Platform.InMemory;

namespace QS3D.Platform.SmokeTests;

internal static class CadDatabaseConformanceModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        RunDatabaseConformance(static () => new InMemoryCadDatabase());
        Console.WriteLine("PASS CAD database conformance module");
    }

    internal static void RunDatabaseConformance(Func<ICadDatabase> factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));
        CommitAndRollback(factory());
        StableHandleUndoRedo(factory());
        StaleTransactionFailsClosed(factory());

        var capabilityProbe = factory();
        if ((capabilityProbe.Capabilities & CadCapabilities.Layers) != 0)
            LayerContract(factory());
        if ((capabilityProbe.Capabilities & CadCapabilities.Blocks) != 0)
            BlockContract(factory());
    }

    private static void CommitAndRollback(ICadDatabase database)
    {
        using (var tx = database.BeginTransaction())
            tx.Append(Line(0, 0, 1, 0));
        Equal(0, Count(database), "uncommitted append must roll back");

        using (var tx = database.BeginTransaction())
        {
            tx.Append(Line(0, 0, 1, 0));
            tx.Commit();
        }
        Equal(1, Count(database), "committed append must publish exactly once");
    }

    private static void StableHandleUndoRedo(ICadDatabase database)
    {
        CadHandle handle;
        using (var tx = database.BeginTransaction())
        {
            handle = tx.Append(Line(0, 0, 10, 0));
            tx.Commit();
        }
        Require(database.History.CanUndo, "commit must expose undo");
        database.History.Undo();
        Equal(0, Count(database), "undo must restore previous entity set");
        Require(database.History.CanRedo, "undo must expose redo");
        database.History.Redo();
        using var read = database.BeginTransaction(CadTransactionMode.ReadOnly);
        Require(read.Get(handle) is not null, "redo must restore the original stable handle");
    }

    private static void StaleTransactionFailsClosed(ICadDatabase database)
    {
        using var first = database.BeginTransaction();
        using var stale = database.BeginTransaction();
        first.Append(Line(0, 0, 1, 0));
        first.Commit();
        stale.Append(Line(2, 0, 3, 0));
        Throws<InvalidOperationException>(stale.Commit, "stale transaction must fail closed");
    }

    private static void LayerContract(ICadDatabase database)
    {
        CadHandle handle;
        using (var tx = database.BeginTransaction())
        {
            tx.CreateLayer("A-CONFORMANCE");
            tx.SetCurrentLayer("A-CONFORMANCE");
            handle = tx.Append(Line(0, 0, 2, 0));
            tx.Commit();
        }
        using (var read = database.BeginTransaction(CadTransactionMode.ReadOnly))
        {
            Equal("A-CONFORMANCE", read.CurrentLayerName, "current layer must persist");
            Equal("A-CONFORMANCE", read.Get(handle)!.LayerName, "new entity must own current layer");
        }
        using (var tx = database.BeginTransaction())
        {
            tx.SetCurrentLayer("0");
            tx.UpdateLayer(tx.GetLayer("A-CONFORMANCE")! with { IsLocked = true });
            tx.Commit();
        }
        using var blocked = database.BeginTransaction();
        Throws<InvalidOperationException>(() => blocked.Erase(handle), "locked layer must reject entity mutation");
    }

    private static void BlockContract(ICadDatabase database)
    {
        using (var tx = database.BeginTransaction())
        {
            tx.CreateBlock("CONFORMANCE", new Point3(0, 0), new[] { Line(0, 0, 1, 0) });
            tx.Commit();
        }
        CadHandle reference;
        using (var tx = database.BeginTransaction())
        {
            reference = tx.InsertBlock("CONFORMANCE", new Point3(10, 20), 2d, 0d);
            tx.Commit();
        }
        using (var read = database.BeginTransaction(CadTransactionMode.ReadOnly))
        {
            Equal(CadEntityKind.BlockReference, read.Get(reference)!.Kind, "INSERT must produce a block reference");
            Equal(1, read.GetBlocks().Count, "definition must remain available");
        }
        using var blocked = database.BeginTransaction();
        Throws<InvalidOperationException>(() => blocked.EraseBlock("CONFORMANCE"), "referenced block definition must not be erased");
    }

    private static CadEntityDraft Line(double x1, double y1, double x2, double y2)
        => new(CadEntityKind.Line, BoundingBox3.FromPoints(new Point3(x1, y1), new Point3(x2, y2)));

    private static int Count(ICadDatabase database)
    {
        using var read = database.BeginTransaction(CadTransactionMode.ReadOnly);
        return read.Query().Count;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual, string message) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"{message}: expected {typeof(T).Name}.");
    }
}
