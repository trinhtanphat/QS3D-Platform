# Work claim — C02 quantity schedule location affinity

- Status: `ACTIVE`
- Agent: `c02-gpt56sol-20260903-1133`
- Registered: `2026-09-03T11:49:33+07:00`
- Baseline main SHA: `7a58a2f5efd6d302a7b0ba6ef73d1d85d630f52b`
- Implementation branch: `agent/c02-gpt56sol-20260903-1133/issue-157-schedule-location-affinity`
- Integration batch: `TBD`
- Lane-Key: `c02-quantity-schedule-location-affinity-20260903`
- Canonical issue: `#157`

## Reserved scope
Fail closed at the Quantity schedule/evidence boundary when an included semantic element's current FloorId or ZoneId no longer belongs to the supplied project.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleLocationAffinityModuleSmoke.cs`
- this coordination claim

## Excluded scope
SemanticProject/SemanticElement production policy, CSV/XLSX formatting, quantity rule/unit arithmetic, UI/MCP, release/install, native CAD, unrelated parity code.

## Validation plan
- deterministic regression first for post-add orphaned FloorId and ZoneId;
- preserve valid and null location metadata;
- validate only elements actually emitted by sparse schedules, while include-empty schedules validate every emitted element;
- preserve SourceReference affinity, missing-element/family checks, cardinality ceilings, aggregation and deterministic ordering;
- exact-head authoritative Platform CI GREEN before merge and exact-main CI after merge.
