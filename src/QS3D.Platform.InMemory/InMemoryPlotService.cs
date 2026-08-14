using QS3D.Platform.Cad.Abstractions;

namespace QS3D.Platform.InMemory;

public sealed class InMemoryPlotService : ICadPlotService
{
    private readonly ICadLayoutService _layouts;
    private readonly List<CadPlotRequest> _requests = new();

    public InMemoryPlotService(ICadLayoutService layouts)
        => _layouts = layouts ?? throw new ArgumentNullException(nameof(layouts));

    public IReadOnlyList<CadPlotRequest> Requests => _requests.ToArray();

    public CadPlotResult Plot(CadPlotRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (!Enum.IsDefined(typeof(CadPlotTargetKind), request.TargetKind))
            throw new ArgumentOutOfRangeException(nameof(request), request.TargetKind, "Plot target kind must be a defined value.");
        if (string.IsNullOrWhiteSpace(request.LayoutName)) throw new ArgumentException("Plot layout name must not be blank.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Target)) throw new ArgumentException("Plot target must not be blank.", nameof(request));
        if (!_layouts.GetLayouts().Any(layout => StringComparer.OrdinalIgnoreCase.Equals(layout.Name, request.LayoutName)))
            return new CadPlotResult(false, null, $"Layout '{request.LayoutName}' does not exist.");
        _requests.Add(request);
        return new CadPlotResult(false, null, "Reference plot service recorded the request but does not produce native plot output.");
    }
}
