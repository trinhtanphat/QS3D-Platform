# QS3D Platform implementation status

**Date:** 2026-08-13 (UTC+7)  
**Repository role:** vendor-neutral shared QS3D domain/contracts  
**Rule:** source presence and deterministic in-memory tests are not native CAD runtime qualification.

## Implemented shared foundation

- `netstandard2.0` shared target for BricsCAD V25/net48 compatibility.
- Finite numeric/geometry value objects.
- Project/floor/zone/family/element IDs and canonical hexadecimal CAD handles.
- Drawing + stable-handle CAD references.
- Semantic project/floor/zone/family/element model.
- Family-kind invariant and generated/source reference ownership primitives.
- Host-neutral CAD document/database/transaction/editor/selection contracts.
- Optimistic transaction revision checks.
- Undo/redo contract.
- Layer table/current layer/entity ownership contract.
- Static block definition/reference contract including uniform scale + Z rotation insertion semantics.
- Command registry/application command context.
- Quantity dimensions, canonical SI units, traceable facts and deterministic aggregation.
- Semantic model-health diagnostics for missing family/floor/zone/CAD reference state.
- Advanced optional host contracts for viewport/hit-test, OSNAP, Xrefs, layouts, plotting and spatial selection.
- Deterministic dependency impact graph with cycle fail-closed behavior.
- Revision-safe dirty/freshness tracker with downstream propagation and stale-regeneration protection.
- Non-production in-memory CAD adapter for contract development.
- Deterministic smoke executable plus module-initializer regression surfaces.
- Reusable database conformance smoke that tests advertised transaction/layer/block invariants.
- Vendor-neutral source preflight.

## Deliberately not implemented in Platform

Platform must not contain:

- BricsCAD/AutoCAD/ODA proprietary types;
- a DWG parser/database implementation;
- a GPU renderer;
- a B-Rep/native solid kernel;
- WPF/desktop host UI;
- native Xref/layout/plot implementations;
- vendor SDK binaries;
- runtime object pointers as persisted identity.

Those belong to product adapters.

## Adapter qualification model

An adapter may advertise a `CadCapabilities` bit only when its implementation and tests satisfy the corresponding contract. Optional advanced service interfaces do not automatically enable a capability.

Evidence levels remain separate:

1. source/preflight;
2. deterministic Platform tests;
3. adapter conformance tests;
4. exact host/native runtime tests;
5. file-format round-trip/performance/release qualification.

## GitHub Actions note

A Platform Actions build was attempted after the CI workflow was added. GitHub rejected the job before a runner started because the account had exhausted included Actions minutes. That run is therefore **not a compiler/test failure and not a PASS**. Local/other-runner validation remains required until Actions capacity is restored.

## BricsCAD migration status

`QS3D-BricsCAD` remains authoritative for production plugin behavior until each migration slice has parity evidence. See that repository's `docs/QS3D-PLATFORM-MIGRATION.md`.

Priority shared migration order:

1. IDs/handles/finite units/geometry facts;
2. semantic project/floor/zone/family/element state;
3. quantity/formula/cost/report projections;
4. dependency/dirty/regeneration planning;
5. persistence compatibility rules;
6. adapter conformance.

No big-bang deletion of `QS3D.Core` is authorized by the existence of this repository.

## Next host-neutral implementation backlog

These can continue without a native DWG SDK, provided parity with established QS3D behavior is proved:

- richer unit/tolerance policy;
- quantity formulas/rule catalog;
- cost/BQ/schedule projections;
- semantic dependency identity adapters;
- project persistence schema-neutral models;
- model-health rule expansion;
- cross-product golden fixture definitions;
- plugin/extension contracts;
- project/family version migration contracts.

## Native-blocked backlog

These require a real product adapter/runtime:

- DWG read/write fidelity;
- true entity geometry extraction/editing;
- GPU viewport;
- native OSNAP/grips;
- Xrefs;
- layouts/plot/PDF;
- native 3D primitives/B-Rep/booleans;
- real large-drawing performance;
- clean-machine installers and runtime qualification.

The standalone product tracks those gates in `QS3D-CAD/docs/NATIVE-SDK-INTEGRATION-CHECKLIST.md`.