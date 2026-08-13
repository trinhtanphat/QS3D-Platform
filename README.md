# QS3D Platform

Shared clean-room domain and CAD-host contracts for the QS3D product family.

The platform contains **no BricsCAD, AutoCAD, ODA, UI-framework or proprietary SDK dependency**. `QS3D-CAD` and `QS3D-BricsCAD` consume the same host-neutral semantics through adapters.

The shared contracts/domain projects target **`netstandard2.0`** so they can be consumed by the existing BricsCAD V25 `net48` adapter, the V26/.NET 8 adapter and the standalone .NET 8 product. The non-production in-memory adapter and smoke executable target `net8.0`.

See [`PLANNING.md`](PLANNING.md) for the master architecture and roadmap.

## Projects

- `QS3D.Platform.Geometry` (`netstandard2.0`) — finite numeric/geometry value objects.
- `QS3D.Platform.Domain` (`netstandard2.0`) — canonical IDs, CAD references and semantic BIM/QS state.
- `QS3D.Platform.Cad.Abstractions` (`netstandard2.0`) — document/database/transaction/editor/selection contracts.
- `QS3D.Platform.Application` (`netstandard2.0`) — command registry and host-neutral command execution.
- `QS3D.Platform.Quantity` (`netstandard2.0`) — deterministic SI quantity facts and aggregation with source/element traceability.
- `QS3D.Platform.Diagnostics` (`netstandard2.0`) — host-neutral semantic model health/readiness diagnostics.
- `QS3D.Platform.InMemory` (`net8.0`) — deterministic non-production adapter used for contract development/tests.
- `QS3D.Platform.SmokeTests` (`net8.0`) — dependency-free executable regression suite.

## Validate

```bash
dotnet build QS3D.Platform.sln -c Release
dotnet run --project tests/QS3D.Platform.SmokeTests/QS3D.Platform.SmokeTests.csproj -c Release
```

`QS3D.Platform.InMemory` is deliberately not a DWG engine. Production drawing/database/rendering is implemented by consuming host adapters.
