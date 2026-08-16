using QS3D.Platform.Application;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Diagnostics;
using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;
using QS3D.Platform.InMemory;
using QS3D.Platform.Quantity;

var tests = new (string Name, Action Run)[]
{
    ("finite numeric policy", FiniteNumericPolicy),
    ("CAD handle canonicality", CadHandleCanonicality),
    ("semantic generated-reference identity", SemanticReferenceIdentity),
    ("semantic family kind invariant", SemanticFamilyKindInvariant),
    ("semantic health references", SemanticHealthReferences),
    ("quantity accumulation", QuantityAccumulation),
    ("Cubicost-style shared parity", CubicostSharedParitySmoke.Run),
    ("TBQ 360-degree price/reference check", TbqPriceReferenceParitySmoke.Run),
    ("TBQ element-code cost analysis", TbqElementAnalysisParitySmoke.Run),
    ("TBQ Build-up Analysis", TbqBuildUpAnalysisParitySmoke.Run),
    ("transaction rollback and commit", TransactionRollbackAndCommit),
    ("transactional layer ownership", TransactionalLayerOwnership),
    ("stale transaction fails closed", StaleTransactionFailsClosed),
    ("undo redo preserves drawing state", UndoRedoPreservesDrawingState),
    ("command registry", CommandRegistryContract)
};

foreach (var test in tests)
{
    test.Run();
    Console.WriteLine($"PASS {test.Name}");
}

static void FiniteNumericPolicy()
{
    Throws<ArgumentOutOfRangeException>(() => _ = new Point3(double.NaN, 0));
    Equal(5d, new Point3(0, 0).DistanceTo(new Point3(3, 4)));
}

static void CadHandleCanonicality()
{
    Equal(new CadHandle("A"), new CadHandle("000a"));
    Throws<FormatException>(() => _ = new CadHandle("not-a-handle"));
}

static void SemanticReferenceIdentity()
{
    var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "200 Wall");
    var project = new SemanticProject(ProjectId.New(), "Demo");
    project.AddFamily(family);
    var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
    project.AddElement(element);
    var drawing = DrawingId.New();
    Require(element.AddGeneratedReference(new CadReference(drawing, new CadHandle("A"))), "first reference must be added");
    Require(!element.AddGeneratedReference(new CadReference(drawing, new CadHandle("000a"))), "canonical alias must deduplicate");
}

static void SemanticFamilyKindInvariant()
{
    var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall");
    var project = new SemanticProject(ProjectId.New(), "Demo");
    project.AddFamily(family);
    var incompatible = new SemanticElement(ElementId.New(), SemanticElementKind.Beam, "B1", family.Id);
    Throws<InvalidOperationException>(() => project.AddElement(incompatible));
}

static void SemanticHealthReferences()
{
    var family = new Family(FamilyId.New(), SemanticElementKind.Wall, "Wall");
    var project = new SemanticProject(ProjectId.New(), "Health");
    project.AddFamily(family);
    var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "W1", family.Id);
    element.AssignLocation(FloorId.New(), ZoneId.New());
    project.AddElement(element);
    var report = SemanticHealthAnalyzer.Analyze(project);
    Require(!report.IsReady, "missing floor and zone references must block readiness");
    Equal(2, report.ErrorCount);
    Equal(1, report.WarningCount);
}

static void QuantityAccumulation()
{
    var first = ElementId.New();
    var second = ElementId.New();
    var facts = new[]
    {
        new QuantityFact(first, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 1.25)),
        new QuantityFact(first, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 2.75)),
        new QuantityFact(second, "WALL.LENGTH", new QuantityValue(QuantityDimension.Length, 3.0)),
        new QuantityFact(second, "WALL.AREA", new QuantityValue(QuantityDimension.Area, 12.5))
    };
    var summaries = QuantityAccumulator.Summarize(facts);
    Equal(2, summaries.Count);
    var length = summaries.Single(static x => x.Code == "WALL.LENGTH");
    Equal(7d, length.Quantity.Value);
    Equal(3, length.FactCount);
    Equal(2, length.ElementCount);
    Equal("m", length.Quantity.CanonicalUnit);
    Throws<ArgumentOutOfRangeException>(() => _ = new QuantityValue(QuantityDimension.Volume, -1));
}

static void TransactionRollbackAndCommit()
{
    var database = new InMemoryCadDatabase();
    using (var tx = database.BeginTransaction()) tx.Append(LineDraft());
    Equal(0, EntityCount(database));
    using (var tx = database.BeginTransaction())
    {
        tx.Append(LineDraft());
        tx.Commit();
    }
    Equal(1, EntityCount(database));
}

static void TransactionalLayerOwnership()
{
    var database = new InMemoryCadDatabase();
    CadHandle handle;
    using (var tx = database.BeginTransaction())
    {
        tx.CreateLayer("A-WALL");
        tx.SetCurrentLayer("A-WALL");
        handle = tx.Append(LineDraft());
        tx.Commit();
    }
    using (var read = database.BeginTransaction(CadTransactionMode.ReadOnly))
    {
        Equal("A-WALL", read.CurrentLayerName);
        Equal("A-WALL", read.Get(handle)!.LayerName);
        Equal(2, read.GetLayers().Count);
    }
    database.History.Undo();
    using (var read = database.BeginTransaction(CadTransactionMode.ReadOnly))
    {
        Equal("0", read.CurrentLayerName);
        Equal(0, read.Query().Count);
        Equal(1, read.GetLayers().Count);
    }
    database.History.Redo();
    using (var tx = database.BeginTransaction())
    {
        tx.SetCurrentLayer("0");
        var wallLayer = tx.GetLayer("A-WALL")!;
        tx.UpdateLayer(wallLayer with { IsLocked = true });
        tx.Commit();
    }
    using (var tx = database.BeginTransaction())
        Throws<InvalidOperationException>(() => tx.Append(LineDraft() with { LayerName = "A-WALL" }));
}

static void StaleTransactionFailsClosed()
{
    var database = new InMemoryCadDatabase();
    using var first = database.BeginTransaction();
    using var stale = database.BeginTransaction();
    first.Append(PointDraft(0, 0));
    first.Commit();
    stale.Append(PointDraft(1, 1));
    Throws<InvalidOperationException>(stale.Commit);
}

static void UndoRedoPreservesDrawingState()
{
    var database = new InMemoryCadDatabase();
    CadHandle handle;
    using (var tx = database.BeginTransaction())
    {
        handle = tx.Append(LineDraft());
        tx.Commit();
    }
    Require(database.History.CanUndo, "commit must create undo history");
    database.History.Undo();
    Equal(0, EntityCount(database));
    Require(database.History.CanRedo, "undo must create redo history");
    database.History.Redo();
    Equal(1, EntityCount(database));
    using var read = database.BeginTransaction(CadTransactionMode.ReadOnly);
    Require(read.Get(handle) is not null, "redo must preserve the original stable handle");
}

static void CommandRegistryContract()
{
    var manager = new InMemoryDocumentManager();
    var document = manager.CreateNew("Untitled");
    var registry = new CommandRegistry();
    registry.Register(new PingCommand());
    Require(registry.Execute("ping", new CommandContext(document)).Succeeded, "case-insensitive command should execute");
    Equal("PONG", ((InMemoryEditor)document.Editor).Messages.Single());
    Throws<InvalidOperationException>(() => registry.Register(new PingCommand()));
}

static CadEntityDraft LineDraft() => new(CadEntityKind.Line, BoundingBox3.FromPoints(new Point3(0, 0), new Point3(10, 0)));
static CadEntityDraft PointDraft(double x, double y) => new(CadEntityKind.Point, new BoundingBox3(new Point3(x, y), new Point3(x, y)));

static int EntityCount(ICadDatabase database)
{
    using var read = database.BeginTransaction(CadTransactionMode.ReadOnly);
    return read.Query().Count;
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Equal<T>(T expected, T actual) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected} but got {actual}.");
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

sealed class PingCommand : ICadCommand
{
    public string Name => "PING";
    public CommandFlags Flags => CommandFlags.RequiresDocument | CommandFlags.ReadOnly;
    public CommandResult Execute(CommandContext context)
    {
        context.Document.Editor.WriteMessage("PONG");
        return CommandResult.Success();
    }
}
