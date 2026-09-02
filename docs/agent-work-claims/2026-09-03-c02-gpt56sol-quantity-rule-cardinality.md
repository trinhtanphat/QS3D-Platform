# Work claim — C02 quantity rule cardinality

- Status: `ACTIVE`
- Agent: `c02-gpt56sol`
- Registered: `2026-09-03T04:46:00+07:00`
- Baseline main SHA: `0fe0420918c7cbaeca811fed07e4d0d38364ea18`
- Implementation branch: `agent/c02-gpt56sol/issue-81-rule-cardinality`
- Integration batch: `issue-81`
- Lane-Key: `issue-81`

## Reserved scope
Bound caller-controlled quantity-rule factor and catalog enumerables before eager materialization/dimension/duplicate validation.

## Expected surfaces
- `src/QS3D.Platform.Quantity/QuantityRules.cs`
- `tests/QS3D.Platform.SmokeTests/QuantityRuleCardinalityModuleSmoke.cs`
- smoke registration only if required

## Excluded scope
Quantity schedule and accumulator carriers already landed; Workspace/UI, MCP, Core persistence, installer/release.

## Validation plan
- TDD streamed limit+1 RED for both factors and catalog;
- exact-limit, advertised oversized Count and Count/enumerator drift;
- preserve dimension inference, duplicate detection and deterministic ordering;
- exact-head hosted Platform CI.

## Completion condition
Fresh exact-head GREEN, reviewed PR merge and resulting main SHA verification.
