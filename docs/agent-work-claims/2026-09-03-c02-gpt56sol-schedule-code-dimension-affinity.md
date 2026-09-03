# Work claim — C02 schedule code/dimension affinity

- Status: `ACTIVE`
- Agent: `gpt56sol-c02-20260903`
- Registered: `2026-09-03T22:01:45+07:00`
- Baseline main SHA: `dabc7c99f7811245e58f194af36cf77a00404ee9`
- Implementation branch: `agent/gpt56sol-c02-20260903/issue-259-schedule-code-dimension-affinity`
- Integration batch: `TBD`
- Lane-Key: `issue-259`

## Reserved scope
C02 Quantity / schedule/export integrity only. Reject ambiguous reuse of one quantity code for multiple dimensions within a public `QuantityScheduleRow` ingestion boundary.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleCodeDimensionAffinityModuleSmoke.cs`
- this claim file

## Excluded scope
No Domain/Persistence production, Workspace/UI, MCP, release/installer, BOQ pricing arithmetic, or unrelated feature code.

## Validation plan
- deterministic regression first;
- prove same-code/different-dimension row is rejected while distinct codes remain valid;
- preserve duplicate rejection, row-local provenance/evidence, generation-stability and 100,000-entry bounds;
- run hosted Platform CI on exact implementation head and require GREEN before merge.

## Completion condition
Implementation merged to current main, exact main SHA re-read, hosted CI evidence recorded, and issue #259 closed completed.
