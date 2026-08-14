# QS3D Platform implementation status

**Date:** 2026-08-14 (UTC+7)  
**Repository role:** vendor-neutral shared QS3D domain/contracts  
**Evidence state:** `SOURCE_VALIDATED / NATIVE_ADAPTER_EVIDENCE_OUT_OF_SCOPE`  
**Rule:** source/reference validation is not native CAD runtime qualification.

## Implemented shared foundation

- `netstandard2.0` shared boundary for BricsCAD V25/net48 consumption: Geometry, Domain, CAD Abstractions, Application, Quantity, Diagnostics, Persistence, Parity and Families.
- Finite numeric/geometry primitives, explicit tolerance/unit policies and canonical hexadecimal CAD handles.
- Project/floor/zone/family/element identities and semantic model with family-kind/location/CAD-reference invariants.
- Semantic mutation and persistence reject supplied empty identities, structurally invalid CAD references and undefined semantic element-kind enum values before invalid state enters canonical project data.
- CAD entity draft/snapshot contracts reject `Unknown` or undefined entity kinds; snapshots reject empty handles, blank layers and null property collections/values. Init accessors preserve those invariants across `with` expressions.
- Host-neutral CAD document/database/transaction/editor/selection/layer/static-block contracts.
- Command registry/application context, dependency planning, dirty/freshness propagation and stale-regeneration guards.
- Quantity dimensions/units, deterministic property-based rule evaluation, facts/aggregation, cost/BQ/schedule projections and deterministic CSV projection.
- Quantity value/rule/rate/schedule boundaries reject undefined enum values, empty source drawing/handle identities and null schedule entries before invalid state can be retained.
- Family-schema boundaries reject undefined semantic kind, parameter type and quantity-dimension values.
- Model-health diagnostics including missing semantic references and canonical CAD source/generated ownership conflicts; findings reject undefined severity values so readiness cannot fail open on an unknown severity.
- Schema-neutral semantic snapshots, deterministic capture/restore, migration chains and project-container manifest/integrity contracts.
- Module semantic versions, dependency ranges, deterministic load planning and cycle/missing/version fail-closed behavior.
- Cross-product golden parity fixture model/runner and reusable CAD adapter conformance harness.

## Deterministic reference services

`QS3D.Platform.InMemory` is a non-production `net8.0` reference adapter used to exercise contracts without a proprietary/native CAD SDK. It includes transactional database/history/layer/block behavior, viewport/snap/spatial selection, reference Xref/Layout/Plot lifecycle and a weak document-scoped advanced-service registry.

These implementations prove API semantics and deterministic regressions. They do **not** qualify `CadCapabilities` for a production native adapter.

## Authoritative validation

`scripts/validate.sh` runs:

1. vendor-neutral preflight;
2. `netstandard2.0` boundary gate;
3. reference-services gate;
4. parity gate;
5. family-schema gate;
6. Release build of `QS3D.Platform.sln`;
7. deterministic `QS3D.Platform.SmokeTests`.

## Current validated checkpoint

Exact current source checkpoint **`986d5baa00065a1adb53e53733811beb9cf0a2d9`** passed GitHub Actions **QS3D Platform CI #94**, run **`31778331523`**, on 2026-08-14.

That run validates all prior semantic/snapshot/quantity/family/diagnostic/schedule fail-closed hardening plus the CAD entity contract invariants. `QS3D-CAD` pins this exact Platform SHA for its current cross-repository validation lane.

## Adapter qualification model

Evidence levels remain separate:

1. source/preflight;
2. deterministic Platform/reference/parity tests;
3. adapter conformance tests;
4. exact product-SHA/backend-version native runtime tests;
5. file-format round-trip/performance/release qualification.

`REFERENCE_PASS` must never be promoted to native or production qualification.

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
