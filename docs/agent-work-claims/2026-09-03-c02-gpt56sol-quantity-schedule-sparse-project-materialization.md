# Work claim — C02 sparse quantity schedule project materialization

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903-1133`
- Registered: `2026-09-03T11:47:07+07:00`
- Baseline main SHA: `7a58a2f5efd6d302a7b0ba6ef73d1d85d630f52b`
- Implementation branch: `agent/c02-gpt56sol-20260903-1133/issue-155-sparse-schedule-elements`
- Integration batch: `TBD`
- Lane-Key: `c02-quantity-schedule-sparse-project-materialization-20260903`
- Canonical issue: `#155`

## Reserved scope
Bound sparse quantity-schedule element materialization to the already-bounded fact identity set instead of eagerly copying every element in an otherwise unbounded valid semantic project.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleSparseProjectMaterializationModuleSmoke.cs`
- this coordination claim

## Excluded scope
CSV/XLSX formatting, quantity rule/unit arithmetic, Domain/Core cardinality or persistence policy, UI/MCP, release/install, native CAD, unrelated parity code.

## Validation plan
- deterministic regression first proving sparse projection can avoid retaining unrelated project elements;
- preserve issue #147 include-empty admission before facts enumeration;
- preserve missing-element and CAD provenance fail-closed behavior;
- preserve deterministic schedule ordering, family/floor/zone metadata and bounded fact materialization;
- run authoritative Platform validation on exact candidate head and exact merged main.
