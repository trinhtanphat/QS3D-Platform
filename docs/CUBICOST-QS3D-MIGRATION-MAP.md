# Cubicost-style parity migration map — QS3D Core to Platform

Updated: 2026-08-15 (UTC+7)  
Tracking: #13

## Purpose

`QS3D-BricsCAD/src/QS3D.Core` predates the current multi-host `QS3D-Platform` architecture and already contains production-oriented host-neutral quantity, MEP, coordination and cost logic. This document defines a **compatibility-first convergence**, not a rewrite.

No existing BricsCAD command should be switched to Platform merely because an equivalent type now exists. A host migration lane must prove semantic parity first, adapt data at the boundary, run both deterministic suites, and only then remove duplicate code in a later cleanup PR.

## Ownership mapping

| Existing BricsCAD Core surface | Shared Platform target | Migration rule |
|---|---|---|
| `QS3D.Core.Mep.MepElementKind` | `QS3D.Platform.Parity.MepElementKind` | map by explicit enum switch; never numeric cast across products |
| `QS3D.Core.Mep.MepElement` | `QS3D.Platform.Parity.MepElement` | preserve stable ID, system, specification, region and SI metrics |
| `QS3D.Core.Mep.MepQuantityService` | `QS3D.Platform.Parity.MepQuantityService` | golden compare grouping/count/L/A/V before adapter switch |
| `QS3D.Core.Mep.MepRecognitionRule` + `QS3D.Core.Mep.MepRecognitionProfile` | `QS3D.Platform.Parity.MepRecognitionRule` + `QS3D.Platform.Parity.MepRecognitionProfile` | preserve priority, Layer/Block scope and fail-closed ambiguity |
| `QS3D.Core.Coordination.AxisAlignedBox` | `QS3D.Platform.Parity.AxisAlignedBox` | SI metres only; native extents conversion remains adapter-owned |
| `QS3D.Core.Coordination.CoordinationElement` | `QS3D.Platform.Parity.CoordinationElement` | map explicit discipline/category/system/region |
| `QS3D.Core.Coordination.ClashDetectionService` | `QS3D.Platform.Parity.ClashDetectionService` | golden compare hard/clearance ordering and separation |
| advanced rate build-up | `CostRateBuildUp` | decimal money semantics preserved |
| historical cost/benchmark | `HistoricalCostCatalog` / `CostBenchmarkService` | preserve comparable dimension key and currency filters |
| BQ library/project reuse | `BqLibraryCatalog` | duplicate incoming payload must fail even when replacement is enabled |
| BQ/rate reference marks | `CostReferenceIndex` | stable mark/source identities; reverse lookup parity |
| Adjust Cost | `CostAdjustmentService` | compare ratio/target arithmetic |
| trade/CFA analysis | `TradeCostAnalysisService` | blank trade maps to `Unclassified` |
| tender requirement/bid evaluation | `TenderEvaluationService` | incomplete bids rank 0; complete bids deterministic by total/id |
| tender addendum/change review | `TenderRevisionService` | added/removed/changed item identities |
| multi-round tender review | `MultiRoundTenderEvaluationService` | order rounds by UTC opening time then stable round ID |
| progress claim certification | `ProgressClaimService` | contract cap, rejected overclaim, retention and net value parity |
| time/progress cost monitoring | `TimePhasedCostService` | shared cumulative 4D/5D projection |
| TAS CAD identification options | `CadIdentificationProfile` | vendor-neutral options only; actual entity reading stays native |

## BricsCAD adapter surfaces that do not move to Platform

These remain in `QS3D-BricsCAD`:

- `EntitySnapshotReader` and BricsCAD selection semantics;
- `CadHandleService` native handle/ObjectId resolution;
- `CadUnitService` reading BricsCAD drawing units;
- `Transaction`, `DBObject`, `Entity`, `Solid3d` operations;
- `QS3DMEPTAKEOFF`, `QS3DMEPCLASH`, `QS3DMEPCLASHLOCATE`, `QS3DMEPEXACTCLASH` command wiring;
- native `CheckInterference` exact clash;
- transient `Highlight` / `Unhighlight`, zoom/view transforms and modeless palettes;
- DWG/XData/NOD/sidecar persistence bridges;
- licensed V25/V26 runtime qualification.

Platform accepts only portable values and stable `CadReference` identities. It must never expose BricsCAD `ObjectId`, `DBObject`, `Solid3d` or UI/runtime types.

## AutoCAD and standalone adoption

`QS3D-AutoCAD` should consume the same Platform parity contracts through Autodesk-specific extraction/mapping. It must not copy BricsCAD native code or Teigha types.

`QS3D-CAD` can consume the contracts directly for reference/in-memory workflows. Native DWG/rendering truth remains gated on the separately licensed standalone native adapter.

## Golden migration sequence

For each migrated capability:

1. freeze representative host-neutral fixtures from the legacy implementation without private/customer data;
2. evaluate legacy `QS3D.Core` and Platform implementation on identical normalized inputs;
3. compare deterministic outputs including ordering, identities, numeric tolerances and fail-closed errors;
4. add an adapter conversion function with explicit enum/value mapping;
5. switch one command/service lane to Platform on an agent branch;
6. run Core smoke + Platform smoke + host source guards;
7. run native LOCAL_ONLY acceptance when the changed lane touches the CAD host;
8. only after exact-SHA evidence, retire the duplicate legacy implementation in a separate cleanup lane.

## Non-goals

- no bulk namespace replacement;
- no history rewrite;
- no deletion of mature `QS3D.Core` code before consumers migrate;
- no native-CAD PASS inferred from Platform smoke tests;
- no copying proprietary Cubicost implementation or private file formats.
