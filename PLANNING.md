# QS3D Platform — Master Planning

**Status:** architecture baseline  
**Owner:** QS3D  
**Target:** shared clean-room platform consumed by `QS3D-CAD` and `QS3D-BricsCAD`  
**Primary rule:** this repository must remain independent of BricsCAD, AutoCAD, ODA, UI frameworks, proprietary SDK binaries, customer drawings, and machine-specific runtime assumptions.

## 1. Product goal

`QS3D-Platform` is the durable product/domain layer behind the QS3D family. It owns semantic BIM/QS state, units, geometry value objects, quantity logic, persistence contracts, diagnostics, command contracts, and CAD-host abstractions. It does not own a desktop viewport and it does not directly call a vendor CAD API.

The target product family is:

```text
                         QS3D-Platform
                       /               \
                      /                 \
            QS3D-BricsCAD             QS3D-CAD
            hosted adapter            standalone host
                 |                         |
             BricsCAD              native CAD runtime
```

The platform is the compatibility boundary. A feature that can be deterministic and host-neutral belongs here. A feature that requires live document/editor/database/rendering APIs belongs behind an interface and is implemented by a host adapter.

## 2. Repository responsibilities

### This repository owns

- strongly typed identifiers and canonical handle/string identities;
- units and finite numeric policies;
- geometry value types that do not depend on a CAD kernel;
- project/building/floor/zone/family/element domain state;
- BIM/QS semantic objects and relationships;
- dependency graph, dirty/freshness and regeneration contracts;
- quantity rules, schedules and report models;
- persistence, schema migration and atomic-store contracts;
- health/readiness diagnostics;
- command metadata, command results and host-neutral command orchestration contracts;
- CAD abstraction contracts for document/database/transaction/editor/selection/layers/blocks/xrefs/layouts/plot/viewport/geometry services;
- package/version compatibility policy;
- deterministic tests and synthetic fixtures.

### This repository must not own

- `BrxMgd.dll`, `TD_Mgd.dll`, Autodesk assemblies, ODA binaries or headers;
- WPF/WinUI/Avalonia UI;
- a vendor-specific `ObjectId`, transaction, database, viewport or entity type;
- native renderer implementation;
- installer, updater or desktop application entry point;
- customer/private DWG files;
- proprietary geometry/kernel implementation details.

## 3. Architecture

```text
QS3D.Platform.Domain
  identifiers, units, project model, semantic objects
        |
        +--> QS3D.Platform.Geometry
        |      immutable points/vectors/bounds/tolerances
        |
        +--> QS3D.Platform.Cad.Abstractions
        |      host-neutral CAD contracts
        |
        +--> QS3D.Platform.Application
        |      commands/use-cases/transactions/undo contracts
        |
        +--> QS3D.Platform.Quantity
        |      quantity rules, schedules, BQ models
        |
        +--> QS3D.Platform.Persistence
        |      project serialization/migrations/atomic persistence contracts
        |
        +--> QS3D.Platform.Diagnostics
               model health/readiness/audit contracts
```

Dependencies must point inward. Domain and Geometry may not depend on Application, Persistence, Diagnostics or a CAD adapter.

## 4. CAD abstraction contract

The first stable API surface must cover:

### Documents

- application document manager;
- active document;
- drawing identity;
- open/new/save/save-as/close lifecycle;
- document events;
- document lock/write scope.

### Database

- canonical entity handle;
- entity lookup/query;
- append/erase/update;
- owner/container identity;
- layers, linetypes, text/dimension styles;
- blocks and block references;
- model/paper spaces;
- dictionaries and metadata extension points.

### Transactions and undo

- read/write transaction scope;
- commit/rollback;
- command transaction grouping;
- undo/redo records;
- no partial mutation after failed command.

### Editor and selection

- current coordinate system;
- point/distance/angle/entity prompts;
- selection set and filters;
- highlight/reveal/focus;
- command cancellation.

### Geometry

- line/polyline/arc/circle/ellipse/spline primitives;
- extents and transforms;
- intersections and closest-point queries;
- offsets/trims/extensions where kernel-backed;
- solid creation and boolean operations behind capability interfaces;
- tessellation/mesh exchange for viewport adapters.

### Drawing services

- layers;
- blocks;
- xrefs;
- layouts/viewports;
- plot/export;
- units/UCS;
- object snap candidates;
- capability discovery.

No abstraction method may expose a BricsCAD/ODA type in its public signature.

## 5. Identity rules

- Persist stable semantic IDs as explicit value objects.
- Persist CAD references as drawing identity + canonical CAD handle, never runtime object pointers/IDs.
- Canonicalize hex-like handles once at the abstraction boundary.
- Reject blank, non-finite or structurally invalid identity data before mutation.
- Distinguish source entities from generated entities and retain ownership/provenance.

## 6. Numeric and geometry policy

- all public numeric measurements must be finite;
- domain lengths use metres, areas m², volumes m³ unless a value type explicitly states otherwise;
- host units convert at adapter boundaries;
- tolerances are centralized and named by purpose;
- no hidden `double.Epsilon` geometry decisions;
- algorithms must be deterministic for identical normalized input;
- overflow/underflow guards are required for large-coordinate drawings.

## 7. Semantic BIM/QS model

Initial semantic families:

- Building / Project;
- Zone;
- Floor / Level;
- Family / Type;
- Wall;
- Slab;
- Beam;
- Column;
- Door;
- Window / Opening;
- Room / Space;
- Curtain Wall;
- Foundation;
- Rebar/Rebar Set;
- Finish / Material assignment.

Each semantic element stores authoritative semantic parameters and source/generated CAD references separately. Generated native geometry is derived state and must be reproducible from semantic inputs when the feature supports regeneration.

## 8. Quantity and schedule architecture

Quantity logic must remain deterministic and testable without a CAD host.

Required layers:

1. normalized measurements from a host adapter;
2. semantic quantity facts;
3. quantity rules;
4. calculated quantity results;
5. schedules/BQ projections;
6. XLSX/CSV/export models.

A report is never the source of truth. Recalculation must be possible from project/semantic state plus normalized measurements.

## 9. Persistence strategy

### Near term

Maintain compatibility with the existing QS3D project semantics and `.qsdb` concepts while extracting clean contracts and serializers.

### Standalone target

Define a versioned `.qs3d` package/container contract capable of storing:

```text
manifest
project semantic state
families/materials
quantities/schedules
views/layout metadata
source/provenance metadata
optional embedded or linked drawing payloads
migration history
```

Requirements:

- explicit schema version;
- bounded input sizes;
- hardened parsing;
- canonical IDs;
- validation before publication;
- atomic save + backup/recovery;
- forward-compatible unknown-field policy;
- migration tests for every released schema.

## 10. Cross-repository integration

### `QS3D-CAD`

Consumes released/pinned platform code and implements the standalone CAD adapter plus desktop shell.

### `QS3D-BricsCAD`

Remains a BricsCAD-hosted product. Existing `QS3D.Core` behavior is migrated incrementally into the platform after equivalence tests exist. The migration must not destabilize the shipping plugin.

### Dependency strategy

During bootstrap, consumers may pin this repository by exact commit/submodule or local source path. Stable releases should publish versioned packages. Version changes follow semantic versioning and compatibility tests.

## 11. Migration from `QS3D-BricsCAD`

Every existing surface must be classified:

- **MOVE** — deterministic host-neutral behavior moves to Platform;
- **ADAPT** — interface in Platform, BricsCAD implementation stays in plugin;
- **KEEP** — purely BricsCAD UI/runtime/integration remains in plugin;
- **REWRITE** — host-coupled code whose semantics are reusable but implementation is not;
- **DEFER** — feature awaiting runtime/kernel capability.

Migration order:

1. identifiers/units/numeric policies;
2. project/semantic domain;
3. persistence contracts;
4. quantity/reporting;
5. diagnostics/health;
6. dependency/regeneration;
7. command contracts;
8. CAD abstractions;
9. BricsCAD adapter conformance;
10. remove duplicated Core implementation only after parity evidence.

Never mass-copy vendor-dependent source into Platform.

## 12. Testing strategy

### Unit tests

- identifiers/canonicalization;
- units and numeric guards;
- domain invariants;
- quantity rules;
- persistence schema/migrations;
- dependency graph;
- diagnostics.

### Contract tests

A reusable adapter conformance suite must validate any CAD host implementation:

- transaction commit/rollback;
- handle stability;
- entity lifecycle;
- layer/block semantics;
- selection identity;
- undo/redo;
- save/reopen identity;
- xref/layout behavior where capabilities claim support.

### Property/fuzz tests

Prioritize geometry normalization, malformed persistence, numeric extremes, canonical IDs and round-trip serialization.

## 13. Security and legal boundaries

- clean-room implementation only;
- no proprietary vendor binary committed;
- no reverse-engineered confidential SDK material;
- third-party SDK licenses documented by the consuming adapter;
- sanitize diagnostic exports;
- bounded parsers and file inputs;
- fail closed on unsupported schema/capability;
- dependency/license inventory generated for releases.

## 14. Versioning

Platform packages use semantic versioning.

- patch: compatible fixes;
- minor: additive compatible contracts/features;
- major: breaking public API/schema behavior.

Consumers pin an exact released version/commit for production qualification. A CAD or BricsCAD release must record the exact Platform version it was built against.

## 15. Delivery phases

### P0 — repository/bootstrap

- solution/build props;
- Domain, Geometry, CAD Abstractions, Application projects;
- baseline tests;
- architecture docs;
- no vendor dependency.

### P1 — domain extraction

- project/floor/zone/family/element model;
- canonical identities;
- units/numeric policies;
- semantic source/generated ownership.

### P2 — CAD contracts

- document/database/transaction/editor/selection;
- layers/blocks/layout/xref abstractions;
- geometry capability interfaces;
- adapter contract test kit.

### P3 — persistence/quantity

- versioned project state;
- migration framework;
- quantity facts/rules/results;
- schedules/report projections.

### P4 — diagnostics/regeneration

- dependency graph;
- dirty/freshness;
- health/readiness;
- deterministic regeneration orchestration.

### P5 — consumer migration

- BricsCAD adapter consumes Platform progressively;
- standalone CAD consumes Platform natively;
- cross-host semantic parity suite.

## 16. Definition of done for Platform 1.0

Platform 1.0 is reached only when:

- no vendor CAD/UI dependency exists in public or private project references;
- both `QS3D-CAD` and `QS3D-BricsCAD` can consume the same released domain/quantity/persistence contracts;
- adapter contract tests exist and are used by both hosts where applicable;
- project data has a documented versioned migration path;
- deterministic test suites cover critical domain/quantity/persistence invariants;
- public APIs are versioned and documented;
- release provenance identifies exact source and dependency versions.

## 17. Immediate implementation backlog

1. scaffold solution and repository policy;
2. create value-object IDs, finite numeric helpers and geometry primitives;
3. create CAD capability model;
4. create document/database/transaction/editor/selection interfaces;
5. create command/result abstractions;
6. create in-memory adapter/test doubles so the contracts can execute without proprietary SDKs;
7. add unit/contract tests;
8. create migration inventory from `QS3D-BricsCAD`;
9. expose package metadata for later consumer pinning;
10. integrate `QS3D-CAD` against the first stable abstractions.

This document is the architecture/roadmap baseline. Implementation may refine details, but changes to repository responsibility, clean-room/vendor boundaries, identity semantics, persistence authority, or cross-repository ownership require an explicit documented decision.