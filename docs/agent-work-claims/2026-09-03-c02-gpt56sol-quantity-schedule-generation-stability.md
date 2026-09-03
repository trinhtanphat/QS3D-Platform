# Work claim — C02 quantity schedule generation stability

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T21:46:00+07:00`
- Baseline main SHA: `e95101db567cb7e7f3d49e1d7b345b0674643801`
- Implementation branch: `agent/c02-gpt56sol-20260903/issue-255-schedule-generation-stability`
- Integration batch: `issue-255`
- Lane-Key: `issue-255`
- Issue: `#255`

## Reserved scope
Harden counted top-level `QuantityScheduleRow` input collections against same-cardinality semantic row replacement while preserving canonical schedule ordering and one-pass raw streaming behavior.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantitySchedule.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityScheduleGenerationStabilityModuleSmoke.cs`
- this claim file

## Excluded scope
QuantityRules, BOQ, accumulator, Domain/Persistence, Workspace/UI, MCP, release/installer, native CAD adapters.

## Validation plan
Deterministic regression-only smoke, hosted RED, minimal production fix, targeted/broad smoke, fresh exact-head GREEN, merge, exact-main evidence, terminal claim closeout.
