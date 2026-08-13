# QS3D Platform implementation status

**Date:** 2026-08-13 (UTC+7)  
**Repository role:** vendor-neutral shared QS3D domain/contracts  
**Evidence state:** `SOURCE_READY / PENDING_BUILD_EVIDENCE`  
**Rule:** source presence and deterministic reference behavior are not native CAD runtime qualification.

## Implemented shared foundation

- `netstandard2.0` shared boundary for BricsCAD V25/net48 consumption: Geometry, Domain, CAD Abstractions, Application, Quantity, Diagnostics, Persistence and Parity.
- Finite numeric/geometry primitives, explicit tolerance/unit policies and canonical hexadecimal CAD handles.
- Project/floor/zone/family/element identities and semantic model with family-kind/location/CAD-reference invariants.
- Host-neutral CAD document/database/transaction/editor/selection/layer/static-block contracts.
- Command registry/application context, dependency planning, dirty/freshness propagation and stale-regeneration guards.
- Quantity dimensions/units, deterministic property-based rule evaluation, facts/aggregation, cost/BQ/schedule projections and deterministic CSV projection.
- Model-health diagnostics including missing semantic references and canonical CAD source/generated ownership conflicts.
- Schema-neutral semantic snapshots, deterministic capture/restore, migration chains and project-container manifest/integrity contracts.
- Module semantic versions, dependency ranges, deterministic load planning and cycle/missing/version fail-closed behavior.
- Cross-product golden parity fixture model/runner. A fixture carries a shared semantic snapshot, quantity rules and expected readiness/quantity results; the runner restores through shared Persistence and evaluates shared Diagnostics/Quantity. Platform smoke verifies canonical handle normalization and `2500 mm × 3000 mm = 7.5 m²`, and QS3D-CAD smoke consumes the same runner from standalone command-generated state.
- Reusable CAD adapter conformance harness for transaction/layer/block invariants.
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

These implementations exist to prove API semantics and deterministic regressions. They do **not** qualify `CadCapabilities` for a production native adapter.

## Deliberately not implemented in Platform

Platform must not contain or claim:

- BricsCAD/AutoCAD/ODA proprietary types or redistributed vendor SDK binaries;
- a DWG/DXF parser/database implementation;
- a production GPU renderer/device;
- a native geometry/B-Rep/topology kernel;
- WPF/desktop product UI;
- native Xref resolution against a real drawing database;
- native page setup/printing/PDF output;
- native 3D solids/booleans;
- runtime object pointers as persisted identity.

Those belong to product adapters. Reference Xref/Layout/Plot implementations above are intentionally not native implementations.

## Validation/source gates

`scripts/validate.sh` is the normal Platform validation entry point when a .NET toolchain is available. It currently runs vendor-neutral preflight, the `netstandard2.0` boundary, reference-service gate, Release build and deterministic smoke. `scripts/check-parity.py` separately guards the Parity project/solution/smoke wiring; attempts to add that one line to `validate.sh` in this session were blocked by the repository write safety gateway, so do not assume the parity source gate is automatically invoked until `validate.sh` visibly contains it.

The reference-services gate requires viewport/snap/spatial/Xref/Layout/Plot source and regression modules, enforces the weak document registry, and prevents the reference plot recorder from claiming native output success.

## Adapter qualification model

An adapter may advertise a `CadCapabilities` bit only when its implementation and evidence satisfy the corresponding contract. Evidence levels remain separate:

1. source/preflight;
2. deterministic Platform/reference/parity tests;
3. adapter conformance tests;
4. exact product-SHA/backend-version native runtime tests;
5. file-format round-trip/performance/release qualification.

`REFERENCE_PASS` must never be promoted to native or production qualification.

## BricsCAD migration status

`QS3D-BricsCAD` remains authoritative for production plugin behavior until individual Platform migration slices have parity evidence. The migration remains incremental; the existence of Platform does not authorize big-bang deletion of `QS3D.Core`.

Shared foundations now available for migration include canonical IDs/handles, finite/unit/tolerance policies, semantic project state, quantity/BQ/schedules, dependency/dirty planning, persistence snapshots/migrations, readiness diagnostics, golden parity fixtures and adapter conformance contracts.

## Remaining host-neutral backlog

Useful work that can continue without a native DWG SDK is now narrower:

- richer family/parametric schema contracts and deterministic family-version migration fixtures;
- semantic authoring/parity helpers shared by hosted and standalone products where behavior is already established;
- stronger compatibility fixtures for snapshot/container migration and forward/backward rejection behavior;
- extension/plugin packaging metadata and host-neutral registration contracts without implementing vendor/native dynamic loading;
- shared backend capability/evidence vocabulary where it can remain product-neutral;
- broader model-health rules that derive only from host-neutral semantic/reference state;
- canonical serialization/export for golden fixtures if a stable cross-repository artifact format is needed.

## Native-blocked backlog

These require a real product adapter/runtime and remain outside Platform reference evidence:

- DWG/DXF read/write and round-trip fidelity;
- native entity geometry extraction/editing;
- GPU viewport/device behavior;
- true intersection/tangent/perpendicular OSNAP and grips;
- native Xrefs;
- native layouts/page setup/plot/PDF;
- native 3D primitives/B-Rep/booleans;
- real large-drawing performance;
- clean-machine installers/signing/runtime qualification.

`QS3D-CAD/docs/LOCAL-NATIVE-QUALIFICATION.md` and `docs/NATIVE-SDK-INTEGRATION-CHECKLIST.md` define the standalone native evidence lane.

## Current evidence status

No `BUILD_PASS` is claimed by this document. GitHub Actions capacity was previously exhausted before useful runner execution, and the current conversation execution environment has no usable .NET compiler/SDK. Current claims therefore remain source implementation + static review + authored deterministic regression coverage until the validation entry points succeed on a real toolchain.
