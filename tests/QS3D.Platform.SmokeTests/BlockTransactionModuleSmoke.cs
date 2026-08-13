using System.Runtime.CompilerServices;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Geometry;
using QS3D.Platform.InMemory;

namespace QS3D.Platform.SmokeTests;

internal static class BlockTransactionModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var database = new InMemoryCadDatabase();
        using (var tx = database.BeginTransaction())
        {
            tx.CreateLayer("A-BLOCK");
            tx.CreateBlock(
                "Door",
                new Point3(0, 0),
                new[]
                {
                    new CadEntityDraft(
                        CadEntityKind.Line,
                        BoundingBox3.FromPoints(new Point3(0, 0), new Point3(10, 0)),
                        null,
                        "A-BLOCK")
                });
            tx.Commit();
        }

        CadHandle reference;
        using (var tx = database.BeginTransaction())
        {
            reference = tx.InsertBlock("door", new Point3(20, 5), 2d, 0d);
            tx.Commit();
        }

        using (var read = database.BeginTransaction(CadTransactionMode.ReadOnly))
        {
            Require(read.GetBlocks().Count == 1, "block definition must be committed");
            var entity = read.Get(reference) ?? throw new InvalidOperationException("inserted block reference is missing");
            Require(entity.Kind == CadEntityKind.BlockReference, "insert must create a block-reference entity");
            Require(entity.Extents.Min.Equals(new Point3(20, 5)), "inserted block minimum extents mismatch");
            Require(entity.Extents.Max.Equals(new Point3(40, 5)), "inserted block maximum extents mismatch");
        }

        using (var tx = database.BeginTransaction())
            Throws<InvalidOperationException>(() => tx.EraseBlock("Door"));

        database.History.Undo();
        using (var read = database.BeginTransaction(CadTransactionMode.ReadOnly))
        {
            Require(read.Query().Count == 0, "undo insert must remove the block reference");
            Require(read.GetBlock("DOOR") is not null, "undo insert must preserve the block definition");
        }

        using (var tx = database.BeginTransaction())
        {
            tx.EraseBlock("door");
            tx.Commit();
        }
        using (var read = database.BeginTransaction(CadTransactionMode.ReadOnly))
            Require(read.GetBlocks().Count == 0, "block delete must remove the definition");

        database.History.Undo();
        using (var read = database.BeginTransaction(CadTransactionMode.ReadOnly))
            Require(read.GetBlock("Door") is not null, "undo block delete must restore the definition");

        Console.WriteLine("PASS transactional block definitions");
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
