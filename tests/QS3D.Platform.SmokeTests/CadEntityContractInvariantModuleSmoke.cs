using System.Runtime.CompilerServices;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.Domain;
using QS3D.Platform.Geometry;

namespace QS3D.Platform.SmokeTests;

internal static class CadEntityContractInvariantModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var bounds = new BoundingBox3(new Point3(0, 0, 0), new Point3(1, 1, 0));
        var invalidKind = (CadEntityKind)int.MaxValue;

        Throws<ArgumentOutOfRangeException>(() => new CadEntityDraft(CadEntityKind.Unknown, bounds));
        Throws<ArgumentOutOfRangeException>(() => new CadEntityDraft(invalidKind, bounds));
        Throws<ArgumentException>(() => new CadEntityDraft(CadEntityKind.Line, bounds, layerName: " "));

        Throws<ArgumentException>(() => new CadEntitySnapshot(default, CadEntityKind.Line, bounds, new Dictionary<string, string>()));
        Throws<ArgumentOutOfRangeException>(() => new CadEntitySnapshot(new CadHandle("1"), CadEntityKind.Unknown, bounds, new Dictionary<string, string>()));
        Throws<ArgumentOutOfRangeException>(() => new CadEntitySnapshot(new CadHandle("1"), invalidKind, bounds, new Dictionary<string, string>()));
        Throws<ArgumentNullException>(() => new CadEntitySnapshot(new CadHandle("1"), CadEntityKind.Line, bounds, null!));
        Throws<ArgumentException>(() => new CadEntitySnapshot(new CadHandle("1"), CadEntityKind.Line, bounds, new Dictionary<string, string>(), " "));

        var draft = new CadEntityDraft(CadEntityKind.Line, bounds);
        Throws<ArgumentOutOfRangeException>(() => _ = draft with { Kind = invalidKind });
        var snapshot = new CadEntitySnapshot(new CadHandle("A"), CadEntityKind.Line, bounds, new Dictionary<string, string>());
        Throws<ArgumentOutOfRangeException>(() => _ = snapshot with { Kind = invalidKind });

        Console.WriteLine("PASS CAD entity contract structural invariants");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
