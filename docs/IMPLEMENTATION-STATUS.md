# QS3D Platform implementation status

**Date:** 2026-08-14 (UTC+7)  
**Repository role:** vendor-neutral shared QS3D domain/contracts  
**Evidence state:** `SOURCE_VALIDATED / NATIVE_ADAPTER_EVIDENCE_OUT_OF_SCOPE`  
**Rule:** source/reference validation is not native CAD runtime qualification.

## Implemented shared foundation

- `netstandard2.0` shared boundary for BricsCAD V25/net48 consumption: Geometry, Domain, CAD Abstractions, Application, Quantity, Diagnostics, Persistence, Parity and Families.
- Finite numeric/geometry primitives, explicit tolerance/unit policies and canonical hexadecimal CAD handles.
- Project/floor/zone/family/element identities and semantic model with family-kind/location/CAD-reference invariants.
- Semantic mutation rejects supplied empty Floor/Zone identities, structurally invalid source/generated CAD references and undefined semantic element-kind enum values before they enter canonical project state or snapshots.
- Host-neutral CAD document/database/transaction/editor/selection/layer/static-block contracts.
- Command registry/application context, dependency planning, dirty/freshness propagation and stale-regeneration guards.
- Quantity dimensions/units, deterministic property-based rule evaluation, facts/aggregation, cost/BQ/schedule projections and deterministic CSV projection.
- Quantity value/rule/rate/schedule boundaries reject undefined enum values, empty source drawing/handle identities and null schedule entries before invalid state can be retained.
- Family-schema boundaries reject undefined semantic kind, parameter type and quantity-dimension values.
- Model-health diagnostics including missing semantic references and canonical CAD source/generated ownership conflicts; diagnostic findings reject undefined severity values so readiness cannot fail open on an unknown severity.
- Schema-neutral semantic snapshots, deterministic capture/restore, migration chains and project-container manifest/integrity contracts.
- Module semantic versions, dependency ranges, deterministic load planning and cycle/missing/version fail-closed behavior.
- Cross-product golden parity fixture model/runner and reusable CAD adapter conformance harness.
- Source guards for vendor-neutral boundaries and `netstandard2.0` compatibility.

## Deterministic reference services implemented

`QS3D.Platform.InMemory` is a non-production `net8.0` reference adapter used to exercise contracts without a proprietary/native CAD SDK. It includes:

- in-memory document/database/transaction/history/layer/block behavior;
- viewport state, zoom-extents/window, deterministic AABB hit testing and invalidation bookkeeping;
- supported reference snaps such as endpoint, midpoint, center, quadrant and nearest;
- XY polygon window/crossing reference selection;
- Xref attach/reload/unload/detach lifecycle with deterministic loaded/missing status resolution;
- Model/paper-layout reference lifecycle with current-layout and deletion guards;
- deliberately non-producing plot recorder: a reference plot request is recorded but returns no native output success/path;
- document-scoped `InMemoryAdvancedServicesRegistry` backed by `ConditionalWeakTable`, preventing the reference registry itself from retaining closed/unreferenced documents.

These implementations prove API semantics and deterministic regressions. They do **not** qualify `CadCapabilities` for a production native adapter.

## Validation/source gates

`scripts/validate.sh` is the authoritative Platform validation entry point. It runs, in order:

1. vendor-neutral preflight;
2. `netstandard2.0` boundary gate;
3. reference-services gate;
4. parity gate (`scripts/check-parity.py`);
5. family-schema gate;
6. Release build of `QS3D.Platform.sln`;
7. deterministic `QS3D.Platform.SmokeTests`.

## Current validated checkpoint

Exact source checkpoint `d179795c5f89a49b54756d99b7b28cc19b9dd6ac` passed GitHub Actions **QS3D Platform CI #92**, run `31777988636`, on 2026-08-14.

The run completed authoritative validation successfully, including source gates, Release build and deterministic smoke coverage. This checkpoint includes the semantic, snapshot, quantity, family-schema, diagnostic-readiness and quantity-schedule fail-closed hardening described above and is the current shared Platform source/reference validation baseline for consumers to pin.

## Adapter qualification model

An adapter may advertise a `CadCapabilities` bit only when its implementation and evidence satisfy the corresponding contract. Evidence levels remain separate:

1. source/preflight;
2. deterministic Platform/reference/parity tests;
3. adapter conformance tests;
4. exact product-SHA/backend-version native runtime tests;
5. file-format round-trip/performance/release qualification.

`REFERENCE_PASS` must never be promoted to native or production qualification.

## BricsCAD migration status

`QS3D-BricsCAD` remains authoritative for production plugin behavior until individual Platform migration slices have parity evidence. Migration remains incremental; Platform does not authorize big-bang deletion of `QS3D.Core`.

Shared foundations available for migration include canonical IDs/handles, finite/unit/tolerance policies, semantic project state, quantity/BQ/schedules, dependency/dirty planning, persistence snapshots/migrations, readiness diagnostics, golden parity fixtures and adapter conformance contracts.

## Remaining host-neutral backlog

Useful non-native work remains deliberately bounded:

- richer family/parametric schema contracts and deterministic family-version migration fixtures;
- semantic authoring/parity helpers shared by hosted and standalone products where behavior is already established;
- stronger compatibility fixtures for snapshot/container migration and forward/backward rejection behavior;
- extension/plugin packaging metadata and host-neutral registration contracts;
- broader model-health rules derived only from host-neutral semantic/reference state.

## Native-blocked backlog

These belong to a real product adapter/runtime, not Platform reference evidence:

- DWG/DXF read/write and round-trip fidelity;
- native entity geometry extraction/editing;
- GPU viewport/device behavior;
- true intersection/tangent/perpendicular OSNAP and grips;
- native Xrefs/layouts/page setup/plot/PDF;
- native 3D primitives/B-Rep/booleans;
- real large-drawing performance;
- clean-machine installers/signing/runtime qualification.

`QS3D-CAD/docs/LOCAL-NATIVE-QUALIFICATION.md` and `docs/NATIVE-SDK-INTEGRATION-CHECKLIST.md` define the standalone native evidence lane.
