using System.Runtime.CompilerServices;
using QS3D.Platform.Domain;

namespace QS3D.Platform.SmokeTests;

internal static class SemanticElementInvariantModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var element = new SemanticElement(ElementId.New(), SemanticElementKind.Wall, "Invariant wall", FamilyId.New());

        Throws<ArgumentException>(() => element.AssignLocation(new FloorId(Guid.Empty), null));
        Throws<ArgumentException>(() => element.AssignLocation(null, new ZoneId(Guid.Empty)));

        Throws<ArgumentException>(() => element.SetSource(new CadReference(new DrawingId(Guid.Empty), new CadHandle("1"))));
        Throws<ArgumentException>(() => element.SetSource(new CadReference(DrawingId.New(), default)));
        Throws<ArgumentException>(() => element.AddGeneratedReference(new CadReference(new DrawingId(Guid.Empty), new CadHandle("2"))));
        Throws<ArgumentException>(() => element.AddGeneratedReference(new CadReference(DrawingId.New(), default)));
        Throws<ArgumentException>(() => element.RemoveGeneratedReference(default));

        var drawing = DrawingId.New();
        var source = new CadReference(drawing, new CadHandle("000A"));
        var generated = new CadReference(drawing, new CadHandle("000B"));
        element.SetSource(source);
        if (!element.SourceReference.HasValue || element.SourceReference.Value != source)
            throw new InvalidOperationException("Valid source CAD reference was not retained.");
        if (!element.AddGeneratedReference(generated) || !element.GeneratedReferences.Contains(generated))
            throw new InvalidOperationException("Valid generated CAD reference was not retained.");
        if (!element.RemoveGeneratedReference(generated) || element.GeneratedReferences.Contains(generated))
            throw new InvalidOperationException("Valid generated CAD reference was not removed.");

        Console.WriteLine("PASS semantic element structural identity invariants");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
