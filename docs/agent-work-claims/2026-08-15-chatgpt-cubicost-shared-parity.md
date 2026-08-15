# Work claim — Cubicost-style shared parity master plan

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol`
- Registered: `2026-08-15T13:38:14+07:00`
- Baseline main SHA: `4bbdbc78efe9ab225ae3309152d3efc98bd0f40d`
- Implementation branch: `agent/chatgpt-gpt56sol/cubicost-shared-parity-20260815`
- Integration batch: `TBD`
- Tracking issue: `#13`

## Reserved scope

Consolidate the full clean-room Cubicost-style feature inventory into one authoritative Platform master plan and add missing vendor-neutral shared contracts needed by BricsCAD, AutoCAD and standalone QS3D hosts.

## Expected surfaces

- `docs/CUBICOST-QS3D-FEATURE-MASTER-PLAN.md`
- new vendor-neutral MEP recognition/takeoff/coordination source under `src/QS3D.Platform.Parity/` or adjacent Platform projects as dependency boundaries require
- new vendor-neutral cost/tender/progress parity source under `src/QS3D.Platform.Parity/` or adjacent Platform projects
- deterministic smoke coverage in `tests/QS3D.Platform.SmokeTests/`
- source/preflight guards and compatibility/migration notes

## Excluded scope

- BricsCAD/AutoCAD/ODA/proprietary SDK types, binaries or runtime evidence
- BricsCAD-native commands, Solid3d interference, palette/highlight/zoom implementation
- AutoCAD-native commands/palette/ribbon implementation
- standalone native DWG/rendering implementation
- server/cloud implementation
- direct implementation writes to `main`

## Validation plan

- preserve `netstandard2.0` for shared libraries
- no vendor namespaces/references in public Platform APIs
- deterministic finite/identity/duplicate/ambiguity guards
- deterministic smoke coverage for recognition, MEP aggregation, clash coordination, rate buildup, benchmarks, tender evaluation and progress claims
- exact diff review against refreshed `main`

## Completion condition

The feature/ownership matrix is authoritative, the reserved host-neutral parity contracts and tests are source-complete on the implementation branch, and a reviewable PR is published without claiming native CAD qualification.
