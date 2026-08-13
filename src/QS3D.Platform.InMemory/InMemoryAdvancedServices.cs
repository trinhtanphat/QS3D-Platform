using System.Runtime.CompilerServices;

namespace QS3D.Platform.InMemory;

public sealed class InMemoryAdvancedServices
{
    internal InMemoryAdvancedServices(InMemoryCadDocument document)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        Viewport = new InMemoryViewportService(document.Database);
        Snaps = new InMemorySnapService(document.Database, Viewport);
        SpatialSelection = new InMemorySpatialSelectionService(document.Database);
        Xrefs = new InMemoryXrefService();
        Layouts = new InMemoryLayoutService();
        Plot = new InMemoryPlotService(Layouts);
    }

    public InMemoryViewportService Viewport { get; }
    public InMemorySnapService Snaps { get; }
    public InMemorySpatialSelectionService SpatialSelection { get; }
    public InMemoryXrefService Xrefs { get; }
    public InMemoryLayoutService Layouts { get; }
    public InMemoryPlotService Plot { get; }
}

public static class InMemoryAdvancedServicesRegistry
{
    private static readonly ConditionalWeakTable<InMemoryCadDocument, InMemoryAdvancedServices> Services = new();

    public static InMemoryAdvancedServices For(InMemoryCadDocument document)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        return Services.GetValue(document, static value => new InMemoryAdvancedServices(value));
    }
}
