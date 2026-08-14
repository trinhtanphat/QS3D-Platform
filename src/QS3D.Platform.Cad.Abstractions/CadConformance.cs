using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.Cad.Abstractions;

public enum CadConformanceSeverity
{
    Info = 0,
    Warning,
    Error
}

public sealed class CadConformanceFinding
{
    public CadConformanceFinding(string code, CadConformanceSeverity severity, string message)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Conformance code must not be blank.", nameof(code));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Conformance message must not be blank.", nameof(message));
        Code = code.Trim();
        Severity = severity;
        Message = message.Trim();
    }

    public string Code { get; }
    public CadConformanceSeverity Severity { get; }
    public string Message { get; }
}

public sealed class CadConformanceReport
{
    public CadConformanceReport(IEnumerable<CadConformanceFinding> findings)
    {
        if (findings is null) throw new ArgumentNullException(nameof(findings));
        Findings = findings.OrderByDescending(static finding => finding.Severity)
            .ThenBy(static finding => finding.Code, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<CadConformanceFinding> Findings { get; }
    public bool Passed => Findings.All(static finding => finding.Severity != CadConformanceSeverity.Error);
    public int ErrorCount => Findings.Count(static finding => finding.Severity == CadConformanceSeverity.Error);
}

public interface ICadConformanceFixture : IDisposable
{
    ICadDocument CreateIsolatedDocument(string name);
}

public static class CadAdapterConformance
{
    public static CadConformanceReport Run(ICadConformanceFixture fixture)
    {
        if (fixture is null) throw new ArgumentNullException(nameof(fixture));
        var findings = new List<CadConformanceFinding>();
        RunCase("CAD_TX_ROLLBACK_COMMIT", findings, () => TransactionRollbackCommit(fixture));
        RunCase("CAD_STALE_WRITE_FAIL_CLOSED", findings, () => StaleWriteFailsClosed(fixture));
        RunCase("CAD_UNDO_REDO_STABLE_HANDLE", findings, () => UndoRedoStableHandle(fixture));
        RunCase("CAD_LAYER_LOCK_INTEGRITY", findings, () => LayerLockIntegrity(fixture));
        RunCase("CAD_BLOCK_REFERENCE_INTEGRITY", findings, () => BlockReferenceIntegrity(fixture));
        RunCase("CAD_EDITOR_SELECTION", findings, () => EditorSelection(fixture));
        return new CadConformanceReport(findings);
    }

    private static void TransactionRollbackCommit(ICadConformanceFixture fixture)
    {
        var document = fixture.CreateIsolatedDocument("conformance-transaction");
        using (var tx = document.Database.BeginTransaction())
        {
            tx.Append(LineDraft());
        }
        Require(EntityCount(document.Database) == 0, "Uncommitted transaction leaked an entity.");
        using (var tx = document.Database.BeginTransaction())
        {
            tx.Append(LineDraft());
            tx.Commit();
        }
        Require(EntityCount(document.Database) == 1, "Committed transaction did not publish exactly one entity.");
    }

    private static void StaleWriteFailsClosed(ICadConformanceFixture fixture)
    {
        var document = fixture.CreateIsolatedDocument("conformance-stale");
        using var first = document.Database.BeginTransaction();
        using var stale = document.Database.BeginTransaction();
        first.Append(LineDraft());
        first.Commit();
        stale.Append(new CadEntityDraft(CadEntityKind.Point, PointBounds(2, 2)));
        RequireThrows<InvalidOperationException>(stale.Commit, "Stale concurrent write was accepted.");
        Require(EntityCount(document.Database) == 1, "Stale write partially mutated the database.");
    }

    private static void UndoRedoStableHandle(ICadConformanceFixture fixture)
    {
        var document = fixture.CreateIsolatedDocument("conformance-history");
        CadHandle handle;
        using (var tx = document.Database.BeginTransaction())
        {
            handle = tx.Append(LineDraft());
            tx.Commit();
        }
        Require(document.Database.History.CanUndo, "Committed drawing change did not expose undo history.");
        document.Database.History.Undo();
        Require(EntityCount(document.Database) == 0, "Undo did not remove committed entity.");
        Require(document.Database.History.CanRedo, "Undo did not expose redo history.");
        document.Database.History.Redo();
        using var read = document.Database.BeginTransaction(CadTransactionMode.ReadOnly);
        Require(read.Get(handle) is not null, "Redo did not restore the original stable handle.");
    }

    private static void LayerLockIntegrity(ICadConformanceFixture fixture)
    {
        var document = fixture.CreateIsolatedDocument("conformance-layer");
        CadHandle handle;
        using (var tx = document.Database.BeginTransaction())
        {
            tx.CreateLayer("QA-LOCKED");
            handle = tx.Append(new CadEntityDraft(CadEntityKind.Line, LineDraft().Extents, null, "QA-LOCKED"));
            tx.Commit();
        }
        using (var tx = document.Database.BeginTransaction())
        {
            tx.UpdateLayer(new CadLayerSnapshot("QA-LOCKED", true, false, true));
            tx.Commit();
        }
        using var locked = document.Database.BeginTransaction();
        var entity = locked.Get(handle) ?? throw new InvalidOperationException("Layer test entity disappeared.");
        RequireThrows<InvalidOperationException>(() => locked.Update(entity), "Locked-layer entity update was accepted.");
    }

    private static void BlockReferenceIntegrity(ICadConformanceFixture fixture)
    {
        var document = fixture.CreateIsolatedDocument("conformance-block");
        if ((document.Database.Capabilities & CadCapabilities.Blocks) == 0) return;
        CadHandle reference;
        using (var tx = document.Database.BeginTransaction())
        {
            tx.CreateBlock("QA-BLOCK", new Point3(0, 0), new[] { LineDraft() });
            reference = tx.InsertBlock("QA-BLOCK", new Point3(5, 5));
            tx.Commit();
        }
        using (var tx = document.Database.BeginTransaction())
        {
            RequireThrows<InvalidOperationException>(() => tx.EraseBlock("QA-BLOCK"), "Referenced block definition was deletable.");
        }
        using (var tx = document.Database.BeginTransaction())
        {
            tx.Erase(reference);
            tx.EraseBlock("QA-BLOCK");
            tx.Commit();
        }
        using var read = document.Database.BeginTransaction(CadTransactionMode.ReadOnly);
        Require(read.GetBlock("QA-BLOCK") is null, "Unreferenced block definition was not deleted.");
    }

    private static void EditorSelection(ICadConformanceFixture fixture)
    {
        var document = fixture.CreateIsolatedDocument("conformance-selection");
        CadHandle handle;
        using (var tx = document.Database.BeginTransaction())
        {
            handle = tx.Append(LineDraft());
            tx.Commit();
        }
        document.Editor.Selection.Set(new[] { handle });
        Require(document.Editor.Selection.Current.Count == 1 && document.Editor.Selection.Current.Contains(handle), "Editor selection did not preserve the requested handle.");
        document.Editor.Selection.Clear();
        Require(document.Editor.Selection.Current.Count == 0, "Editor selection did not clear.");
    }

    private static int EntityCount(ICadDatabase database)
    {
        using var read = database.BeginTransaction(CadTransactionMode.ReadOnly);
        return read.Query().Count;
    }

    private static CadEntityDraft LineDraft()
        => new(CadEntityKind.Line, new BoundingBox3(new Point3(0, 0), new Point3(10, 0)));

    private static BoundingBox3 PointBounds(double x, double y)
        => new(new Point3(x, y), new Point3(x, y));

    private static void RunCase(string code, List<CadConformanceFinding> findings, Action action)
    {
        try
        {
            action();
            findings.Add(new CadConformanceFinding(code, CadConformanceSeverity.Info, "PASS"));
        }
        catch (Exception ex)
        {
            findings.Add(new CadConformanceFinding(code, CadConformanceSeverity.Error, ex.GetType().Name + ": " + ex.Message));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void RequireThrows<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException(message);
    }
}
