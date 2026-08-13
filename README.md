# QS3D Platform

Shared clean-room domain and CAD-host contracts for the QS3D product family.

The platform contains **no BricsCAD, AutoCAD, ODA, UI-framework or proprietary SDK dependency**. `QS3D-CAD` and `QS3D-BricsCAD` consume the same host-neutral semantics through adapters.

See [`PLANNING.md`](PLANNING.md) for the master architecture and roadmap.

## Projects

- `QS3D.Platform.Geometry` — finite numeric/geometry value objects.
- `QS3D.Platform.Domain` — canonical IDs, CAD references and semantic BIM/QS state.
- `QS3D.Platform.Cad.Abstractions` — document/database/transaction/editor/selection contracts.
- `QS3D.Platform.Application` — command registry and host-neutral command execution.
- `QS3D.Platform.InMemory` — deterministic non-production adapter used for contract development/tests.
- `QS3D.Platform.SmokeTests` — dependency-free executable regression suite.

## Validate

```bash
dotnet build QS3D.Platform.sln -c Release
dotnet run --project tests/QS3D.Platform.SmokeTests/QS3D.Platform.SmokeTests.csproj -c Release
```

`QS3D.Platform.InMemory` is deliberately not a DWG engine. Production drawing/database/rendering is implemented by consuming host adapters.
