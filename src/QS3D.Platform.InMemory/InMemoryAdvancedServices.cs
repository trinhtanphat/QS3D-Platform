namespace QS3D.Platform.InMemory;

public sealed class InMemoryAdvancedServices
{
    internal InMemoryAdvancedServices(InMemoryCadDocument document)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        Viewport = new InMemoryViewportService(document.Database);
        Snaps = new InMemorySnapService(document.Database, Viewport);
        SpatialSelection = new InMemorySpatialSelectionService(document.Database);
    }

    public InMemoryViewportService Viewport { get; }
    public InMemorySnapService Snaps { get; }
    public InMemorySpatialSelectionService SpatialSelection { get; }
}

public static class InMemoryAdvancedServicesRegistry
{
    private static readonly object Sync = new();
    private static readonly Dictionary<InMemoryCadDocument, InMemoryAdvancedServices> Services = new();

    public static InMemoryAdvancedServices For(InMemoryCadDocument document)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        lock (Sync)
        {
            if (Services.TryGetValue(document, out var services)) return services;
            services = new InMemoryAdvancedServices(document);
            Services.Add(document, services);
            return services;
        }
    }
}
