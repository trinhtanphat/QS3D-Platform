using System.Runtime.CompilerServices;
using QS3D.Platform.Cad.Abstractions;
using QS3D.Platform.InMemory;

namespace QS3D.Platform.SmokeTests;

internal static class InMemoryDocumentServicesModuleSmoke
{
    [ModuleInitializer]
    internal static void Run()
    {
        var xrefs = new InMemoryXrefService(path => StringComparer.Ordinal.Equals(path, "exists.dwg"));
        Equal(CadXrefStatus.Loaded, xrefs.Attach("exists.dwg", "Base", CadXrefKind.Attach).Status);
        Equal(CadXrefStatus.Missing, xrefs.Attach("missing.dwg", "Missing", CadXrefKind.Overlay).Status);
        Throws<ArgumentOutOfRangeException>(() => xrefs.Attach("exists.dwg", "Bad", (CadXrefKind)999));
        xrefs.Unload("Base");
        Equal(CadXrefStatus.Unloaded, xrefs.GetXrefs().Single(item => item.Name == "Base").Status);
        xrefs.Reload("Base");
        Equal(CadXrefStatus.Loaded, xrefs.GetXrefs().Single(item => item.Name == "Base").Status);
        xrefs.Detach("Missing");
        Equal(1, xrefs.GetXrefs().Count);

        var layouts = new InMemoryLayoutService();
        Equal("Model", layouts.CurrentLayoutName);
        var sheet = layouts.Create("Sheet 01");
        Require(!sheet.IsModel, "created sheet must not be Model");
        Equal(210d, sheet.PaperWidthMm);
        Equal(297d, sheet.PaperHeightMm);
        layouts.SetCurrent(sheet.Name);
        Throws<InvalidOperationException>(() => layouts.Delete(sheet.Name));
        layouts.SetCurrent("Model");
        layouts.Delete(sheet.Name);
        Throws<InvalidOperationException>(() => layouts.Delete("Model"));

        var plot = new InMemoryPlotService(layouts);
        var missing = plot.Plot(new CadPlotRequest("Missing", CadPlotTargetKind.Pdf, "missing.pdf"));
        Require(!missing.Succeeded && plot.Requests.Count == 0, "missing layout must not be recorded");
        Throws<ArgumentOutOfRangeException>(() => plot.Plot(new CadPlotRequest("Model", (CadPlotTargetKind)999, "bad.out")));
        var recorded = plot.Plot(new CadPlotRequest("Model", CadPlotTargetKind.Pdf, "model.pdf"));
        Require(!recorded.Succeeded, "reference plot recorder must not claim output success");
        Require(recorded.OutputPath is null, "reference plot recorder must not manufacture output path");
        Equal(1, plot.Requests.Count);

        Console.WriteLine("PASS xref layout and plot reference lifecycle");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected} but got {actual}.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
