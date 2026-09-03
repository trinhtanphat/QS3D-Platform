# Work claim — C02 quantity schedule location affinity

- Status: `COMPLETED`
- Agent: `c02-gpt56sol-20260903-1133`
- Registered: `2026-09-03T11:49:33+07:00`
- Baseline main SHA: `7a58a2f5efd6d302a7b0ba6ef73d1d85d630f52b`
- Implementation branch: `agent/c02-gpt56sol-20260903-1133/issue-157-schedule-location-affinity`
- Integration batch: `issue-157`
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

## Validation evidence
- Regression-only RED head: `73957da680afe6a936ce8caeee029afeb9b9de7f`.
- RED CI: `33716551102` — FAILURE before production change.
- Final implementation head: `ebfe885b4c19d2eb60297518efc2134d3d55b2f3`.
- Exact-head CI: `33716701169` — SUCCESS.
- Implementation PR: `#159`.
- Implementation merge commit: `d0fd55326b4d2c259f3d1ab90ee57fcf4d1f849a`.
- Exact implementation-main CI: `33716769045` — SUCCESS.

## Completion condition
Satisfied: included rows fail closed on orphaned FloorId/ZoneId, sparse non-emitted elements remain outside this validation, valid/null location provenance is preserved, implementation merged from a fresh exact-head GREEN candidate, and exact implementation-main CI is GREEN.
